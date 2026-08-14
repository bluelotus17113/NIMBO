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
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Gifts;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Wardrobe;
using Nimbo.Social;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Lo que el protagonista puede hacer con un vecino: hablarle y darle cosas.
    /// </summary>
    /// <remarks>
    /// Antes no podía hacer ninguna de las dos. Acercarse y pulsar abría su ficha —lo
    /// mismo que clicar su nombre en la interfaz— y regalar solo existía para ropa y
    /// sacando de la despensa, que es un sitio al que el jugador no llega andando.
    /// Convivir era el pilar número uno del diseño y no tocaba nada.
    /// </remarks>
    public class ConvivenciaTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private SocialService _social;
        private EconomyService _economy;
        private ItemCatalog _catalog;
        private InventoryService _inventory;
        private GiftService _gifts;
        private Nimbo.Player.PlayerService _player;

        private string _vecino;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            _catalog = new ItemCatalog();
            var catalog = _catalog;
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();

            _simulation = new SimulationService(_registry, personalities, needsConfig);
            _economy = new EconomyService(catalog, _save);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, _simulation, factory,
                                        _clock, socialConfig);

            _player = new Nimbo.Player.PlayerService(_save.Player, _clock);
            _inventory = new InventoryService(_save.Player, _economy);

            var wardrobe = new WardrobeService(_registry, _simulation, personalities);
            _gifts = new GiftService(_registry, _simulation, personalities, _inventory, wardrobe);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_simulation);
            ServiceRegistry.Register<IEconomyService>(_economy);
            ServiceRegistry.Register<ISocialService>(_social);

            var vecino = factory.CreateRandom("vecino-de-prueba");
            _registry.Add(vecino);
            _save.Islanders.Add(vecino);
            _vecino = vecino.Id;
        }

        [TearDown]
        public void TearDown()
        {
            _social?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── hablar ───────────────────────────────────────────────────────────

        [Test]
        public void HablarleAUnVecinoSubeLoQueSienteporTi()
        {
            float antes = _social.PlayerRelationship(_vecino).Affinity;

            Assert.That(_social.PlayerInteract(_vecino, SocialInteraction.Chat), Is.True);

            Assert.That(_social.PlayerRelationship(_vecino).Affinity, Is.GreaterThan(antes),
                        "hablar con alguien no ha movido nada");
        }

        [Test]
        public void DejaDeContarCuandoYaHasHabladoBastantePorHoy()
        {
            // El tope existe para que no se pueda subir una amistad a tope repitiendo
            // «hablar» cuarenta veces sin moverse del sitio.
            int contadas = 0;
            for (int i = 0; i < 40; i++)
                if (_social.PlayerInteract(_vecino, SocialInteraction.Chat)) contadas++;

            Assert.That(contadas, Is.GreaterThan(0), "no ha contado ni una charla");
            Assert.That(contadas, Is.LessThan(40), "se puede charlar sin límite");
        }

        [Test]
        public void NadieSeEnamoraDelProtagonistaPorHablarle()
        {
            // Se le habla y se le regala hasta el tope, muchos días seguidos.
            for (int dia = 0; dia < 30; dia++)
            {
                for (int i = 0; i < 5; i++)
                    _social.PlayerInteract(_vecino, SocialInteraction.Gift);
                EventBus.Publish(new DayPassed(dia));
            }

            var conmigo = _social.PlayerRelationship(_vecino);
            Assert.That(conmigo.Romance, Is.EqualTo(RomanceStage.None),
                        "un vecino se ha enamorado del protagonista por acumular regalos");
        }

        [Test]
        public void ElProtagonistaNoSaleEnLaListaDeAmigosDeLaSimulacion()
        {
            // Los sueños y las peticiones resuelven nombres contra el censo, donde el
            // protagonista no está: si se colara ahí, saldría un amigo sin nombre.
            for (int i = 0; i < 40; i++)
                _social.PlayerInteract(_vecino, SocialInteraction.Gift);

            foreach (var amigo in _social.FriendsOf(_vecino))
                Assert.That(amigo.OtherId, Is.Not.EqualTo(SocialIds.Player));
        }

        // ── regalar ──────────────────────────────────────────────────────────

        [Test]
        public void UnaHerramientaNoSeRegala()
        {
            string azada = PrimeroDeCategoria(ItemCategory.Tool);
            Assert.That(azada, Is.Not.Null, "el catálogo no tiene herramientas");

            Assert.That(_gifts.IsGiftable(azada), Is.False,
                        "se puede regalar la azada y quedarse sin poder labrar");
        }

        [Test]
        public void RegalarSacaElObjetoDeLaMochila()
        {
            string flor = PrimeroDeCategoria(ItemCategory.Food);
            _inventory.TryStore(flor, 3, out _);
            _inventory.Select(0);

            Assert.That(_gifts.OfferHeld(_vecino, out _), Is.EqualTo(GiftResult.Ok));
            Assert.That(_inventory.CountOf(flor), Is.EqualTo(2), "no se ha descontado uno");
        }

        [Test]
        public void ConLaMochilaVaciaNoSeRegalaNada()
        {
            _inventory.Select(0);
            Assert.That(_gifts.OfferHeld(_vecino, out _), Is.EqualTo(GiftResult.EmptyHanded));
        }

        [Test]
        public void SoloSeAceptaUnRegaloAlDia_YElSegundoNoCuesta()
        {
            string comida = PrimeroDeCategoria(ItemCategory.Food);
            _inventory.TryStore(comida, 5, out _);
            _inventory.Select(0);

            Assert.That(_gifts.OfferHeld(_vecino, out _), Is.EqualTo(GiftResult.Ok));
            Assert.That(_gifts.OfferHeld(_vecino, out _), Is.EqualTo(GiftResult.AlreadyToday));

            // Y esto es lo que importa: el segundo intento no se ha comido nada. Si el
            // orden fuera al revés, el objeto se perdería a cambio de nada.
            Assert.That(_inventory.CountOf(comida), Is.EqualTo(4));
        }

        [Test]
        public void ElGustoPorUnaCosaNoCambiaDeUnDiaParaOtro()
        {
            string comida = PrimeroDeCategoria(ItemCategory.Food);

            float hoy = _gifts.LikingOf(_vecino, comida);
            EventBus.Publish(new DayPassed(1));
            float manana = _gifts.LikingOf(_vecino, comida);

            Assert.That(manana, Is.EqualTo(hoy).Within(0.0001f),
                        "el gusto se sortea cada vez: no se puede aprender qué le gusta");
        }

        // ── vigor ────────────────────────────────────────────────────────────

        [Test]
        public void ElVigorNoSeRellenaSoloAlPasarElDia()
        {
            _player.SpendVigor(60f);
            float cansado = _player.Vigor;

            // Un día entero de reloj, sin dormir ni comer.
            for (int i = 0; i < 24; i++)
            {
                _clock.Advance(60);
                _player.Tick();
            }

            Assert.That(_player.Vigor, Is.EqualTo(cansado).Within(0.01f),
                        "basta con esperar a medianoche: el vigor no decide nada");
        }

        [Test]
        public void DormirSiLoDevuelve()
        {
            _player.SpendVigor(60f);
            _player.RestoreVigor(Data.Player.PlayerState.MaxVigor);

            Assert.That(_player.Vigor,
                        Is.EqualTo(Data.Player.PlayerState.MaxVigor).Within(0.01f));
        }

        private string PrimeroDeCategoria(ItemCategory category)
        {
            for (int i = 0; i < _catalog.All.Count; i++)
                if (_catalog.All[i].Category == category) return _catalog.All[i].CatalogId;
            return null;
        }
    }
}
