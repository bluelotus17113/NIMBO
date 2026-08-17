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
using Nimbo.Events.News;
using Nimbo.Events.Scheduling;
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
    /// Que el jugador se entere de lo que pasa en la aldea.
    /// </summary>
    /// <remarks>
    /// El tablón estaba escrito y probado, pero <c>EventsService</c> —lo único que lo
    /// construye— **no lo llamaba nadie**, así que en el juego no existía: los vecinos se
    /// hacían amigos, se prometían y se casaban y no había por dónde saberlo.
    ///
    /// Al enchufarlo salió el fallo que estos tests vigilan: casi todos los cambios de
    /// relación se publican **por los dos lados**, así que cada boda salía contada dos
    /// veces, y con dos plantillas distintas para más gracia.
    /// </remarks>
    public class CronicaTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private SocialService _social;
        private NewsBoard _board;
        private EventsConfig _config;

        private string _ana;
        private string _leo;

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
            _config = ScriptableObject.CreateInstance<EventsConfig>();

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

            _board = new NewsBoard(_config, _save, _clock);
        }

        [TearDown]
        public void TearDown()
        {
            _board?.Dispose();
            _social?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
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

        private int LineasQueDicen(string trozo)
        {
            int total = 0;
            for (int i = 0; i < _save.Chronicle.Count; i++)
                if (_save.Chronicle[i].Text.Contains(trozo)) total++;
            return total;
        }

        // ── el fallo del titular doble ───────────────────────────────────────

        [Test]
        public void UnaBodaSeCuentaUnaVez_NoDos()
        {
            // Casarse publica RomanceStageChanged por cada cónyuge. Es lo correcto —cada
            // uno cambia su propia ficha— pero contarlo es cosa de la pareja.
            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Married));
            EventBus.Publish(new RomanceStageChanged(_leo, _ana, RomanceStage.Married));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(1),
                "la misma boda contada desde los dos lados es una sola noticia");
        }

        [Test]
        public void UnaRinaSeCuentaUnaVez_NoDos()
        {
            EventBus.Publish(new ConflictStageChanged(_ana, _leo, ConflictStage.Quarrel));
            EventBus.Publish(new ConflictStageChanged(_leo, _ana, ConflictStage.Quarrel));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(1));
        }

        [Test]
        public void PeroLosDosFlechazosSiSonDosNoticias()
        {
            // Aquí las dos direcciones son la historia: que a Ana le guste Leo y que a
            // Leo le guste Ana son dos cosas distintas, y que pasen las dos es el chiste.
            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Crush));
            EventBus.Publish(new RomanceStageChanged(_leo, _ana, RomanceStage.Crush));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(2),
                "un flechazo es de uno hacia otro, no un estado de la pareja");
        }

        [Test]
        public void AvanzarDeEtapaSiVuelveAContarse()
        {
            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Dating));
            EventBus.Publish(new RomanceStageChanged(_leo, _ana, RomanceStage.Dating));
            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Engaged));
            EventBus.Publish(new RomanceStageChanged(_leo, _ana, RomanceStage.Engaged));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(2),
                "el filtro quita los repetidos, no los cambios de verdad");
        }

        // ── que se guarde ────────────────────────────────────────────────────

        [Test]
        public void LaCronicaSeEscribeEnLaPartida()
        {
            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Engaged));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(1),
                "sin esto la crónica se vacía justo al cerrar, que es cuando hace falta");
            Assert.That(_save.Chronicle[0].Day, Is.EqualTo(_clock.Day));
            Assert.That(_save.Chronicle[0].Text, Is.Not.Empty);
        }

        [Test]
        public void LaCronicaGuardaElNombreYNoElIdentificador()
        {
            string nombre = _registry.Get(_ana).Identity.ShortName;

            EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Married));

            Assert.That(_save.Chronicle[0].Text, Does.Contain(nombre));
            Assert.That(_save.Chronicle[0].Text, Does.Not.Contain(_ana),
                "el texto se escribe una vez; guardar ids dejaría en blanco al que se va");
        }

        [Test]
        public void LaCronicaNoCreceSinLimite()
        {
            // Muchas parejas distintas para que el filtro de repetidos no las coma.
            var factory = new IslanderFactory(_registry, PersonalityRoster.CreateService(),
                                              _clock, new List<string>());
            for (int i = 0; i < 200; i++)
            {
                var uno = factory.CreateRandom($"relleno-a-{i}");
                var otro = factory.CreateRandom($"relleno-b-{i}");
                _registry.Add(uno);
                _registry.Add(otro);
                EventBus.Publish(new RomanceStageChanged(uno.Id, otro.Id, RomanceStage.Married));
            }

            Assert.That(_save.Chronicle.Count, Is.LessThanOrEqualTo(150),
                "la crónica es memoria, no un vertedero: el guardado no puede crecer siempre");
        }

        [Test]
        public void ElTablonYLaCronicaSonDosListasDistintas()
        {
            // El tablón de la plaza es lo de ahora y caben veinte; la crónica es la
            // memoria de la aldea. Si fueran la misma lista, leer hacia atrás sería
            // imposible.
            Assert.That(_config.MaxHeadlines, Is.LessThan(150));
        }

        // ── sin partida, el tablón sigue funcionando ─────────────────────────

        [Test]
        public void SinPartidaElTablonNoRevienta()
        {
            var suelto = new NewsBoard(_config);
            try
            {
                EventBus.Publish(new RomanceStageChanged(_ana, _leo, RomanceStage.Married));

                Assert.That(suelto.Headlines.Count, Is.EqualTo(1), "el tablón sí escribe");
                Assert.That(suelto.Entries.Count, Is.Zero, "la crónica se queda vacía");
            }
            finally { suelto.Dispose(); }
        }

        // ── la boda que se anuncia ───────────────────────────────────────────

        [Test]
        public void LaBodaAnunciadaSaleEnLaCronicaConSuDia()
        {
            EventBus.Publish(new WeddingAnnounced(_ana, _leo, 42));

            Assert.That(_save.Chronicle.Count, Is.EqualTo(1));
            Assert.That(_save.Chronicle[0].Text, Does.Contain("42"),
                "el aviso sirve para poder ir, así que tiene que decir cuándo es");
        }

        // ── la prueba de que la cadena entera cierra ─────────────────────────

        [Test]
        public void UnaBodaDeVerdadAcabaEnLaCronica()
        {
            // Sin trampas: el planificador casa a la pareja y el tablón lo recoge del
            // EventBus, sin que nadie los presente. Es lo que antes no ocurría.
            var planner = new WeddingPlanner(_save, _registry, _social, _simulation);
            try
            {
                Poner(_ana, _leo, RomanceStage.Engaged, 90f);
                Poner(_leo, _ana, RomanceStage.Engaged, 90f);

                for (int day = 1; day <= 10; day++) planner.AdvanceDay(day);

                Assert.That(LineasQueDicen("casad"), Is.EqualTo(1).Or.EqualTo(0),
                    "la boda no puede contarse dos veces");
                Assert.That(_save.Chronicle.Count, Is.GreaterThanOrEqualTo(2),
                    "tiene que haber al menos el anuncio y la boda");

                bool hayAnuncio = false, hayBoda = false;
                for (int i = 0; i < _save.Chronicle.Count; i++)
                {
                    string t = _save.Chronicle[i].Text;
                    if (t.Contains("boda") || t.Contains("Boda") || t.Contains("casan")) hayAnuncio = true;
                    if (t.Contains("casado") || t.Contains("matrimonio") || t.Contains("juraron")) hayBoda = true;
                }

                Assert.That(hayAnuncio, Is.True, "falta el aviso de que hay boda");
                Assert.That(hayBoda, Is.True, "falta la boda misma");
            }
            finally { planner.Dispose(); }
        }
    }
}
