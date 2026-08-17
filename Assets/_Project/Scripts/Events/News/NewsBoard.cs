using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Core.Time;
using Nimbo.Events.Scheduling;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Data.World;

namespace Nimbo.Events.News
{
    /// <summary>
    /// Un titular del tablón, con el minuto de juego en que se escribió.
    /// </summary>
    public class Headline
    {
        public readonly string Text;
        public readonly long GameMinute;

        public Headline(string text, long gameMinute)
        {
            Text = text;
            GameMinute = gameMinute;
        }

        public override string ToString() => Text;
    }

    /// <summary>
    /// El tablón de noticias de la plaza central. Se suscribe a los sucesos sociales
    /// y de progresión y escribe titulares con los nombres reales de los implicados.
    /// Guarda los últimos N y tira los más viejos.
    /// </summary>
    public class NewsBoard : IDisposable, IChronicleService
    {
        /// <summary>Cuántas líneas de crónica se guardan antes de tirar las viejas. ⚙️</summary>
        /// <remarks>
        /// Más que los titulares del tablón, y por eso son dos listas y no una: el tablón
        /// de la plaza es lo de ahora —caben veinte— y la crónica es la memoria de la
        /// aldea, que hay que poder leer hacia atrás. Ciento cincuenta son varias semanas
        /// de juego sin engordar el guardado de forma apreciable.
        /// </remarks>
        const int MaxChronicle = 150;

        readonly EventsConfig _config;
        readonly List<Headline> _headlines = new List<Headline>();
        readonly SaveGame _save;
        readonly GameClock _clock;
        long _currentMinute;

        /// <summary>
        /// La última etapa contada de cada pareja, para no contarla dos veces.
        /// </summary>
        /// <remarks>
        /// Hace falta porque **casi todos los cambios de relación se publican por los dos
        /// lados**: <c>SocialService.Wed</c> lanza un <see cref="RomanceStageChanged"/>
        /// para cada cónyuge, y el evaluador de romance hace lo mismo al empezar a salir
        /// o al prometerse. Sin esto, el tablón escribía «¡Ana y Leo se han casado!» dos
        /// veces seguidas, con dos plantillas distintas para más gracia.
        /// </remarks>
        readonly Dictionary<string, RomanceStage> _lastRomance = new Dictionary<string, RomanceStage>();
        readonly Dictionary<string, ConflictStage> _lastConflict = new Dictionary<string, ConflictStage>();

        // Delegates para poder desuscribir
        readonly Action<RomanceStageChanged> _onRomance;
        readonly Action<IslanderLeveledUp> _onLevelUp;
        readonly Action<BabyBorn> _onBaby;
        readonly Action<BuildingUnlocked> _onBuilding;
        readonly Action<ConflictStageChanged> _onConflict;
        readonly Action<WeddingAnnounced> _onWedding;
        readonly Action<HourPassed> _onHour;

        public IReadOnlyList<Headline> Headlines => _headlines;

        /// <summary>La crónica guardada. Vacía si el tablón se montó sin partida.</summary>
        public IReadOnlyList<ChronicleEntry> Entries =>
            _save != null ? _save.Chronicle : (IReadOnlyList<ChronicleEntry>)Array.Empty<ChronicleEntry>();

        /// <summary>
        /// El tablón. Sin <paramref name="save"/> escribe solo en memoria, que es lo que
        /// quieren los tests del tablón en sí.
        /// </summary>
        public NewsBoard(EventsConfig config, SaveGame save = null, GameClock clock = null)
        {
            _config = config;
            _save = save;
            _clock = clock;

            _onRomance = OnRomanceChanged;
            _onLevelUp = OnLevelUp;
            _onBaby = OnBabyBorn;
            _onBuilding = OnBuildingUnlocked;
            _onConflict = OnConflictChanged;
            _onWedding = OnWeddingAnnounced;
            _onHour = OnHourPassed;

            EventBus.Subscribe(_onRomance);
            EventBus.Subscribe(_onLevelUp);
            EventBus.Subscribe(_onBaby);
            EventBus.Subscribe(_onBuilding);
            EventBus.Subscribe(_onConflict);
            EventBus.Subscribe(_onWedding);
            EventBus.Subscribe(_onHour);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(_onRomance);
            EventBus.Unsubscribe(_onLevelUp);
            EventBus.Unsubscribe(_onBaby);
            EventBus.Unsubscribe(_onBuilding);
            EventBus.Unsubscribe(_onConflict);
            EventBus.Unsubscribe(_onWedding);
            EventBus.Unsubscribe(_onHour);
        }

