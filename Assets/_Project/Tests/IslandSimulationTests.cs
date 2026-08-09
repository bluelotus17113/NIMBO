using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Island;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Behaviour;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Requests;
using Nimbo.Social;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Monta la isla entera sin escena y la deja correr días de juego. Es la prueba
    /// que de verdad importa: que doce módulos que compilan por separado hagan algo
    /// coherente cuando se les da cuerda juntos.
    /// </summary>
    public class IslandSimulationTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private RequestService _requests;
        private SocialService _social;
        private IslandService _island;
        private IslanderBrain _brain;
        private EconomyService _economy;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _save.Wallet.Coins = 200;

            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            var catalog = new ItemCatalog();

            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var requestConfig = ScriptableObject.CreateInstance<RequestConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();

            _simulation = new SimulationService(_registry, personalities, needsConfig);
            _economy = new EconomyService(catalog, _save);
            _island = new IslandService(_registry, _save);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, _simulation, factory,
                                        _clock, socialConfig);

            var generator = new RequestGenerator(_registry, personalities, requestConfig);
            _requests = new RequestService(_registry, _simulation, generator, requestConfig, _clock);
            _brain = new IslanderBrain(_registry, _island, personalities, _social, _clock);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_simulation);

            // Seis habitantes: suficiente para abrir la mitad de la isla y para que
            // pasen cosas entre ellos.
            for (int i = 0; i < 6; i++)
            {
                var islander = factory.CreateRandom($"prueba-{i}");
                _registry.Add(islander);
                _save.Islanders.Add(islander);
                EventBus.Publish(new IslanderCreated(islander.Id));
                _island.TryAssignHome(islander);
            }

            var all = _registry.All;
            for (int a = 0; a < all.Count; a++)
                for (int b = a + 1; b < all.Count; b++)
                    _social.Introduce(all[a].Id, all[b].Id);
        }

        [TearDown]
        public void TearDown()
        {
            _brain?.Dispose();
            _requests?.Dispose();
            _social?.Dispose();
            _island?.Dispose();
            _simulation?.Dispose();
            _economy?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();

            foreach (var path in new[] { Nimbo.Core.Save.SaveSystem.SavePath,
                                         Nimbo.Core.Save.SaveSystem.BackupPath })
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private void SimulateDays(int days) => _clock.Advance(days * GameClock.MinutesPerDay);

        [Test]
        public void NadieCompartePorLoQueSeLeLlama()
        {
            // La lista de habitantes enseña ShortName, que es el apodo cuando lo hay.
            // Una isla con dos «Cometa» tiene dos botones idénticos para dos vecinos
            // distintos, y el jugador no sabe a cuál está entrando. Que el nombre de
            // pila fuera único no bastaba: no es el que se ve.
            var seen = new HashSet<string>();

            foreach (var islander in _registry.All)
            {
                string shown = islander.Identity.ShortName;
                Assert.IsNotEmpty(shown, "un habitante sin nada que enseñar");
                Assert.IsTrue(seen.Add(shown), $"dos habitantes se enseñan como «{shown}»");
            }
        }

        [Test]
        public void LaIslaArrancaConSeisHabitantesConCasa()
        {
            Assert.AreEqual(6, _registry.Count);
            foreach (var islander in _registry.All)
                Assert.IsTrue(islander.Home.HasHome,
                              $"{islander.Identity.DisplayName} se quedó sin casa");
        }

        [Test]
        public void TrasSieteDiasNadieSeHaQuedadoAtascadoEnRojo()
        {
            SimulateDays(7);

            foreach (var islander in _registry.All)
            {
                // Nadie tiene por qué estar lleno todo el rato, pero una necesidad
                // clavada a cero una semana entera significa que su bucle no cierra.
                Assert.Greater(islander.Needs.Energy, 0f,
                    $"{islander.Identity.DisplayName} lleva una semana sin dormir");
                Assert.That(islander.Mood.Happiness, Is.InRange(0f, 100f));
            }
        }

        [Test]
        public void ElReparteEmocionesYNadieSeQuedaCongelado()
        {
            SimulateDays(3);

            int moving = 0;
            foreach (var islander in _registry.All)
                if (islander.Activity != IslanderActivity.Idle) moving++;

            Assert.Greater(moving, 0, "nadie está haciendo nada tras tres días");
        }

        [Test]
        public void SalenPeticionesYAlgunaCaduca()
        {
            int raised = 0, expired = 0;
            EventBus.Subscribe<RequestRaised>(_ => raised++);
            EventBus.Subscribe<RequestExpired>(_ => expired++);

            SimulateDays(4);

            Assert.Greater(raised, 0, "en cuatro días nadie pidió nada");
            Assert.Greater(expired, 0, "nada caducó: la cola no penaliza el abandono");
            Assert.LessOrEqual(_requests.OpenCount, 6 * 3,
                               "hay más peticiones abiertas que el máximo por habitante");
        }

        [Test]
        public void LasRelacionesSeMuevenSolas()
        {
            SimulateDays(5);

            int withOpinion = 0;
            foreach (var islander in _registry.All)
                foreach (var record in islander.Relationships.Records)
                    if (!Mathf.Approximately(record.Affinity, 0f)) withOpinion++;

            Assert.Greater(withOpinion, 0,
                           "cinco días juntos y nadie se ha formado una opinión de nadie");
        }

        [Test]
        public void LaAfinidadNuncaSeSaleDeRango()
        {
            SimulateDays(14);

            foreach (var islander in _registry.All)
                foreach (var record in islander.Relationships.Records)
                    Assert.That(record.Affinity,
                        Is.InRange(RelationshipRecord.MinAffinity, RelationshipRecord.MaxAffinity),
                        $"{islander.Identity.DisplayName} → {record.OtherId}");
        }

        [Test]
        public void LasZonasSeAbrenAlLlegarGente()
        {
            // Con seis habitantes toca todo lo de «3 isleños» y «5 isleños».
            Assert.IsTrue(_island.IsUnlocked("zona_plaza"));
            Assert.IsTrue(_island.IsUnlocked("zona_tienda_muebles"), "debería abrir con 3");
            Assert.IsTrue(_island.IsUnlocked("zona_parque"), "debería abrir con 5");

            // Y no lo que exige doce y un evento.
            Assert.IsFalse(_island.IsUnlocked("zona_embarcadero"));
        }

        [Test]
        public void LaPartidaSobreviveAUnaVueltaCompletaDeGuardado()
        {
            SimulateDays(3);

            _save.ElapsedMinutes = _clock.ElapsedMinutes;
            _save.Requests = new List<Data.Requests.IslanderRequest>(_requests.Open);

            // Por el camino de verdad, no por un serializador aparte: lo que hay que
            // probar es que la partida que escribe el juego se vuelve a leer entera.
            Assert.IsTrue(Nimbo.Core.Save.SaveSystem.Write(_save));
            var loaded = Nimbo.Core.Save.SaveSystem.Read();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(_save.Islanders.Count, loaded.Islanders.Count);
            Assert.AreEqual(_save.ElapsedMinutes, loaded.ElapsedMinutes);

            for (int i = 0; i < _save.Islanders.Count; i++)
            {
                var before = _save.Islanders[i];
                var after = loaded.Islanders[i];
                Assert.AreEqual(before.Id, after.Id);
                Assert.AreEqual(before.Personality.TypeIndex, after.Personality.TypeIndex);
                Assert.AreEqual(before.Relationships.Records.Count,
                                after.Relationships.Records.Count,
                                "se perdieron relaciones al guardar");
                Assert.AreEqual(before.Needs.Hunger, after.Needs.Hunger, 0.01f);
            }
        }

        [Test]
        public void ComprarDescuentaYMeteEnElInventario()
        {
            var food = _economy.ItemsOfCategory(ItemCategory.Food);
            IItemDefinition cheap = null;
            foreach (var item in food)
                if (item.UnlockLevel <= 1 && (cheap == null || item.Price < cheap.Price))
                    cheap = item;

            Assert.IsNotNull(cheap, "no hay nada comprable al nivel 1");

            long before = _economy.Wallet.Coins;
            Assert.IsTrue(_economy.TryBuy(cheap.CatalogId));
            Assert.AreEqual(before - cheap.Price, _economy.Wallet.Coins);
            Assert.AreEqual(1, _economy.Inventory.CountOf(cheap.CatalogId));
        }
    }
}
