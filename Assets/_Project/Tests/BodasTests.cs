using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using Nimbo.Social.Romance;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que dos vecinos prometidos lleguen a casarse solos.
    /// </summary>
    /// <remarks>
    /// Antes no llegaban. <c>RomanceEvaluator</c> los llevaba del flechazo al
    /// compromiso por su cuenta, y ahí se paraban para siempre: <c>TryMarry</c> estaba
    /// escrito, probado y **no lo llamaba nadie**. Los tests de <c>SocialService</c>
    /// pasaban porque comprobaban que casar funciona, no que llegue a pasar.
    ///
    /// Estos van con el <see cref="SocialService"/> de verdad y no con un doble a
    /// propósito: lo que hay que demostrar es que la cadena entera cierra —planificador,
    /// umbrales de la configuración, <c>PartnerOf</c>, <c>CanMarry</c>— y un doble que yo
    /// escriba se va a comportar como yo espere, que es precisamente lo que no sirve.
    /// </remarks>
    public class BodasTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private SocialService _social;
        private WeddingPlanner _planner;

        private string _ana;
        private string _leo;

        /// <summary>Días prometidos + días de aviso, que es lo que tarda una boda.</summary>
        private const int DiasHastaLaBoda = 5 + 3;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            var catalog = new ItemCatalog();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();

            _simulation = new SimulationService(_registry, personalities, needsConfig);
            var economy = new EconomyService(catalog, _save);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, _simulation, factory,
                                        _clock, socialConfig);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_simulation);
            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<ISocialService>(_social);

            var ana = factory.CreateRandom("ana");
            var leo = factory.CreateRandom("leo");
            _registry.Add(ana);
            _registry.Add(leo);
            _save.Islanders.Add(ana);
            _save.Islanders.Add(leo);
            _ana = ana.Id;
            _leo = leo.Id;

            _planner = new WeddingPlanner(_save, _registry, _social, _simulation);
        }

        [TearDown]
        public void TearDown()
        {
            _planner?.Dispose();
            _social?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── utilidades ───────────────────────────────────────────────────────

        /// <summary>
        /// Los pone prometidos por los dos lados, con afinidad de sobra.
        /// </summary>
        /// <remarks>
        /// Se escribe en el libro de relaciones a mano en vez de dejar que la
        /// simulación los enamore: llegar hasta el compromiso por convivencia son
        /// decenas de días de juego y lo que se prueba aquí es el tramo final. Que la
        /// rama de antes funciona ya lo cubren los tests de <c>SocialService</c>.
        /// </remarks>
        private void Prometer(float afinidad = 90f)
        {
            Poner(_ana, _leo, RomanceStage.Engaged, afinidad);
            Poner(_leo, _ana, RomanceStage.Engaged, afinidad);
        }

        private void Poner(string deQuien, string aQuien, RomanceStage etapa, float afinidad)
        {
            var islander = _registry.Get(deQuien);
            var record = islander.Relationships.GetOrCreate(aQuien);
            record.Romance = etapa;
            record.Affinity = afinidad;
            record.Friendship = FriendshipStage.BestFriend;
            islander.Relationships.Set(record);
        }

        private RomanceStage EtapaDe(string deQuien, string aQuien) =>
            _social.GetRelationship(deQuien, aQuien).Romance;

        /// <summary>Pasa los días de uno en uno, como lo haría el reloj.</summary>
        private void PasarDias(int desde, int cuantos)
        {
            for (int d = 0; d < cuantos; d++) _planner.AdvanceDay(desde + d);
        }

        // ── 1. el callejón sin salida ────────────────────────────────────────

        [Test]
        public void UnaParejaPrometidaAcabaCasada()
        {
            Prometer();

            PasarDias(1, DiasHastaLaBoda + 1);

            Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.Married),
                "prometidos tiene que llevar a casados sin que el jugador haga nada");
            Assert.That(EtapaDe(_leo, _ana), Is.EqualTo(RomanceStage.Married));
        }

        [Test]
        public void AntesDeLosOchoDiasTodaviaNoSeCasan()
        {
            Prometer();

            PasarDias(1, DiasHastaLaBoda - 1);

            Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.Engaged),
                "una boda no puede salir de la nada el primer día");
        }

        // ── 2. el aviso ──────────────────────────────────────────────────────

        [Test]
        public void LaBodaSeAnunciaConTresDiasDeAntelacion()
        {
            Prometer();

            int anuncios = 0;
            int diaAnunciado = 0;
            System.Action<WeddingAnnounced> escucha = e => { anuncios++; diaAnunciado = e.Day; };
            EventBus.Subscribe(escucha);

            try
            {
                PasarDias(1, DiasHastaLaBoda + 1);

                Assert.That(anuncios, Is.EqualTo(1), "se anuncia una vez, no una por día");
                // Prometidos el día 1, fecha puesta el día 6, boda el 9.
                Assert.That(diaAnunciado, Is.EqualTo(9));
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        [Test]
        public void LaBodaAvisaDeQueSeHaCelebrado()
        {
            Prometer();

            int bodas = 0;
            System.Action<WeddingHeld> escucha = _ => bodas++;
            EventBus.Subscribe(escucha);

            try
            {
                PasarDias(1, DiasHastaLaBoda + 3);
                Assert.That(bodas, Is.EqualTo(1), "una boda, y una sola");
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        // ── 3. lo que no debe pasar ──────────────────────────────────────────

        [Test]
        public void SiRompenAntesDeLaFechaNoHayBoda()
        {
            Prometer();
            PasarDias(1, 6);   // ya tienen fecha

            // Se les acaba el amor dos días antes.
            Poner(_ana, _leo, RomanceStage.None, 10f);
            Poner(_leo, _ana, RomanceStage.None, 10f);

            PasarDias(7, 6);

            Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.None),
                "una boda apuntada no puede casar a quien ya rompió");
            Assert.That(_planner.Bookings.Count, Is.EqualTo(0),
                "y la reserva se tira, no se queda ahí colgada");
        }

        [Test]
        public void UnCompromisoDeUnSoloLadoNoCuenta()
        {
            // Ana está prometida; Leo solo está saliendo. Nadie casa a medias.
            Poner(_ana, _leo, RomanceStage.Engaged, 90f);
            Poner(_leo, _ana, RomanceStage.Dating, 90f);

            PasarDias(1, DiasHastaLaBoda + 3);

            Assert.That(EtapaDe(_ana, _leo), Is.Not.EqualTo(RomanceStage.Married));
            Assert.That(_planner.Bookings.Count, Is.EqualTo(0));
        }

        [Test]
        public void UnaParejaSoloSeApuntaUnaVez()
        {
            Prometer();

            PasarDias(1, 3);

            Assert.That(_planner.Bookings.Count, Is.EqualTo(1),
                "mirar a la pareja desde los dos lados no puede dar dos reservas");
        }

        // ── 4. volver después de días fuera ──────────────────────────────────

        [Test]
        public void AlVolverDeVariosDiasFueraLaBodaSaleIgual()
        {
            Prometer();

            // El reloj publica un DayPassed por cada día saltado, así que ponerse al
            // día pasa por el mismo camino. Esto comprueba que no hace falta que
            // alguien esté mirando.
            PasarDias(1, 30);

            Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.Married));
        }

        [Test]
        public void PasarElMismoDiaDosVecesNoCasaDosVeces()
        {
            Prometer();
            PasarDias(1, DiasHastaLaBoda + 1);

            int bodas = 0;
            System.Action<WeddingHeld> escucha = _ => bodas++;
            EventBus.Subscribe(escucha);
            try
            {
                _planner.AdvanceDay(DiasHastaLaBoda + 1);
                _planner.AdvanceDay(DiasHastaLaBoda + 1);
                Assert.That(bodas, Is.Zero, "ya estaban casados; no hay segunda boda");
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        // ── 5. una partida vieja con parejas congeladas ──────────────────────

        [Test]
        public void UnaPartidaGuardadaConPrometidosViejosNoCasaATodosDeGolpe()
        {
            // Es el caso real: hay partidas con parejas prometidas desde hace semanas
            // porque nadie llamaba a TryMarry. Al cargar, la cuenta empieza hoy.
            Prometer();

            _planner.AdvanceDay(100);

            Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.Engaged),
                "el primer día que corre el planificador solo las apunta");
            Assert.That(_planner.Bookings.Count, Is.EqualTo(1));
            Assert.That(_planner.Bookings[0].HasDate, Is.False);
        }

        // ── 6. bebés ─────────────────────────────────────────────────────────

        [Test]
        public void DiezDiasDespuesDeLaBodaLlegaUnBebe()
        {
            Prometer();

            string hijo = null;
            System.Action<BabyBorn> escucha = e => hijo = e.ChildId;
            EventBus.Subscribe(escucha);

            try
            {
                // Sin IHomeUpgradeService registrado el requisito de la casa se salta,
                // que es lo que hace que este test hable solo de los días.
                PasarDias(1, DiasHastaLaBoda + 12);

                Assert.That(hijo, Is.Not.Null, "una pareja casada acaba teniendo familia");
                Assert.That(_registry.Count, Is.EqualTo(3));
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        [Test]
        public void UnaParejaNoTieneDosBebesSeguidos()
        {
            Prometer();

            int bebes = 0;
            System.Action<BabyBorn> escucha = _ => bebes++;
            EventBus.Subscribe(escucha);

            try
            {
                PasarDias(1, 60);
                Assert.That(bebes, Is.EqualTo(1),
                    "un hijo por pareja: si no, la isla se llena en dos semanas");
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        [Test]
        public void SinCasaAmpliadaNoHayBebe()
        {
            ServiceRegistry.Register<IHomeUpgradeService>(new CasasSinAmpliar());

            Prometer();

            int bebes = 0;
            System.Action<BabyBorn> escucha = _ => bebes++;
            EventBus.Subscribe(escucha);

            try
            {
                PasarDias(1, 40);
                Assert.That(bebes, Is.Zero,
                    "la casa ampliada es el enganche con la gestión: sin ella no crece la aldea");
                Assert.That(EtapaDe(_ana, _leo), Is.EqualTo(RomanceStage.Married),
                    "pero casados sí están, que eso no depende del jugador");
            }
            finally { EventBus.Unsubscribe(escucha); }
        }

        /// <summary>Nadie ha ampliado nada: todas las casas en el nivel de serie.</summary>
        private sealed class CasasSinAmpliar : IHomeUpgradeService
        {
            public int MaxLevel => 2;
            public int LevelOf(string islanderId) => 0;
            public long PriceOf(int level) => 1200;
            public System.Collections.Generic.IReadOnlyList<MaterialCost> MaterialsFor(int level)
                => new MaterialCost[0];
            public int SizeOfLevel(int level) => 8;
            public UpgradeRejection CanUpgrade(string islanderId) => UpgradeRejection.Ok;
            public bool Upgrade(string islanderId) => false;

            // La cabaña del protagonista no pinta nada en las bodas de los vecinos.
            public int PlayerLevel => 0;
            public UpgradeRejection CanUpgradePlayerHome() => UpgradeRejection.NoHome;
            public bool UpgradePlayerHome() => false;
        }
    }
}
