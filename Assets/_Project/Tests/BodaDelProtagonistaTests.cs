using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Crafting;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Farming;
using Nimbo.Housing;
using Nimbo.Island;
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Player;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using Nimbo.Social.Romance;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La boda del protagonista (§14.5): tres requisitos, uno de cada mitad del juego.
    /// </summary>
    /// <remarks>
    /// Era el sitio donde el juego se paraba: podías salir con alguien y ahí se
    /// acababa. Lo que se comprueba aquí es sobre todo que **los tres requisitos son
    /// tres** —el tiempo, el anillo y la casa— porque cada uno viene de una mitad
    /// distinta y saltarse cualquiera convertiría la boda en el final de una sola de
    /// ellas.
    /// </remarks>
    public class BodaDelProtagonistaTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SocialService _social;
        private EconomyService _economia;
        private InventoryService _mochila;
        private HomeUpgradeService _casas;
        private PlayerProgressionService _vias;
        private ConductService _conducta;
        private FarmingService _huerto;

        private string _bea;

        /// <summary>Los días que hay que llevar saliendo, y los de aviso de la boda.</summary>
        private const int DiasSaliendo = 10;
        private const int DiasDeAviso = 3;

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

            var simulation = new SimulationService(_registry, personalities, needsConfig);
            _economia = new EconomyService(catalog, _save);
            _mochila = new InventoryService(_save.Player, _economia);
            _casas = new HomeUpgradeService(_save, _registry, _economia, _mochila);
            _vias = new PlayerProgressionService(_save.Player);
            _conducta = new ConductService(_save.Player);
            _huerto = new FarmingService(new CropCatalog(), _save.Farm, _mochila);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, simulation, factory,
                                        _clock, socialConfig, _save.Triangles, _save.Weddings);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(simulation);
            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IInventoryService>(_mochila);
            ServiceRegistry.Register<IHomeUpgradeService>(_casas);
            ServiceRegistry.Register<ISocialService>(_social);
            ServiceRegistry.Register<IPlayerProgression>(_vias);
            ServiceRegistry.Register<IConductService>(_conducta);
            ServiceRegistry.Register<IFarmingService>(_huerto);

            var bea = factory.CreateRandom("bea");
            _registry.Add(bea);
            _save.Islanders.Add(bea);
            _bea = bea.Id;
        }

        [TearDown]
        public void TearDown()
        {
            _conducta?.Dispose();
            _vias?.Dispose();
            _social?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── que exista lo que hace falta ─────────────────────────────────────

        [Test]
        public void ElAnilloEstaEnElCatalogoYSeFabricaConAlgoRaro()
        {
            Assert.That(_economia.GetItem(RomanceItems.Ring), Is.Not.Null);

            var crafteo = new CraftingService(new RecipeCatalog(), _mochila,
                                              new IslandService(_registry, _save));
            Assert.That(crafteo.TryGetRecipe("recipe_anillo", out var receta), Is.True);

            bool raro = false;
            foreach (var ing in receta.Ingredients)
                if (ing.CatalogId == "mat_cristal_nimbo") raro = true;

            Assert.That(raro, Is.True,
                "el anillo tiene que costar algo que solo se consigue yendo a por ello: " +
                "es lo que ata el romance a la isla");
        }

        [Test]
        public void FabricarloPideOficioOcho()
        {
            _vias.RequirementFor(Unlock.EngagementRing, out var via, out int nivel);

            Assert.That(via, Is.EqualTo(Data.Player.SkillKind.Crafting));
            Assert.That(nivel, Is.EqualTo(8));
        }

        // ── los tres requisitos ──────────────────────────────────────────────

        [Test]
        public void SinSalirNoSePideLaMano()
        {
            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.NotDating));
        }

        [Test]
        public void RecienEmpezadoTampoco()
        {
            Salir(desdeElDia: _clock.Day, afinidad: 95f);

            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.TooEarly),
                "pedir la mano la misma tarde de la declaración no es una historia");
        }

        [Test]
        public void ConPocoCariñoTampoco()
        {
            Salir(desdeElDia: _clock.Day - DiasSaliendo, afinidad: 60f);

            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.NotFondEnough));
        }

        [Test]
        public void SinAnilloTampoco()
        {
            Salir(desdeElDia: _clock.Day - DiasSaliendo, afinidad: 95f);

            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.NoRing));
        }

        [Test]
        public void ConLaCabañaSinAmpliarTampoco()
        {
            Salir(desdeElDia: _clock.Day - DiasSaliendo, afinidad: 95f);
            DarAnillo();

            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.HomeTooSmall),
                "el tercer requisito viene de la gestión, y sin él la boda sería el final " +
                "de la mitad social y de nada más");
        }

        [Test]
        public void ConLosTresSePuede()
        {
            Preparar();

            Assert.That(_social.CanPropose(_bea), Is.EqualTo(ProposalRefusal.Ok));
        }

        // ── la pedida y la boda ──────────────────────────────────────────────

        [Test]
        public void PedirLaManoGastaElAnilloYDejaFecha()
        {
            Preparar();

            Assert.That(_social.PlayerPropose(_bea), Is.True);

            Assert.That(_mochila.CountOf(RomanceItems.Ring), Is.Zero, "el anillo no se gastó");
            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Engaged));
            Assert.That(_save.Weddings, Has.Count.EqualTo(1));
            Assert.That(_save.Weddings[0].HasDate, Is.True,
                "una boda sin fecha no la ve nadie: el sentido de que la aldea la ponga " +
                "es que se entere");
        }

        [Test]
        public void ElDiaDeLaBodaOsCasais()
        {
            Preparar();
            _social.PlayerPropose(_bea);

            PasarDias(DiasDeAviso);

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Married));
        }

        [Test]
        public void AntesDeLaFechaNoOsCasais()
        {
            Preparar();
            _social.PlayerPropose(_bea);

            PasarDias(DiasDeAviso - 1);

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Engaged));
        }

        [Test]
        public void SiRompeisAntesDeLaFechaNoHayBoda()
        {
            Preparar();
            _social.PlayerPropose(_bea);

            // Se acabó entre la pedida y el día señalado.
            Poner(RomanceStage.None, 20f);
            PasarDias(DiasDeAviso);

            Assert.That(EtapaConmigo(), Is.Not.EqualTo(RomanceStage.Married));
            Assert.That(_save.Weddings, Is.Empty,
                "una fecha que se queda puesta te casaría con alguien que ya no está " +
                "contigo el día que volvierais a salir");
        }

        [Test]
        public void ElPlanificadorDeLaAldeaNoTocaTuBoda()
        {
            Preparar();
            _social.PlayerPropose(_bea);

            // El planificador limpia las reservas de gente que no está en el censo, y el
            // protagonista nunca lo va a estar.
            var planner = new WeddingPlanner(_save, _registry, _social,
                                             ServiceRegistry.Get<ISimulationService>());
            planner.AdvanceDay(_clock.Day + 1);
            planner.Dispose();

            Assert.That(_save.Weddings, Has.Count.EqualTo(1),
                "la primera limpieza se llevaba tu boda por no encontrarte en el censo");
        }

        // ── después ──────────────────────────────────────────────────────────

        [Test]
        public void TuParejaRiegaUnaCasillaAlDia()
        {
            Preparar();
            _social.PlayerPropose(_bea);
            PasarDias(DiasDeAviso);
            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Married));

            // Una casilla sembrada y seca, de las del medio del huerto.
            const int x = 3, y = 2;
            _huerto.Till(x, y);
            _mochila.TryStore(_huerto.Crops[0].SeedId, 1, out _);
            _huerto.Plant(x, y, _huerto.Crops[0].SeedId);
            Assert.That(_huerto.TileAt(x, y).Watered, Is.False);

            var chores = new SpouseChores(_registry);
            Assert.That(chores.DoMorningChore(), Is.True);
            Assert.That(_huerto.TileAt(x, y).Watered, Is.True);
        }

        [Test]
        public void SinParejaNoRiegaNadie()
        {
            var chores = new SpouseChores(_registry);
            Assert.That(chores.DoMorningChore(), Is.False);
        }

        // ── utilidades ───────────────────────────────────────────────────────

        /// <summary>Todo en regla: saliendo desde hace tiempo, con anillo y con casa.</summary>
        private void Preparar()
        {
            Salir(desdeElDia: _clock.Day - DiasSaliendo, afinidad: 95f);
            DarAnillo();
            AmpliarCabaña();
        }

        private void Salir(int desdeElDia, float afinidad)
        {
            Poner(RomanceStage.Dating, afinidad);

            var bea = _registry.Get(_bea);
            var record = bea.Relationships.GetOrCreate(SocialIds.Player);
            record.DatingSinceDay = desdeElDia;
            bea.Relationships.Set(record);
        }

        private void Poner(RomanceStage etapa, float afinidad)
        {
            var bea = _registry.Get(_bea);
            var record = bea.Relationships.GetOrCreate(SocialIds.Player);
            record.Romance = etapa;
            record.Affinity = afinidad;
            record.Friendship = FriendshipStage.BestFriend;
            record.Interactions = 20;
            bea.Relationships.Set(record);
        }

        private void DarAnillo() => _mochila.TryStore(RomanceItems.Ring, 1, out _);

        private void AmpliarCabaña()
        {
            _economia.AddCoins(5000, "prueba");
            foreach (var cost in _casas.MaterialsFor(0))
                _mochila.TryStore(cost.CatalogId, cost.Quantity, out _);

            Assert.That(_casas.UpgradePlayerHome(), Is.True, "no se ha podido ampliar la cabaña");
        }

        private void PasarDias(int cuantos)
        {
            for (int d = 1; d <= cuantos; d++)
                EventBus.Publish(new DayPassed(_clock.Day + d));
        }

        private RomanceStage EtapaConmigo() => _social.PlayerRelationship(_bea).Romance;
    }
}
