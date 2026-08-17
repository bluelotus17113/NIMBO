using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Crafting;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Events.Minigames;
using Nimbo.Events.Scheduling;
using Nimbo.Island;
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que los tres minijuegos lleguen a jugarse y que lo que dan llegue al jugador.
    /// </summary>
    /// <remarks>
    /// La lógica de los tres ya estaba probada —<c>MinigameTests</c> lleva meses en
    /// verde— y no servía de nada: nadie los construía, nadie los arrancaba y nadie
    /// cobraba sus puntos. Esta clase prueba justo la mitad que faltaba, que es la que
    /// hacía que no existieran.
    /// </remarks>
    public class MinijuegosEnchufadosTests
    {
        private SaveGame _save;
        private GameClock _clock;
        private IslanderRegistry _registro;
        private SimulationService _simulacion;
        private EconomyService _economia;
        private InventoryService _mochila;
        private CraftingService _crafteo;
        private IslandService _isla;
        private EventScheduler _calendario;
        private MinigameService _minijuegos;
        private IslanderData _vecino;

        /// <summary>
        /// Una receta de cocina de las que ya hay en el catálogo, y la única que se
        /// puede hacer con la isla recién empezada: las otras cinco piden de nivel 4
        /// para arriba.
        /// </summary>
        private const string Receta = "recipe_palomitas";

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registro = new IslanderRegistry();

            var personalidades = PersonalityRoster.CreateService();
            var catalogo = new ItemCatalog();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var eventsConfig = ScriptableObject.CreateInstance<EventsConfig>();

            _simulacion = new SimulationService(_registro, personalidades, needsConfig);
            _economia = new EconomyService(catalogo, _save);
            _mochila = new InventoryService(_save.Player, _economia);
            _isla = new IslandService(_registro, _save);
            _crafteo = new CraftingService(new RecipeCatalog(), _mochila, _isla);
            _calendario = new EventScheduler(eventsConfig);
            _minijuegos = new MinigameService(null, _calendario);

            ServiceRegistry.Register<IIslanderRegistry>(_registro);
            ServiceRegistry.Register<ISimulationService>(_simulacion);
            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IInventoryService>(_mochila);
            ServiceRegistry.Register<ICraftingService>(_crafteo);
            ServiceRegistry.Register<IIslandService>(_isla);
            ServiceRegistry.Register<IMinigameService>(_minijuegos);

            var comidas = new List<string>();
            for (int i = 0; i < catalogo.All.Count; i++)
                if (catalogo.All[i].Category == ItemCategory.Food)
                    comidas.Add(catalogo.All[i].CatalogId);

            var fabrica = new IslanderFactory(_registro, personalidades, _clock, comidas);
            _vecino = fabrica.CreateRandom("vecino");
            _registro.Add(_vecino);
            _vecino.Mood.Happiness = 50f;
        }

        [TearDown]
        public void TearDown()
        {
            _calendario?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── que existan las cosas de las que dependen ────────────────────────

        [Test]
        public void LaCanaEstaEnElCatalogoYSeSabeQueEsUnaCana()
        {
            var cana = _economia.GetItem("tool_cana");

            Assert.That(cana, Is.Not.Null, "sin caña en el catálogo no se puede pescar");
            Assert.That(cana.Tool, Is.EqualTo(ToolKind.FishingRod),
                "el tipo de herramienta sale del catálogo, no del identificador: ese error " +
                "ya dejó una vez todas las herramientas leyéndose como «ninguna»");
        }

        [Test]
        public void LaCanaSePuedeFabricar()
        {
            Assert.That(_crafteo.TryGetRecipe("recipe_cana", out var receta), Is.True,
                "una herramienta que no se puede conseguir es una herramienta que no existe");
            Assert.That(receta.OutputId, Is.EqualTo("tool_cana"));
        }

        [Test]
        public void TodoLoQueSePescaExisteEnElCatalogo()
        {
            // El fallo de un identificador mal escrito no se ve: la mochila lo rechaza en
            // silencio y el jugador saca el sedal vacío sin que falle nada.
            foreach (var id in new[] { "food_pez_nube", "food_pez_farol",
                                       "food_anguila_de_viento", "food_bota_vieja",
                                       "food_engrudo" })
                Assert.That(_economia.GetItem(id), Is.Not.Null, $"falta {id} en el catálogo");
        }

        // ── el ciclo ─────────────────────────────────────────────────────────

        [Test]
        public void NoSePuedenJugarDosALaVez()
        {
            Assert.That(_minijuegos.Start(MinigameKind.Fishing, 2), Is.True);
            Assert.That(_minijuegos.Start(MinigameKind.Rhythm, 2), Is.False,
                "los tres se juegan en un sitio concreto y no se puede estar en dos");
        }

        [Test]
        public void LaCocinaSinRecetaNoArranca()
        {
            Assert.That(_minijuegos.Start(MinigameKind.Cooking, 2), Is.False,
                "la cocina no es un juego suelto: es cómo se hacen las recetas de cocina");
            Assert.That(_minijuegos.Running, Is.Null);
        }

        [Test]
        public void DejarloAMediasNoGastaNada()
        {
            DarIngredientes(Receta);
            var antes = Existencias(Receta);

            _minijuegos.Start(MinigameKind.Cooking, 2, Receta);
            _minijuegos.Abandon();

            Assert.That(Existencias(Receta), Is.EqualTo(antes),
                "los ingredientes se cobran al terminar; cerrar la ventana sale gratis");
            Assert.That(_minijuegos.Running, Is.Null);
        }

        // ── la cocina ────────────────────────────────────────────────────────

        [Test]
        public void CocinarBienDaElPlatoYGastaLosIngredientes()
        {
            DarIngredientes(Receta);
            _crafteo.TryGetRecipe(Receta, out var receta);

            _minijuegos.Start(MinigameKind.Cooking, 2, Receta);
            JugarLaCocina(acertando: true);
            var resultado = _minijuegos.Finish();

            Assert.That(resultado.IsVictory, Is.True);
            Assert.That(_mochila.CountOf(receta.OutputId), Is.GreaterThan(0),
                "se ha cocinado y no hay plato");
            Assert.That(Existencias(Receta), Is.Zero, "el plato ha salido gratis");
        }

        [Test]
        public void QuemarlaDaEngrudoYGastaLosIngredientesIgual()
        {
            DarIngredientes(Receta);
            _crafteo.TryGetRecipe(Receta, out var receta);

            _minijuegos.Start(MinigameKind.Cooking, 2, Receta);
            JugarLaCocina(acertando: false);
            var resultado = _minijuegos.Finish();

            Assert.That(resultado.IsVictory, Is.False);
            Assert.That(_mochila.CountOf("food_engrudo"), Is.EqualTo(1),
                "perder no puede dejarte sin cenar; te deja sin la receta buena");
            Assert.That(_mochila.CountOf(receta.OutputId), Is.Zero);
            Assert.That(Existencias(Receta), Is.Zero,
                "los ingredientes se gastan gane o pierda: si no, no habría nada en juego");
        }

        // ── la pesca ─────────────────────────────────────────────────────────

        [Test]
        public void SacarElPezDaAlgoParaLaMochila()
        {
            _minijuegos.Start(MinigameKind.Fishing, 1);

            // Recoger sin parar: la tensión sube, pero a dificultad 1 da tiempo.
            for (int i = 0; i < 40 && !_minijuegos.IsOver; i++) _minijuegos.Step(0);
            var resultado = _minijuegos.Finish();

            if (!resultado.IsVictory)
            {
                Assert.That(_minijuegos.LastPrize, Is.Empty,
                    "el pez se ha escapado y aun así ha caído algo en la mochila");
                return;
            }

            Assert.That(_minijuegos.LastPrize, Is.Not.Empty, "se ha sacado el pez y no hay pez");
            Assert.That(_mochila.CountOf(_minijuegos.LastPrize), Is.EqualTo(1));
        }

        [Test]
        public void SiElPezEscapaNoHayNadaQueCobrar()
        {
            long antes = _save.Wallet.Coins;

            // Tirar sin parar del pez más grande. A dificultad 5 el bicho arranca a
            // setenta y cada tirón más el recogido pasan de treinta de tensión: el sedal
            // se rompe al tercero y no da tiempo a acercarlo. Perder aquí no es azar.
            _minijuegos.Start(MinigameKind.Fishing, 5);
            for (int i = 0; i < 200 && !_minijuegos.IsOver; i++) _minijuegos.Step(0);
            var resultado = _minijuegos.Finish();

            Assert.That(resultado.IsVictory, Is.False, "no se ha escapado: la prueba no prueba nada");
            Assert.That(_minijuegos.LastPrize, Is.Empty);
            Assert.That(_save.Wallet.Coins, Is.EqualTo(antes),
                "cobrar por el pez que se escapó quitaría la única razón para elegir bien");
        }

        // ── el ritmo ─────────────────────────────────────────────────────────

        [Test]
        public void SinConciertoTocarEsEnsayar()
        {
            _vecino.Mood.Happiness = 50f;

            TocarPerfecto();
            _minijuegos.Finish();

            Assert.That(_vecino.Mood.Happiness, Is.EqualTo(50f).Within(0.01f),
                "ensayar solo en el escenario vacío no puede alegrar al pueblo entero");
        }

        [Test]
        public void TocarBienEnElConciertoAnimaALaAldea()
        {
            Assert.That(_calendario.TryStartEvent("concert", 18), Is.True,
                "sin concierto puesto esta prueba no prueba nada");
            Assert.That(_minijuegos.ConcertRunning, Is.True);

            _vecino.Mood.Happiness = 50f;

            TocarPerfecto();
            _minijuegos.Finish();

            Assert.That(_vecino.Mood.Happiness, Is.GreaterThan(50f),
                "tocar bien en la fiesta del pueblo tiene que notarse en el pueblo");
        }

        [Test]
        public void LaNotaQueSePasaDeLargoSeFallaSola()
        {
            _minijuegos.Start(MinigameKind.Rhythm, 1);

            // Nadie toca nada: solo corre el reloj. Sin esto, quedarse quieto congelaría
            // la canción y un juego de ritmo dejaría de ir con el ritmo.
            for (int i = 0; i < 600 && !_minijuegos.IsOver; i++) _minijuegos.Tick(0.1f);

            Assert.That(_minijuegos.IsOver, Is.True, "la canción no termina sola");
            Assert.That(_minijuegos.Finish().IsVictory, Is.False);
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>Toca todas las notas en su momento exacto.</summary>
        private void TocarPerfecto()
        {
            _minijuegos.Start(MinigameKind.Rhythm, 1);

            for (int i = 0; i < 100 && !_minijuegos.IsOver; i++)
            {
                // Adelantar el reloj justo hasta la nota y pulsar ahí.
                float faltan = _minijuegos.MillisecondsToBeat / 1000f;
                if (faltan > 0f) _minijuegos.Tick(faltan);
                _minijuegos.Step(0);
            }
        }

        /// <summary>Juega la cocina entera acertando o fallando a propósito.</summary>
        /// <remarks>
        /// La acción buena de cada paso se sabe **después** de jugarlo
        /// (<c>LastStepAction</c>), así que acertar siempre a la primera no se puede.
        /// Lo que sí se puede es leer la pista, que es lo que hace el jugador: cada
        /// familia de frases va con una acción.
        /// </remarks>
        private void JugarLaCocina(bool acertando)
        {
            var cocina = new CookingGameProbe(_minijuegos);

            for (int i = 0; i < 20 && !_minijuegos.IsOver; i++)
            {
                int buena = cocina.AccionSegunLaPista();
                _minijuegos.Step(acertando ? buena : (buena + 1) % 3);
            }
        }

        private void DarIngredientes(string recipeId)
        {
            Assert.That(_crafteo.TryGetRecipe(recipeId, out var receta), Is.True);

            foreach (var ing in receta.Ingredients)
                _mochila.TryStore(ing.CatalogId, ing.Quantity, out _);
        }

        /// <summary>Cuánto queda en la mochila de lo que pide la receta.</summary>
        private int Existencias(string recipeId)
        {
            _crafteo.TryGetRecipe(recipeId, out var receta);

            int total = 0;
            foreach (var ing in receta.Ingredients) total += _mochila.CountOf(ing.CatalogId);
            return total;
        }

        /// <summary>Lee la pista de la cocina y dice qué acción pide.</summary>
        private sealed class CookingGameProbe
        {
            private readonly IMinigameService _service;

            public CookingGameProbe(IMinigameService service) => _service = service;

            public int AccionSegunLaPista()
            {
                string pista = _service.Headline;

                if (pista.Contains("cortar") || pista.Contains("trozos") ||
                    pista.Contains("cuchillo") || pista.Contains("tabla")) return 0;

                if (pista.Contains("removerlo") || pista.Contains("mover") ||
                    pista.Contains("cuchara") || pista.Contains("pegue")) return 1;

                return 2;
            }
        }
    }
}
