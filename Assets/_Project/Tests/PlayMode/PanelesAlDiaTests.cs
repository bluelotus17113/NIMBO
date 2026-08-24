using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Player;
using Nimbo.Data.Requests;
using Nimbo.Data.World;
using Nimbo.UI.Achievements;
using Nimbo.UI.Chronicle;
using Nimbo.UI.Player;
using Nimbo.UI.Requests;
using Nimbo.UI.Village;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que un panel **abierto** refleje lo que cambia detrás mientras se mira.
    /// </summary>
    /// <remarks>
    /// El tablón, los logros, la crónica, las fiestas y las vías solo se levantaban al
    /// abrirlos: un encargo caducaba delante del jugador y la pantalla lo seguía
    /// enseñando en verde hasta cerrar y reabrir. Ahora cada panel tiene un
    /// <c>Refresh</c> que firma lo que pinta y solo reconstruye si la firma cambió —
    /// el patrón de <c>JobSection</c>, que evitó que la ficha se comiera sus propios
    /// mensajes por refrescar a ciegas cada 0,4 s.
    ///
    /// Cada panel se prueba por los dos lados: que refleja el cambio cuando toca
    /// refrescar, y que NO reconstruye nada cuando nada cambió — sin la segunda
    /// mitad, meterlos en el ciclo lento de UiRoot sería el derroche que ya costó
    /// una ficha. Va en modo juego porque el ensamblado de edición no ve
    /// <c>Nimbo.UI</c>.
    /// </remarks>
    public class PanelesAlDiaTests
    {
        private IRequestService _peticionesPrevias;
        private IIslanderRegistry _censoPrevio;
        private GameClock _relojPrevio;
        private IAchievementService _logrosPrevios;
        private IChronicleService _cronicaPrevia;
        private IVillageEvents _fiestasPrevias;
        private IPlayerProgression _viasPrevias;

        private bool _habiaPeticiones;
        private bool _habiaCenso;
        private bool _habiaReloj;
        private bool _habiaLogros;
        private bool _habiaCronica;
        private bool _habiaFiestas;
        private bool _habiaVias;

        [SetUp]
        public void SetUp()
        {
            // Nada de ServiceRegistry.Clear(): el registro es global y todas las
            // pruebas comparten proceso (ver CronicaVisibleTests). Se guarda lo que
            // hubiera y se devuelve al salir.
            _habiaPeticiones = ServiceRegistry.TryGet(out _peticionesPrevias);
            _habiaCenso = ServiceRegistry.TryGet(out _censoPrevio);
            _habiaReloj = ServiceRegistry.TryGet(out _relojPrevio);
            _habiaLogros = ServiceRegistry.TryGet(out _logrosPrevios);
            _habiaCronica = ServiceRegistry.TryGet(out _cronicaPrevia);
            _habiaFiestas = ServiceRegistry.TryGet(out _fiestasPrevias);
            _habiaVias = ServiceRegistry.TryGet(out _viasPrevias);
        }

        [TearDown]
        public void TearDown()
        {
            Devolver<IRequestService>(_peticionesPrevias, _habiaPeticiones);
            Devolver<IIslanderRegistry>(_censoPrevio, _habiaCenso);
            Devolver<GameClock>(_relojPrevio, _habiaReloj);
            Devolver<IAchievementService>(_logrosPrevios, _habiaLogros);
            Devolver<IChronicleService>(_cronicaPrevia, _habiaCronica);
            Devolver<IVillageEvents>(_fiestasPrevias, _habiaFiestas);
            Devolver<IPlayerProgression>(_viasPrevias, _habiaVias);
        }

        private static void Devolver<T>(T servicio, bool habia) where T : class
        {
            if (habia) Poner(servicio);
            else ServiceRegistry.Unregister<T>();
        }

        /// <summary>Registra sin el aviso de «ya estaba registrado».</summary>
        private static void Poner<T>(T service) where T : class
        {
            ServiceRegistry.Unregister<T>();
            ServiceRegistry.Register(service);
        }

        // ── el tablón ────────────────────────────────────────────────────────

        [Test]
        public void ElTablonReflejaUnEncargoCaducadoMientrasEstabaAbierto()
        {
            var tablon = new TablonDePrueba();
            tablon.Poner("bea", RequestKind.Material, "mat_madera", 5,
                         "Necesito tablas.", expiraEn: 1120);
            Poner<IRequestService>(tablon);
            IIslanderRegistry censo = new CensoDePrueba();
            Poner(censo);
            var reloj = new GameClock(1000);
            Poner(reloj);

            var panel = new RequestBoardPanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "Quedan unas 2 horas"), Is.True,
                "el plazo no se pinta; no hay nada que caduque delante del jugador");

            // El minuto que cruza el plazo pasa mientras el tablón está abierto.
            reloj.Advance(121);
            panel.Refresh();

            Assert.That(Contiene(panel.Root, "Se le ha pasado el momento"), Is.True,
                "el encargo caducó con el tablón abierto y la pantalla siguió " +
                "enseñándolo como si tuviera tiempo");
        }

        [Test]
        public void ElTablonReflejaQueLoQueAntesFaltabaAhoraSePuedeDar()
        {
            var tablon = new TablonDePrueba();
            tablon.Poner("bea", RequestKind.Material, "mat_madera", 5,
                         "Necesito tablas.");
            Poner<IRequestService>(tablon);
            IIslanderRegistry censo = new CensoDePrueba();
            Poner(censo);

            var panel = new RequestBoardPanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "te falta"), Is.True);

            // Vender de la mochila o recoger madera cambia lo pagable sin que el
            // tablón se entere por otro lado: es justo lo que firma.
            tablon.Tengo("mat_madera");
            panel.Refresh();

            Assert.That(Botones(panel.Root).Exists(t => t.StartsWith("Darle")), Is.True,
                "hay con qué pagarle a Bea y el tablón sigue diciendo que falta");
        }

        [Test]
        public void ElTablonNoSeReconstruyeSiNadaCambio()
        {
            var tablon = new TablonDePrueba();
            tablon.Poner("bea", RequestKind.Material, "mat_madera", 5, "Necesito tablas.");
            Poner<IRequestService>(tablon);
            IIslanderRegistry censo = new CensoDePrueba();
            Poner(censo);

            var panel = new RequestBoardPanel();
            panel.Show();

            var antes = Etiquetas(panel.Root);
            panel.Refresh();
            var despues = Etiquetas(panel.Root);

            Assert.That(despues, Is.EqualTo(antes),
                "el tablón reconstruyó sus filas sin que cambiara nada: son las " +
                "filas derribadas bajo el cursor que se comían los clics en la ficha");
        }

        // ── los logros ───────────────────────────────────────────────────────

        [Test]
        public void LosLogrosReflejanUnoNuevoMientrasEstaAbierto()
        {
            var logros = new LogrosDePrueba();
            logros.Definir("log_a", "Primer paso", 1);
            logros.Definir("log_b", "Paso largo", 10);
            Poner<IAchievementService>(logros);

            var panel = new AchievementsPanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "0 de 2 conseguidos"), Is.True);

            logros.Advance("log_a");
            panel.Refresh();

            Assert.That(Contiene(panel.Root, "1 de 2 conseguidos"), Is.True,
                "se consiguió un logro con la pantalla abierta y la cabecera se quedó vieja");
            Assert.That(Contiene(panel.Root, "hecho"), Is.True,
                "la fila del logro nuevo no lleva su marca de hecho");
        }

        [Test]
        public void LosLogrosNoSeReconstruyenSiNadaCambio()
        {
            var logros = new LogrosDePrueba();
            logros.Definir("log_a", "Primer paso", 1);
            Poner<IAchievementService>(logros);

            var panel = new AchievementsPanel();
            panel.Show();

            var antes = Etiquetas(panel.Root);
            panel.Refresh();
            var despues = Etiquetas(panel.Root);

            Assert.That(despues, Is.EqualTo(antes),
                "cuarenta filas reconstruidas cada 0,4 s para enseñar siempre lo mismo");
        }

        // ── la crónica ───────────────────────────────────────────────────────

        [Test]
        public void LaCronicaReflejaUnaLineaNuevaMientrasEstaAbierta()
        {
            var cronica = new CronicaDePrueba();
            cronica.Anadir(1, "Ana y Leo se conocieron.");
            Poner<IChronicleService>(cronica);
            Poner(new GameClock((1 - 1L) * GameClock.MinutesPerDay));

            var panel = new ChroniclePanel();
            panel.Show();

            cronica.Anadir(1, "Ana y Leo se han casado.");
            panel.Refresh();

            Assert.That(Contiene(panel.Root, "Ana y Leo se han casado."), Is.True,
                "la boda se contó con la crónica abierta y no aparece por ninguna parte");
        }

        [Test]
        public void LaCronicaAmaneceConLasCabecerasCambiadas()
        {
            var cronica = new CronicaDePrueba();
            cronica.Anadir(5, "Algo pasó.");
            Poner<IChronicleService>(cronica);
            Poner(new GameClock((5 - 1L) * GameClock.MinutesPerDay));

            var panel = new ChroniclePanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "Hoy"), Is.True);

            // Amanece sin entrar ninguna línea nueva: «Hoy» ya no lo es.
            Poner(new GameClock((6 - 1L) * GameClock.MinutesPerDay));
            panel.Refresh();

            Assert.That(Contiene(panel.Root, "Ayer"), Is.True,
                "lo que ayer era «Hoy» sigue diciendo «Hoy» al día siguiente");
            Assert.That(Contiene(panel.Root, "Hoy"), Is.False,
                "«Hoy» y «Ayer» a la vez es una pantalla que miente dos veces");
        }

        // ── las fiestas ──────────────────────────────────────────────────────

        [Test]
        public void LasFiestasReflejanQueYaSePuedeMontarMientrasEstaAbierta()
        {
            var fiestas = new FiestasDePrueba { Veredicto = HostRefusal.NotEnoughCoins };
            Poner<IVillageEvents>(fiestas);

            var panel = new EventsPanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "no llega"), Is.True);

            fiestas.Veredicto = HostRefusal.Ok;
            panel.Refresh();

            Assert.That(Botones(panel.Root), Does.Contain("Montarla"),
                "llegaron los nimbos con el panel abierto y la fiesta sigue en «no llega»");
        }

        [Test]
        public void LasFiestasNoSeReconstruyenSiNadaCambio()
        {
            Poner<IVillageEvents>(new FiestasDePrueba());

            var panel = new EventsPanel();
            panel.Show();

            var antes = Etiquetas(panel.Root);
            panel.Refresh();
            var despues = Etiquetas(panel.Root);

            Assert.That(despues, Is.EqualTo(antes),
                "reconstruyó las filas de fiestas sin que el veredicto de ninguna cambiara");
        }

        // ── las vías ─────────────────────────────────────────────────────────

        [Test]
        public void LasViasReflejanUnaSubidaDeNivelMientrasEstaAbierta()
        {
            using var vias = new Nimbo.Player.PlayerProgressionService(new PlayerState());
            Poner<IPlayerProgression>(vias);

            var panel = new SkillsPanel();
            panel.Show();
            Assert.That(Contiene(panel.Root, "nivel 1"), Is.True);

            vias.Grant(SkillKind.Farming, Data.Player.SkillSet.XpForLevel(1));
            panel.Refresh();

            Assert.That(Contiene(panel.Root, "nivel 2"), Is.True,
                "la vía subió de nivel con la pantalla abierta y sigue enseñando la vieja");
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        private static List<string> Textos(VisualElement raiz)
        {
            var textos = new List<string>();
            Recorrer(raiz, textos, botonesSolo: false);
            return textos;
        }

        private static List<string> Botones(VisualElement raiz)
        {
            var textos = new List<string>();
            Recorrer(raiz, textos, botonesSolo: true);
            return textos;
        }

        private static bool Contiene(VisualElement raiz, string fragmento)
        {
            foreach (var texto in Textos(raiz))
                if (texto.Contains(fragmento)) return true;
            return false;
        }

        private static void Recorrer(VisualElement element, List<string> textos,
                                     bool botonesSolo)
        {
            if (element is Button button)
            {
                if (!string.IsNullOrEmpty(button.text)) textos.Add(button.text);
            }
            else if (!botonesSolo && element is Label label &&
                     !string.IsNullOrEmpty(label.text))
            {
                textos.Add(label.text);
            }

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos, botonesSolo);
        }

        /// <summary>
        /// Las instancias de etiqueta del árbol, en orden. Si tras un Refresh son las
        /// mismas referencias, no hubo reconstrucción: es la única forma honesta de
        /// comprobar que la firma frena el repintado.
        /// </summary>
        private static List<Label> Etiquetas(VisualElement raiz)
        {
            var etiquetas = new List<Label>();
            Buscar(raiz, etiquetas);
            return etiquetas;

            static void Buscar(VisualElement element, List<Label> encontradas)
            {
                if (element is Label label) encontradas.Add(label);

                for (int i = 0; i < element.childCount; i++)
                    Buscar(element[i], encontradas);
            }
        }

        // ── los dobles ──────────────────────────────────────────────────────

        /// <summary>Una cola de peticiones de mentira, con lo que el jugador lleva encima.</summary>
        private sealed class TablonDePrueba : IRequestService
        {
            private readonly List<IslanderRequest> _abiertas = new List<IslanderRequest>();
            private readonly HashSet<string> _tengo = new HashSet<string>();

            public void Poner(string islanderId, RequestKind kind, string targetId,
                              int amount, string line, long expiraEn = 10_000)
            {
                _abiertas.Add(new IslanderRequest
                {
                    RequestId = $"pet-{_abiertas.Count}",
                    IslanderId = islanderId,
                    Kind = kind,
                    Priority = RequestPriority.Normal,
                    TargetId = targetId,
                    Amount = amount,
                    Line = line,
                    ExpiresMinute = expiraEn,
                });
            }

            public void Tengo(params string[] catalogIds)
            {
                foreach (var id in catalogIds) _tengo.Add(id);
            }

            public IReadOnlyList<IslanderRequest> Open => _abiertas;
            public int OpenCount => _abiertas.Count;

            public bool TryGet(string requestId, out IslanderRequest request)
            {
                for (int i = 0; i < _abiertas.Count; i++)
                {
                    if (_abiertas[i].RequestId != requestId) continue;
                    request = _abiertas[i];
                    return true;
                }
                request = default;
                return false;
            }

            public IEnumerable<IslanderRequest> OpenFor(string islanderId)
            {
                for (int i = 0; i < _abiertas.Count; i++)
                    if (_abiertas[i].IslanderId == islanderId)
                        yield return _abiertas[i];
            }

            public RequestDemand DemandOf(string requestId)
            {
                if (!TryGet(requestId, out var request)) return RequestDemand.Nothing;

                return request.Kind == RequestKind.Material
                    ? new RequestDemand(ItemCategory.Material, request.TargetId,
                                        request.Amount)
                    : RequestDemand.Nothing;
            }

            public IReadOnlyList<string> OptionsFor(string requestId)
            {
                var opciones = new List<string>();
                var demanda = DemandOf(requestId);
                if (!demanda.WantsAnItem) return opciones;

                if (_tengo.Contains(demanda.CatalogId)) opciones.Add(demanda.CatalogId);
                return opciones;
            }

            public bool Resolve(string requestId, string payloadId = null) => true;
            public void Refuse(string requestId) { }
            public IslanderRequest Raise(string islanderId, RequestKind kind,
                                         string targetId = null) => default;
        }

        /// <summary>Un censo de mentira, solo para poner nombres a los identificadores.</summary>
        private sealed class CensoDePrueba : IIslanderRegistry
        {
            private readonly List<IslanderData> _todos = new List<IslanderData>();

            public IReadOnlyList<IslanderData> All => _todos;
            public int Count => _todos.Count;
            public IslanderData Get(string id) => null;
            public bool TryGet(string islanderId, out IslanderData islander)
            {
                islander = null;
                return false;
            }
            public bool Exists(string islanderId) => false;
            public IEnumerable<IslanderData> InZone(string zoneId) => _todos;
            public void Add(IslanderData islander) { }
            public void Remove(string islanderId) { }
        }

        /// <summary>Un catálogo de logros de mentira, con avance en memoria.</summary>
        private sealed class LogrosDePrueba : IAchievementService
        {
            private readonly List<AchievementDefinition> _catalogo =
                new List<AchievementDefinition>();
            private readonly Dictionary<string, int> _avance =
                new Dictionary<string, int>();
            private readonly HashSet<string> _conseguidos = new HashSet<string>();

            public void Definir(string id, string nombre, int meta) =>
                _catalogo.Add(new AchievementDefinition(id, nombre, "por qué no",
                                AchievementKind.Life, meta, 10, false));

            public IReadOnlyList<AchievementDefinition> Catalog => _catalogo;

            public bool TryGetDefinition(string achievementId,
                                         out AchievementDefinition definition)
            {
                foreach (var def in _catalogo)
                {
                    if (def.AchievementId != achievementId) continue;
                    definition = def;
                    return true;
                }
                definition = default;
                return false;
            }

            public AchievementProgress ProgressOf(string achievementId)
            {
                TryGetDefinition(achievementId, out var def);
                _avance.TryGetValue(achievementId, out int actual);
                return new AchievementProgress(achievementId, actual, def.Goal,
                                               _conseguidos.Contains(achievementId));
            }

            public int UnlockedCount => _conseguidos.Count;

            public bool Advance(string achievementId, int amount = 1)
            {
                _avance.TryGetValue(achievementId, out int actual);
                _avance[achievementId] = actual + amount;

                TryGetDefinition(achievementId, out var def);
                if (_avance[achievementId] < def.Goal ||
                    !_conseguidos.Add(achievementId))
                    return false;

                return true;
            }

            public bool Record(string achievementId, int value) =>
                Advance(achievementId, value);
        }

        /// <summary>Una crónica de mentira, para no montar la aldea entera.</summary>
        private sealed class CronicaDePrueba : IChronicleService
        {
            private readonly List<ChronicleEntry> _entries = new List<ChronicleEntry>();
            public IReadOnlyList<ChronicleEntry> Entries => _entries;
            public void Anadir(int day, string text) =>
                _entries.Add(new ChronicleEntry(day, text));
        }

        /// <summary>Un calendario de fiestas de mentira con veredicto manipulable.</summary>
        private sealed class FiestasDePrueba : IVillageEvents
        {
            public HostRefusal Veredicto = HostRefusal.NotEnoughCoins;

            private readonly List<HostableEvent> _hostables = new List<HostableEvent>
            {
                new HostableEvent("fiesta_prueba", "Verbena", "Una verbena.", 50, false),
            };

            public IReadOnlyList<HostableEvent> Hostable => _hostables;
            public HostRefusal CanHost(string eventId) => Veredicto;
            public bool Host(string eventId) => false;
            public string ActiveEventId => "";
            public string ActiveEventZoneId => "";
        }
    }
}
