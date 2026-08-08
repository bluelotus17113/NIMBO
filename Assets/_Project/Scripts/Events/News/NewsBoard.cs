using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Events.Scheduling;
using Nimbo.Data.Social;

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
    public class NewsBoard : IDisposable
    {
        readonly EventsConfig _config;
        readonly List<Headline> _headlines = new List<Headline>();
        long _currentMinute;

        // Delegates para poder desuscribir
        readonly Action<RomanceStageChanged> _onRomance;
        readonly Action<IslanderLeveledUp> _onLevelUp;
        readonly Action<BabyBorn> _onBaby;
        readonly Action<BuildingUnlocked> _onBuilding;
        readonly Action<ConflictStageChanged> _onConflict;
        readonly Action<HourPassed> _onHour;

        public IReadOnlyList<Headline> Headlines => _headlines;

        public NewsBoard(EventsConfig config)
        {
            _config = config;

            _onRomance = OnRomanceChanged;
            _onLevelUp = OnLevelUp;
            _onBaby = OnBabyBorn;
            _onBuilding = OnBuildingUnlocked;
            _onConflict = OnConflictChanged;
            _onHour = OnHourPassed;

            EventBus.Subscribe(_onRomance);
            EventBus.Subscribe(_onLevelUp);
            EventBus.Subscribe(_onBaby);
            EventBus.Subscribe(_onBuilding);
            EventBus.Subscribe(_onConflict);
            EventBus.Subscribe(_onHour);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(_onRomance);
            EventBus.Unsubscribe(_onLevelUp);
            EventBus.Unsubscribe(_onBaby);
            EventBus.Unsubscribe(_onBuilding);
            EventBus.Unsubscribe(_onConflict);
            EventBus.Unsubscribe(_onHour);
        }

        // ------------------------------------------------------------------- suscripciones

        void OnHourPassed(HourPassed evt) => _currentMinute = evt.Day * 24L * 60L + evt.Hour * 60L;

        void OnRomanceChanged(RomanceStageChanged evt)
        {
            string a = ShortName(evt.FromId);
            string b = ShortName(evt.ToId);
            if (a == null || b == null) return;

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
        }

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
