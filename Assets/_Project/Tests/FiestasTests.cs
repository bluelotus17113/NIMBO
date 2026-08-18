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
using Nimbo.Events.Scheduling;
using Nimbo.Island;
using Nimbo.Island.Zones;
using Nimbo.Personality.Runtime;
using Nimbo.Player;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El jugador monta una fiesta, y de la fiesta sale algo (§15.3).
    /// </summary>
    /// <remarks>
    /// Es la mejor herramienta social que se le puede dar sin romper la autonomía de
    /// nadie: no empareja a dos personas, monta el sitio donde se conocen. Lo que se
    /// protege aquí es sobre todo el límite — **un flechazo por fiesta como mucho**—,
    /// porque tirando por cada par de asistentes media aldea se enamoraría el mismo
    /// sábado y eso no es una fiesta memorable, es un sorteo.
    /// </remarks>
    public class FiestasTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private EventScheduler _calendario;
        private VillageEvents _fiestas;
        private EventSparks _chispas;
        private EconomyService _economia;
        private SocialService _social;
        private PlayerProgressionService _vias;

        /// <summary>Una fiesta pequeña de las que no piden ni zona ni mucho nivel.</summary>
        private const string Pequeña = "rainy_day";

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 10 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            var catalog = new ItemCatalog();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();
            var eventsConfig = ScriptableObject.CreateInstance<EventsConfig>();

            var simulation = new SimulationService(_registry, personalities, needsConfig);
            _economia = new EconomyService(catalog, _save);
            var island = new IslandService(_registry, _save);
            _vias = new PlayerProgressionService(_save.Player);

            var factory = new IslanderFactory(_registry, personalities, _clock,
                                              new List<string>());
            _social = new SocialService(_registry, personalities, simulation, factory,
                                        _clock, socialConfig, _save.Triangles, _save.Weddings);

            _calendario = new EventScheduler(eventsConfig);
            _fiestas = new VillageEvents(_calendario, _clock);
            _chispas = new EventSparks(_calendario);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(simulation);
            ServiceRegistry.Register<IPersonalityService>(personalities);
            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IIslandService>(island);
            ServiceRegistry.Register<ISocialService>(_social);
            ServiceRegistry.Register<IPlayerProgression>(_vias);
            ServiceRegistry.Register<IVillageEvents>(_fiestas);

            for (int i = 0; i < 4; i++)
            {
                var vecino = factory.CreateRandom($"vecino-{i}");
                _registry.Add(vecino);
                _save.Islanders.Add(vecino);
            }

            _economia.AddCoins(5000, "prueba");

            // Hasta la fiesta más humilde pide nivel de isla 2, y una isla recién
            // empezada está a 1. Se sube por donde se sube en el juego —los vecinos van
            // subiendo de nivel— en vez de escribir el número a mano: así la prueba
            // también comprueba que el nivel de isla se recalcula.
            SubirLaIsla();
        }

        /// <summary>Deja la isla en nivel alto subiendo a sus vecinos, como en el juego.</summary>
        private void SubirLaIsla()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var progression = all[i].Progression;
                progression.Level = 30;
                all[i].Progression = progression;
            }

            // El servicio de isla recalcula al enterarse de que alguien ha subido.
            EventBus.Publish(new IslanderLeveledUp(all[0].Id, 30));
        }

        [TearDown]
        public void TearDown()
        {
            _chispas?.Dispose();
            _calendario?.Dispose();
            _vias?.Dispose();
            _social?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── qué se puede pedir ───────────────────────────────────────────────

        [Test]
        public void LosEventosDisparadosNoSePiden()
        {
            foreach (var fiesta in _fiestas.Hostable)
                Assert.That(fiesta.Id, Is.Not.EqualTo("birthday"),
                    "pagar por un cumpleaños sería comprarle el cumpleaños a alguien");
        }

        [Test]
        public void LosFestivalesCuestanMasQueUnaMerienda()
        {
            long pequeña = 0, festival = 0;
            foreach (var f in _fiestas.Hostable)
            {
                if (f.IsFestival) festival = f.Cost;
                else pequeña = f.Cost;
            }

            Assert.That(festival, Is.GreaterThan(pequeña));
        }

        // ── las puertas ──────────────────────────────────────────────────────

        [Test]
        public void SinAldeaCincoNoSeOrganizaNada()
        {
            Assert.That(_fiestas.CanHost(Pequeña), Is.EqualTo(HostRefusal.NotUnlocked));
        }

        [Test]
        public void ConAldeaCincoSiYNoHaceFaltaMas()
        {
            SubirAldea(5);

            Assert.That(_fiestas.CanHost(Pequeña), Is.EqualTo(HostRefusal.Ok));
        }

        [Test]
        public void LosFestivalesPidenAldeaSiete()
        {
            SubirAldea(5);

            foreach (var f in _fiestas.Hostable)
            {
                if (!f.IsFestival) continue;
                Assert.That(_fiestas.CanHost(f.Id), Is.Not.EqualTo(HostRefusal.Ok),
                    $"{f.DisplayName} es un festival y se ha colado con Aldea 5");
            }
        }

        [Test]
        public void SinMonedasNoSeMonta()
        {
            SubirAldea(5);
            _economia.TrySpend(_economia.Wallet.Coins, "vaciar el monedero");

            Assert.That(_fiestas.CanHost(Pequeña), Is.EqualTo(HostRefusal.NotEnoughCoins));
        }

        [Test]
        public void PagarNoAbreUnSitioQueNoEsta()
        {
            SubirAldea(7);

            // La llegada del visitante pide el embarcadero, que en esta isla no está
            // abierto: hacen falta doce vecinos y una bandera de suceso.
            Assert.That(_fiestas.CanHost("visitor_arrival"), Is.EqualTo(HostRefusal.NotYet),
                "los requisitos del propio evento siguen mandando: pagar no abre un " +
                "embarcadero que no está construido");
        }

        [Test]
        public void LasZonasQuePidenLosEventosExisten()
        {
            // Tres eventos pedían zonas con nombres que no existen —«stage» y
            // «plaza_central» en vez de «zona_escenario» y «zona_plaza»— así que
            // `IsUnlocked` decía que no para siempre: el concierto, el festival y el
            // concurso de talentos **no podían ocurrir nunca**, ni sorteados ni pagados.
            // Y con ellos se caía el minijuego de ritmo, que solo se puede jugar si hay
            // concierto.
            var zonas = new HashSet<string>();
            foreach (var zone in IslandLayout.FirstIsland()) zonas.Add(zone.ZoneId);

            foreach (var def in EventCalendar.All)
            {
                if (string.IsNullOrEmpty(def.RequiredZone)) continue;

                Assert.That(zonas, Does.Contain(def.RequiredZone),
                    $"«{def.DisplayName}» pide la zona «{def.RequiredZone}», que no existe " +
                    "en el plano de la isla: ese evento no puede pasar nunca");
            }
        }

        // ── montarla ─────────────────────────────────────────────────────────

        [Test]
        public void MontarlaLaPoneEnMarchaYCuesta()
        {
            SubirAldea(5);
            long antes = _economia.Wallet.Coins;

            Assert.That(_fiestas.Host(Pequeña), Is.True);

            Assert.That(_fiestas.ActiveEventId, Is.EqualTo(Pequeña));
            Assert.That(_economia.Wallet.Coins, Is.LessThan(antes),
                "si organizar saliera gratis, habría fiesta todos los días y dejarían " +
                "de ser fiestas");
        }

        [Test]
        public void NoSeMontanDosALaVez()
        {
            SubirAldea(5);
            _fiestas.Host(Pequeña);

            Assert.That(_fiestas.CanHost(Pequeña), Is.EqualTo(HostRefusal.AlreadyRunning));
        }

        // ── lo que sale de ella ──────────────────────────────────────────────

        [Test]
        public void HaberEstadoEnLoMismoAcercaATodos()
        {
            var a = _registry.All[0].Id;
            var b = _registry.All[1].Id;
            _social.Introduce(a, b);
            float antes = _social.GetRelationship(a, b).Affinity;

            Celebrar();

            Assert.That(_social.GetRelationship(a, b).Affinity, Is.GreaterThan(antes),
                "es la mitad silenciosa de una fiesta, y la que se nota a las semanas");
        }

        [Test]
        public void DeUnaFiestaSaleUnFlechazoComoMucho()
        {
            // Todos se adoran y se parecen: si fuera por tiradas, se enamorarían todos
            // a la vez y la aldea entera cambiaría de estado el mismo sábado.
            QuererseMucho();

            Celebrar();

            Assert.That(ContarFlechazos(), Is.LessThanOrEqualTo(1),
                "media aldea enamorada de golpe no es una fiesta memorable, es un sorteo");
        }

        [Test]
        public void YEseFlechazoEsDeUnoSolo()
        {
            QuererseMucho();
            Celebrar();

            if (ContarFlechazos() == 0) Assert.Ignore("esta vez no saltó ninguna chispa");

            foreach (var uno in _registry.All)
            {
                foreach (var record in uno.Relationships.Records)
                {
                    if (record.Romance != RomanceStage.Crush) continue;
                    if (!_registry.TryGet(record.OtherId, out var otro)) continue;

                    otro.Relationships.TryGet(uno.Id, out var vuelta);
                    Assert.That(vuelta.Romance, Is.Not.EqualTo(RomanceStage.Crush),
                        "un flechazo correspondido de golpe se salta el tramo más bonito: " +
                        "el rato en que uno de los dos todavía no lo sabe");
                }
            }
        }

        [Test]
        public void SinNadieCompatibleNoPasaNada()
        {
            // Sin presentarse ni quererse, no hay chispa que valga.
            Celebrar();

            Assert.That(ContarFlechazos(), Is.Zero);
        }

        // ── utilidades ───────────────────────────────────────────────────────

        private void SubirAldea(int nivel)
        {
            for (int n = 1; n < nivel; n++)
                _vias.Grant(Data.Player.SkillKind.Village, Data.Player.SkillSet.XpForLevel(n));
        }

        /// <summary>Monta la fiesta y la termina, que es cuando salta la chispa.</summary>
        private void Celebrar()
        {
            SubirAldea(5);
            Assert.That(_fiestas.Host(Pequeña), Is.True, "no se ha podido montar");
            _calendario.FinishActiveEvent();
        }

        private void QuererseMucho()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                // Todos con la misma personalidad: compatibilidad al máximo.
                all[i].Personality = Data.Islanders.PersonalityProfile.FromTypeIndex(0);

                for (int j = 0; j < all.Count; j++)
                {
                    if (i == j) continue;

                    var record = all[i].Relationships.GetOrCreate(all[j].Id);
                    record.Affinity = 90f;
                    record.Friendship = FriendshipStage.CloseFriend;
                    record.Interactions = 20;
                    all[i].Relationships.Set(record);
                }
            }
        }

        private int ContarFlechazos()
        {
            int total = 0;
            foreach (var uno in _registry.All)
                foreach (var record in uno.Relationships.Records)
                    if (record.Romance == RomanceStage.Crush) total++;
            return total;
        }
    }
}
