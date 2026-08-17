using System;
using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Data.Islanders;
using Nimbo.Events;
using Nimbo.Events.News;
using Nimbo.Events.Scheduling;
using NUnit.Framework;
using UnityEngine;
using Nimbo.Data.World;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Social;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests para el módulo de eventos. Simulan un entorno mínimo sin Unity:
    /// usan el EventBus y ServiceRegistry reales (estáticos) y registran
    /// dobles de los servicios que el módulo consulta.
    /// </summary>
    public class EventsTests
    {
        EventsConfig _config;
        FakeRegistry _registry;
        FakeIslandService _island;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EventsConfig>();
            _config.MaxHeadlines = 20;
            _config.ConcertMaxAudience = 4;
            _config.BaseChancePerHour = 1f; // forzar que siempre salga si hay elegibles
            _config.CooldownHours = 0;

            _registry = new FakeRegistry();
            _island = new FakeIslandService();

            ServiceRegistry.Clear();
            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<IIslandService>(_island);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // -------------------------------------------------------------------
        // 1. Calendario: ≥12 eventos, sin IDs repetidos
        // -------------------------------------------------------------------

        [Test]
        public void Calendar_HasAtLeast12Events()
        {
            Assert.GreaterOrEqual(EventCalendar.All.Count, 12,
                "el calendario debe tener al menos 12 eventos distintos");
        }

        [Test]
        public void Calendar_NoDuplicateIds()
        {
            var ids = new HashSet<string>();
            foreach (var def in EventCalendar.All)
            {
                Assert.IsFalse(ids.Contains(def.Id),
                    $"id duplicado en el calendario: '{def.Id}'");
                ids.Add(def.Id);
            }
        }

        [Test]
        public void Calendar_AllHaveRequiredFields()
        {
            foreach (var def in EventCalendar.All)
            {
                Assert.IsNotNull(def.Id, "un evento tiene Id null");
                Assert.IsNotEmpty(def.Id, "un evento tiene Id vacío");
                Assert.IsNotNull(def.DisplayName, $"'{def.Id}' no tiene DisplayName");
                // Una ventana puede cruzar la medianoche (22 → 6), así que lo que
                // se exige no es que fin > inicio, sino que las dos horas sean horas
                // y que la ventana no esté vacía.
                Assert.That(def.StartHour, Is.InRange(0, 24), $"'{def.Id}': StartHour fuera de rango");
                Assert.That(def.EndHour, Is.InRange(0, 24), $"'{def.Id}': EndHour fuera de rango");
                Assert.AreNotEqual(def.StartHour, def.EndHour,
                    $"'{def.Id}': la ventana horaria está vacía");
            }
        }

        // -------------------------------------------------------------------
        // 2. Zona cerrada → evento no elegible
        // -------------------------------------------------------------------

        [Test]
        public void Scheduler_EventRequiringLockedZone_IsNotEligible()
        {
            _registry.AddIslander("alba", "Alba", 5);
            _registry.AddIslander("leo", "Leo", 5);
            _registry.AddIslander("mia", "Mia", 5);
            _island.Level = 5;

            // El escenario no está desbloqueado
            _island.UnlockedBuildings.Clear();

            int eventsStarted = 0;
            using (var scheduler = new EventScheduler(_config))
            {
                scheduler.EventStarted += (def, h) => eventsStarted++;

                // Disparar HourPassed en sábado a las 18h (ventana de concierto)
                _island.UnlockedBuildings.Clear(); // zona "stage" cerrada
                EventBus.Publish(new HourPassed(18, 6)); // día 6 = sábado
            }

            Assert.AreEqual(0, eventsStarted,
                "el concierto no debe programarse si el escenario está cerrado");
        }

        // -------------------------------------------------------------------
        // 3. Evento en marcha → no arranca otro
        // -------------------------------------------------------------------

        [Test]
        public void Scheduler_WithActiveEvent_DoesNotStartAnother()
        {
            _registry.AddIslander("alba", "Alba", 5);
            _registry.AddIslander("leo", "Leo", 5);
            _registry.AddIslander("mia", "Mia", 5);
            _island.Level = 5;
            _island.UnlockedBuildings.Add("stage");

            var started = new List<string>();
            using (var scheduler = new EventScheduler(_config))
            {
                scheduler.EventStarted += (def, h) => started.Add(def.Id);

                // Sábado 18h — debería salir un evento de concierto
                EventBus.Publish(new HourPassed(18, 6));
                Assert.GreaterOrEqual(started.Count, 1, "debería haber arrancado un evento");

                // Misma hora, segundo intento — no debe añadir otro
                int countAfter = started.Count;
                EventBus.Publish(new HourPassed(18, 6));
                Assert.AreEqual(countAfter, started.Count,
                    "con un evento activo no debe arrancar otro en la misma hora");
            }
        }

        // -------------------------------------------------------------------
        // 4. Cumpleaños: el día que toca y no el anterior
        // -------------------------------------------------------------------

        [Test]
        public void Birthday_FiresOnCorrectDay()
        {
            // Alba nació el 05-03 → día de juego = (3-1)*30 + 5 = 65
            var alba = MakeIslander("alba", "Alba", "03-05", 1);
            _registry.Add(alba);
            _island.Level = 1;

            string birthdayEvent = null;
            using (var scheduler = new EventScheduler(_config))
            {
                scheduler.EventStarted += (def, h) =>
                {
                    if (def.Id == "birthday") birthdayEvent = def.Id;
                };

                // Día 64 (un día antes): no debería saltar
                EventBus.Publish(new DayPassed(64));
                Assert.IsNull(birthdayEvent, "el cumpleaños no debe saltar el día anterior");

                // Día 65: debería saltar
                EventBus.Publish(new DayPassed(65));
                Assert.AreEqual("birthday", birthdayEvent,
                    "el cumpleaños debe saltar exactamente el día que toca");
            }
        }

        [Test]
        public void Birthday_DoesNotFireOnWrongDay()
        {
            var alba = MakeIslander("alba", "Alba", "12-25", 1);
            _registry.Add(alba);
            _island.Level = 1;

            int birthdayCount = 0;
            using (var scheduler = new EventScheduler(_config))
            {
                scheduler.EventStarted += (def, h) =>
                {
                    if (def.Id == "birthday") birthdayCount++;
                };

                // Día aleatorio que no es su cumpleaños
                EventBus.Publish(new DayPassed(100));
            }

            Assert.AreEqual(0, birthdayCount,
                "el cumpleaños no debe saltar un día que no coincide");
        }

        // -------------------------------------------------------------------
        // 5. Tablón: máximo 20 titulares
        // -------------------------------------------------------------------

        // Estos dos llenaban el tablón publicando **la misma pareja empezando a salir
        // veinticinco veces**. El tablón lo aceptaba, así que el test pasaba, pero eso
        // no puede ocurrir en el juego: una pareja empieza a salir una vez. Y desde que
        // el tablón filtra los repetidos —hacía falta, porque casarse se publica por los
        // dos lados y cada boda salía contada dos veces— el dato de entrada dejó de
        // producir titulares. El límite sigue mereciendo prueba; se llena con parejas
        // distintas, que es como se llena de verdad.

        /// <summary>Apunta a dos vecinos nuevos y los pone a salir. Un titular.</summary>
        void UnRomanceMas(int n)
        {
            _registry.AddIslander($"vecino{n}a", $"Vecin{n}a", 5);
            _registry.AddIslander($"vecino{n}b", $"Vecin{n}b", 5);
            EventBus.Publish(new RomanceStageChanged($"vecino{n}a", $"vecino{n}b",
                                                     RomanceStage.Dating));
        }

        [Test]
        public void NewsBoard_CapsAtMaxHeadlines()
        {
            using (var news = new NewsBoard(_config))
            {
                for (int i = 0; i < 25; i++) UnRomanceMas(i);

                Assert.LessOrEqual(news.Headlines.Count, _config.MaxHeadlines,
                    $"el tablón debe tener como mucho {_config.MaxHeadlines} titulares");
                Assert.AreEqual(_config.MaxHeadlines, news.Headlines.Count,
                    "los titulares sobrantes deben haberse eliminado");
            }
        }

        [Test]
        public void NewsBoard_OldestHeadlineRemovedFirst()
        {
            _config.MaxHeadlines = 5;

            using (var news = new NewsBoard(_config))
            {
                // Seis titulares con el tope en cinco: el primero tiene que caerse.
                for (int i = 0; i < 6; i++)
                {
                    EventBus.Publish(new HourPassed(i, 1)); // avanza el reloj
                    UnRomanceMas(i);
                }

                Assert.AreEqual(5, news.Headlines.Count);

                string all = string.Join(" ", news.Headlines.Select(h => h.Text));
                Assert.IsFalse(all.Contains("Vecin0a"),
                    "el titular más viejo es el que se va");
                Assert.IsTrue(all.Contains("Vecin5a"),
                    "y el más nuevo se queda");
            }
        }

        [Test]
        public void NewsBoard_LaMismaBodaPorLosDosLadosEsUnSoloTitular()
        {
            _registry.AddIslander("alba", "Alba", 5);
            _registry.AddIslander("leo", "Leo", 5);

            using (var news = new NewsBoard(_config))
            {
                // Es lo que hace SocialService.Wed: un aviso por cónyuge.
                EventBus.Publish(new RomanceStageChanged("alba", "leo", RomanceStage.Married));
                EventBus.Publish(new RomanceStageChanged("leo", "alba", RomanceStage.Married));

                Assert.AreEqual(1, news.Headlines.Count,
                    "una boda es una noticia, aunque la cuenten los dos novios");
            }
        }

        // -------------------------------------------------------------------
        // 6. Titular contiene nombre del habitante
        // -------------------------------------------------------------------

        [Test]
        public void NewsBoard_HeadlineContainsIslanderName()
        {
            _registry.AddIslander("alba", "Alba", 5);
            _registry.AddIslander("leo", "Leo", 5);

            using (var news = new NewsBoard(_config))
            {
                EventBus.Publish(new RomanceStageChanged("alba", "leo", RomanceStage.Dating));
                Assert.Greater(news.Headlines.Count, 0);
                string text = news.Headlines[news.Headlines.Count - 1].Text;
                Assert.IsTrue(text.Contains("Alba") || text.Contains("Leo"),
                    $"el titular '{text}' debe contener el nombre de al menos un implicado");
            }
        }

        [Test]
        public void NewsBoard_LevelUpContainsName()
        {
            _registry.AddIslander("alba", "Alba", 10);

            using (var news = new NewsBoard(_config))
            {
                EventBus.Publish(new IslanderLeveledUp("alba", 10));
                Assert.Greater(news.Headlines.Count, 0);
                string text = news.Headlines[news.Headlines.Count - 1].Text;
                Assert.IsTrue(text.Contains("Alba"),
                    $"el titular de subida de nivel '{text}' debe contener el nombre del isleño");
            }
        }

        // -------------------------------------------------------------------
        // 7. DayName y BirthdayString
        // -------------------------------------------------------------------

        [Test]
        public void DayName_Day1_IsMonday()
        {
            Assert.AreEqual("monday", EventScheduler.DayName(1));
        }

        [Test]
        public void DayName_Day7_IsSunday()
        {
            Assert.AreEqual("sunday", EventScheduler.DayName(7));
        }

        [Test]
        public void BirthdayString_Day1_IsJanuary1()
        {
            Assert.AreEqual("01-01", EventScheduler.BirthdayString(1));
        }

        [Test]
        public void IsBirthdayToday_MatchesCorrectly()
        {
            var alba = MakeIslander("alba", "Alba", "02-15", 1);
            // 02-15 → día (2-1)*30 + 15 = 45
            Assert.IsTrue(EventScheduler.IsBirthdayToday(alba, 45));
            Assert.IsFalse(EventScheduler.IsBirthdayToday(alba, 44));
            Assert.IsFalse(EventScheduler.IsBirthdayToday(alba, 46));
        }

        // -------------------------------------------------------------------
        // helpers
        // -------------------------------------------------------------------

        IslanderData MakeIslander(string id, string name, string birthday, int level)
        {
            return new IslanderData
            {
                Identity = new IslanderIdentity
                {
                    Id = id,
                    DisplayName = name,
                    Birthday = birthday,
                },
                Personality = new PersonalityProfile(0.5f, 0.5f, 0.5f, 0.5f),
                Progression = new ProgressionState { Level = level },
            };
        }

        // -------------------------------------------------------------------
        // fakes
        // -------------------------------------------------------------------

        class FakeRegistry : IIslanderRegistry
        {
            readonly List<IslanderData> _all = new List<IslanderData>();
            readonly Dictionary<string, IslanderData> _byId = new Dictionary<string, IslanderData>();

            public IReadOnlyList<IslanderData> All => _all;
            public int Count => _all.Count;

            public void Add(IslanderData islander)
            {
                _all.Add(islander);
                _byId[islander.Id] = islander;
            }

            public void AddIslander(string id, string name, int level)
            {
                Add(new IslanderData
                {
                    Identity = new IslanderIdentity { Id = id, DisplayName = name, Birthday = "01-01" },
                    Personality = new PersonalityProfile(0.5f, 0.5f, 0.5f, 0.5f),
                    Progression = new ProgressionState { Level = level },
                });
            }

            public IslanderData Get(string id) => _byId.TryGetValue(id, out var i) ? i : null;
            public bool TryGet(string id, out IslanderData islander) => _byId.TryGetValue(id, out islander);
            public bool Exists(string id) => _byId.ContainsKey(id);
            public IEnumerable<IslanderData> InZone(string zoneId) => _all;
            public void Remove(string id) { _all.RemoveAll(i => i.Id == id); _byId.Remove(id); }
        }

        class FakeIslandService : IIslandService
        {
            public IslandState State => new IslandState { Level = Level };
            public int Level;
            public List<string> UnlockedBuildings = new List<string>();
            public IReadOnlyList<string> ZoneIds => UnlockedBuildings;

            public ZonePurpose PurposeOf(string zoneId) => ZonePurpose.Social;
            public bool TryGetSpawnPoint(string zoneId, out Vector3 position) { position = Vector3.zero; return true; }
            public IEnumerable<string> ReachableFrom(string zoneId) { yield break; }
            public bool IsUnlocked(string buildingId) => UnlockedBuildings.Contains(buildingId);
            public bool Unlock(string buildingId) { UnlockedBuildings.Add(buildingId); return true; }
            public void SendTo(string islanderId, string zoneId) { }
        }
    }
}
