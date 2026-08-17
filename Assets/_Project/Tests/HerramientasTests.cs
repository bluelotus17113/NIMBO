using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Crafting;
using Nimbo.Data.Economy;
using Nimbo.Data.Player;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Gathering;
using Nimbo.Island;
using Nimbo.Items;
using Nimbo.Player;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// El segundo escalón de herramientas (§12.4): que exista, que se pueda fabricar y
    /// que se note en la mano.
    /// </summary>
    /// <remarks>
    /// Dos escalones y no tres a propósito. Dos bastan para que la mejora se note y no
    /// obligan a rehacer el equilibrio de los ciento veinte nodos de la isla; con tres,
    /// cada número del catálogo de recursos habría que mirarlo tres veces.
    /// </remarks>
    public class HerramientasTests
    {
        private SaveGame _save;
        private EconomyService _economia;
        private InventoryService _mochila;
        private CraftingService _crafteo;
        private PlayerProgressionService _vias;

        /// <summary>Las cinco buenas, con la de siempre de la que salen.</summary>
        private static readonly (string Vieja, string Buena, string Receta)[] Escalones =
        {
            ("tool_azada", "tool_azada_recia", "recipe_azada_recia"),
            ("tool_regadera", "tool_regadera_grande", "recipe_regadera_grande"),
            ("tool_hacha", "tool_hacha_buena", "recipe_hacha_buena"),
            ("tool_pico", "tool_pico_buena", "recipe_pico_buena"),
            ("tool_guadana", "tool_guadana_larga", "recipe_guadana_larga"),
        };

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame();
            _economia = new EconomyService(new ItemCatalog(), _save);
            _mochila = new InventoryService(_save.Player, _economia);
            _crafteo = new CraftingService(new RecipeCatalog(), _mochila,
                                           new IslandService(new IslanderRegistry(), _save));
            _vias = new PlayerProgressionService(_save.Player);

            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IInventoryService>(_mochila);
            ServiceRegistry.Register<IPlayerProgression>(_vias);
        }

        [TearDown]
        public void TearDown()
        {
            _vias?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── que existan ──────────────────────────────────────────────────────

        [Test]
        public void LasCincoBuenasEstanEnElCatalogoYSonDelSegundoEscalon()
        {
            foreach (var (vieja, buena, _) in Escalones)
            {
                var uno = _economia.GetItem(vieja);
                var dos = _economia.GetItem(buena);

                Assert.That(dos, Is.Not.Null, $"falta {buena} en el catálogo");
                Assert.That(dos.ToolTier, Is.EqualTo(2), $"{buena} no dice ser del segundo");
                Assert.That(uno.ToolTier, Is.EqualTo(1), $"{vieja} dejó de ser la de siempre");
                Assert.That(dos.Tool, Is.EqualTo(uno.Tool),
                    $"{buena} no sirve para lo mismo que {vieja}: el escalón cambia lo que " +
                    "hace, no para qué es");
            }
        }

        [Test]
        public void CadaBuenaSeFabricaConLaVieja()
        {
            foreach (var (vieja, buena, receta) in Escalones)
            {
                Assert.That(_crafteo.TryGetRecipe(receta, out var r), Is.True, $"falta {receta}");
                Assert.That(r.OutputId, Is.EqualTo(buena));

                bool llevaLaVieja = false;
                foreach (var ing in r.Ingredients)
                    if (ing.CatalogId == vieja) llevaLaVieja = true;

                Assert.That(llevaLaVieja, Is.True,
                    $"{receta} no se come la {vieja}: acabarías con dos y usando la mala");
            }
        }

        [Test]
        public void FabricarlasPideOficioSeis()
        {
            _vias.RequirementFor(Unlock.BetterTools, out var via, out int nivel);

            Assert.That(via, Is.EqualTo(SkillKind.Crafting));
            Assert.That(nivel, Is.EqualTo(6));
            Assert.That(_vias.IsUnlocked(Unlock.BetterTools), Is.False,
                "el sumidero de material que le da sentido a acumular no puede estar abierto " +
                "el primer día");
        }

        // ── que se noten ─────────────────────────────────────────────────────

        [Test]
        public void ConElHachaBuenaSeTalaEnMenosGolpes()
        {
            int conLaVieja = GolpesHastaAgotar(escalon: 1);
            int conLaBuena = GolpesHastaAgotar(escalon: 2);

            Assert.That(conLaBuena, Is.LessThan(conLaVieja),
                "la herramienta buena no ahorra ni un golpe: no es una mejora, es un adorno caro");
        }

        [Test]
        public void NingunNodoCaeDeUnSoloGolpe()
        {
            // Con Recolección 5 y la herramienta buena se descuentan tres golpes de una
            // vez. Un árbol de tres que cayera al primer toque dejaría de ser un sitio
            // al que ir y pasaría a ser un botón.
            _vias.Grant(SkillKind.Gathering, 99999f);

            Assert.That(GolpesHastaAgotar(escalon: 2), Is.GreaterThan(1));
        }

        /// <summary>Cuántas veces hay que darle a un árbol hasta que suelta.</summary>
        private int GolpesHastaAgotar(int escalon)
        {
            var mochila = new MochilaConEscalon { Escalon = escalon };
            var recoleccion = new GatheringService(new NodeCatalog(), new GatheringState(),
                                                   mochila, new GameClock());

            var nodos = recoleccion.Nodes;
            string arbol = null;
            for (int i = 0; i < nodos.Count; i++)
            {
                if (!recoleccion.TryGetDefinition(nodos[i].NodeId, out var def)) continue;
                if (def.RequiredTool != ToolKind.Axe || def.Hits < 4) continue;
                arbol = nodos[i].InstanceId;
                break;
            }
            Assert.That(arbol, Is.Not.Null, "la isla no tiene ningún árbol que aguante lo bastante");

            for (int golpe = 1; golpe <= 20; golpe++)
                if (recoleccion.Gather(arbol, ToolKind.Axe, out _) != GatherResult.Hit)
                    return golpe;

            Assert.Fail("veinte hachazos y el árbol sigue en pie");
            return 0;
        }

        /// <summary>Una mochila que solo sabe decir de qué escalón es lo que lleva.</summary>
        private sealed class MochilaConEscalon : IInventoryService
        {
            public int Escalon = 1;

            public int ToolTierInHand => Escalon;
            public ToolKind ToolInHand => ToolKind.Axe;

            public int SlotCount => 20;
            public int SelectedSlot => 0;
            public int HotbarSize => 10;
            public ItemStack InHand => default;
            public ItemStack At(int slot) => default;
            public IReadOnlyList<ItemStack> Slots => System.Array.Empty<ItemStack>();

            public int CountOf(string catalogId) => 0;
            public int StackLimitOf(string catalogId) => 99;

            public StoreResult TryStore(string catalogId, int quantity, out int leftover)
            {
                leftover = 0;
                return StoreResult.Ok;
            }

            public bool TryTake(string catalogId, int quantity = 1) => false;
            public bool TryConsumeSelected(int quantity = 1) => false;
            public void Select(int slot) { }
            public void Swap(int a, int b) { }
            public bool Drop(int slot) => false;
        }
    }
}