        // ------------------------------------------------------------------- suscripciones

        void OnHourPassed(HourPassed evt) => _currentMinute = evt.Day * 24L * 60L + evt.Hour * 60L;

        void OnWeddingAnnounced(WeddingAnnounced evt)
        {
            string a = ShortName(evt.AId);
            string b = ShortName(evt.BId);
            if (a == null || b == null) return;

            AddHeadline(PickTemplate(new[] {
                $"¡Boda el día {evt.Day}: {a} y {b} se casan!",
                $"Que no se te olvide: {a} y {b} se casan el día {evt.Day}.",
                $"La isla prepara la boda de {a} y {b}, el día {evt.Day}.",
            }));
        }

        void OnRomanceChanged(RomanceStageChanged evt)
        {
            string a = ShortName(evt.FromId);
            string b = ShortName(evt.ToId);
            if (a == null || b == null) return;

            // Un flechazo es de uno hacia otro y las dos direcciones son noticia: que
            // Ana esté colada por Leo y que Leo lo esté por Ana son dos titulares, y ahí
            // está la historia. Salir, prometerse, casarse y romper son estados de la
            // pareja, y esos se cuentan una vez.
            bool directional = evt.Stage is RomanceStage.Crush or RomanceStage.Confessed;
            string key = directional
                ? $"{evt.FromId}>{evt.ToId}"
                : PairKey(evt.FromId, evt.ToId);

            if (_lastRomance.TryGetValue(key, out var told) && told == evt.Stage) return;
            _lastRomance[key] = evt.Stage;

            string[] templates = evt.Stage switch
            {
                RomanceStage.Crush => new[] {
                    $"¡{a} está coladit{e_oa(a)} por {b}!",
                    $"Dicen que a {a} le gusta {b}… y no lo niega.",
                    $"{a} mira a {b} distinto. Algo se cuece.",
                },
                RomanceStage.Dating => new[] {
                    $"¡{a} y {b} ya son pareja!",
                    $"{a} y {b} han dado el paso. La isla entera lo celebra.",
                    $"Oficial: {a} y {b} están junt{o_os(a, b)}.",
                },
                RomanceStage.Engaged => new[] {
                    $"¡{a} y {b} se han prometido!",
                    $"Campanas de boda para {a} y {b}.",
                    $"{a} le pidió la mano a {b}. Y dijo que sí.",
                },
                RomanceStage.Married => new[] {
                    $"¡{a} y {b} se han casado!",
                    $"Boda en la isla: {a} y {b} ya son matrimonio.",
                    $"{a} y {b} se juraron amor eterno bajo el Árbol Nimbo.",
                },
                RomanceStage.Separated => new[] {
                    $"{a} y {b} han roto. Necesitan espacio.",
                    $"Se acabó: {a} y {b} ya no están junt{o_os(a, b)}.",
                },
                _ => null,
            };

            if (templates != null) AddHeadline(PickTemplate(templates));
        }

        void OnLevelUp(IslanderLeveledUp evt)
        {
            string name = ShortName(evt.IslanderId);
            if (name == null) return;

            string[] templates = evt.NewLevel switch
            {
                10 => new[] {
                    $"¡{name} ha llegado al nivel 10!",
                    $"{name} ya es todo{u_na(name)} profesional.",
                },
                20 => new[] {
                    $"¡{name} alcanza el nivel 20!",
                    $"{name} sigue subiendo como la espuma.",
                },
                50 => new[] {
                    $"¡{name} ha llegado al nivel 50! ¡Legendari{o_a(name)}!",
                    $"{name} entra en el salón de la fama de la isla.",
                },
                _ => null,
            };

            if (templates != null) AddHeadline(PickTemplate(templates));
        }

        void OnBabyBorn(BabyBorn evt)
        {
            string a = ShortName(evt.ParentAId);
            string b = ShortName(evt.ParentBId);
            string c = ShortName(evt.ChildId);
            if (a == null || b == null || c == null) return;

            string[] templates = {
                $"¡Ha nacido {c}! {a} y {b} son padres.",
                $"La isla tiene nuev{o_oa(c)} habitante: ¡bienvenid{o_a(c)}, {c}!",
                $"{c} llegó a la isla. {a} y {b} no caben de orgullo.",
            };
            AddHeadline(PickTemplate(templates));
        }

        void OnBuildingUnlocked(BuildingUnlocked evt)
        {
            string[] templates = {
                $"¡Nuevo edificio disponible: {evt.BuildingId}!",
                $"La isla crece: {evt.BuildingId} ya está abierto.",
                $"Se inaugura {evt.BuildingId}. ¡A estrenarlo!",
            };
            AddHeadline(PickTemplate(templates));
        }

        void OnConflictChanged(ConflictStageChanged evt)
        {
            string a = ShortName(evt.FromId);
            string b = ShortName(evt.ToId);
            if (a == null || b == null) return;

            // Una riña es de dos, y la afinidad se mueve en los dos sentidos: sin este
            // filtro, cada discusión salía contada dos veces.
            string key = PairKey(evt.FromId, evt.ToId);
            if (_lastConflict.TryGetValue(key, out var told) && told == evt.Stage) return;
            _lastConflict[key] = evt.Stage;

            string[] templates = evt.Stage switch
            {
                ConflictStage.Tension => new[] {
                    $"Hay tirantez entre {a} y {b}…",
                    $"{a} y {b} no se miran igual. Algo pasa.",
                },
                ConflictStage.Quarrel => new[] {
                    $"¡{a} y {b} han discutido!",
                    $"Se oyeron gritos: {a} y {b} tuvieron una pelea.",
                    $"{a} y {b} están de morros. Ojalá se les pase.",
                },
                ConflictStage.Feud => new[] {
                    $"{a} y {b} están en plena enemistad.",
                    $"Esto es serio: {a} y {b} ya ni se hablan.",
                },
                _ => null,
            };

            if (templates != null) AddHeadline(PickTemplate(templates));
        }

        // ------------------------------------------------------------------- helpers

        void AddHeadline(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _headlines.Add(new Headline(text, _currentMinute));
            while (_headlines.Count > _config.MaxHeadlines)
                _headlines.RemoveAt(0);

            if (_save == null) return;

            _save.Chronicle.Add(new ChronicleEntry(Today(), text));
            while (_save.Chronicle.Count > MaxChronicle)
                _save.Chronicle.RemoveAt(0);
        }

        /// <summary>
        /// Qué día es. Del reloj si lo hay, y si no de la última hora que pasó.
        /// </summary>
        /// <remarks>
        /// El reloj primero porque la primera línea de una partida puede escribirse antes
        /// de que haya pasado ninguna hora, y entonces <c>_currentMinute</c> vale cero y
        /// la crónica empezaría con un «día 0» que no existe.
        /// </remarks>
        int Today() => _clock?.Day ?? (int)(_currentMinute / (24L * 60L)) + 1;

        /// <summary>
        /// La clave de una pareja, sin importar el orden. Es lo que evita contar dos
        /// veces lo que se publica desde los dos lados.
        /// </summary>
        static string PairKey(string one, string other) =>
            string.CompareOrdinal(one, other) <= 0 ? $"{one}|{other}" : $"{other}|{one}";

        static string PickTemplate(string[] templates)
        {
            if (templates == null || templates.Length == 0) return null;
            var rng = Rng.FromTime();
            return templates[rng.Range(0, templates.Length)];
        }

        string ShortName(string islanderId)
        {
            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            if (registry == null || !registry.TryGet(islanderId, out var islander)) return null;
            return islander.Identity.ShortName;
        }

        // ------------------------------------------------------------------- gramática mínima en español

        /// <summary>a/o según si el nombre acaba en 'a' (femenino) o no (masculino).
        /// Heurística: acaba en 'a' → femenino. Nada más.</summary>
        static string e_oa(string name)
        {
            if (string.IsNullOrEmpty(name)) return "o";
            char last = char.ToLowerInvariant(name[name.Length - 1]);
            return last == 'a' ? "a" : "o";
        }

        static string o_os(string a, string b) => "os";

        static string o_a(string name) => e_oa(name);

        static string u_na(string name) => e_oa(name) == "a" ? "a una" : " un";

        static string o_oa(string name) => e_oa(name) == "a" ? "a" : "o";
    }
}
