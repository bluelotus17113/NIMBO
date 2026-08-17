using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Crafting;
using Nimbo.Data.Economy;
using Nimbo.Data.World;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests del sistema de crafteo. Cubren los caminos buenos y, sobre todo,
    /// los fallos: que un error no se lleve materiales por delante.
    /// </summary>
    public class CraftingTests
    {
        // ── JSON mínimo con recetas de prueba ────────────────────────────────

        const string TEST_RECIPES_JSON = @"{
            ""version"": 1,
            ""items"": [
                {
                    ""recipeId"": ""recipe_test_basica"",
                    ""displayName"": ""Básica"",
                    ""description"": ""Dos ingredientes fáciles."",
                    ""station"": ""Hand"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_madera"", ""quantity"": 2 },
                        { ""catalogId"": ""mat_piedra"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_azada"",
                    ""outputQuantity"": 1,
                    ""unlockLevel"": 1
                },
                {
                    ""recipeId"": ""recipe_test_doble"",
                    ""displayName"": ""Doble"",
                    ""description"": ""Devuelve dos unidades."",
                    ""station"": ""Hand"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_fibra"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_azada"",
                    ""outputQuantity"": 2,
                    ""unlockLevel"": 1
                },
                {
                    ""recipeId"": ""recipe_test_triple"",
                    ""displayName"": ""Triple"",
                    ""description"": ""Tres ingredientes distintos."",
                    ""station"": ""Hand"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_madera"", ""quantity"": 1 },
                        { ""catalogId"": ""mat_piedra"", ""quantity"": 1 },
                        { ""catalogId"": ""mat_fibra"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_pico"",
                    ""outputQuantity"": 1,
                    ""unlockLevel"": 1
                },
                {
                    ""recipeId"": ""recipe_test_bench"",
                    ""displayName"": ""De banco"",
                    ""description"": ""Solo se puede en banco de trabajo."",
                    ""station"": ""Bench"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_madera"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_hacha"",
                    ""outputQuantity"": 1,
                    ""unlockLevel"": 1
                },
                {
                    ""recipeId"": ""recipe_test_nivel5"",
                    ""displayName"": ""Nivel 5"",
                    ""description"": ""Pide isla nivel 5."",
                    ""station"": ""Hand"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_madera"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_regadera"",
                    ""outputQuantity"": 1,
                    ""unlockLevel"": 5
                },
                {
                    ""recipeId"": ""recipe_test_cuatro"",
                    ""displayName"": ""Cuatro ingredientes"",
                    ""description"": ""El máximo: cuatro ingredientes."",
                    ""station"": ""Bench"",
                    ""ingredients"": [
                        { ""catalogId"": ""mat_madera"", ""quantity"": 2 },
                        { ""catalogId"": ""mat_piedra"", ""quantity"": 1 },
                        { ""catalogId"": ""mat_fibra"", ""quantity"": 1 },
                        { ""catalogId"": ""mat_savia"", ""quantity"": 1 }
                    ],
                    ""outputId"": ""tool_guadana"",
                    ""outputQuantity"": 1,
                    ""unlockLevel"": 3
                }
            ]
        }";

        // ── helpers ──────────────────────────────────────────────────────────

        RecipeCatalog _catalog;
        FakeInventoryForCrafting _inventory;
        FakeIslandForCrafting _island;
        CraftingService _service;

        [SetUp]
        public void SetUp()
        {
            _catalog = new RecipeCatalog(TEST_RECIPES_JSON);
            _inventory = new FakeInventoryForCrafting();
            // nivel de isla por defecto: 3 (suficiente para la mayoría de recetas de test)
            _island = new FakeIslandForCrafting { Level = 3 };
            _service = new CraftingService(_catalog, _inventory, _island);
        }

        // ── 1. Crafteo con todo a favor ──────────────────────────────────────

        [Test]
        public void Craft_ConTodoAFavor_GastaIngredientesYMeteResultado()
        {
            _inventory.Add("mat_madera", 2);
            _inventory.Add("mat_piedra", 1);

            var result = _service.Craft("recipe_test_basica", CraftStation.Hand);

            Assert.AreEqual(CraftError.Ok, result);
            // ingredientes gastados
            Assert.AreEqual(0, _inventory.CountOf("mat_madera"));
            Assert.AreEqual(0, _inventory.CountOf("mat_piedra"));
            // resultado en la mochila
            Assert.AreEqual(1, _inventory.CountOf("tool_azada"));
        }

        // ── 2. Sin materiales suficientes ────────────────────────────────────

        [Test]
        public void Craft_SinMateriales_DevuelveMissingIngredientsYNoGasta()
        {
            _inventory.Add("mat_madera", 1); // falta una
            _inventory.Add("mat_piedra", 1);

            var result = _service.Craft("recipe_test_basica", CraftStation.Hand);

            Assert.AreEqual(CraftError.MissingIngredients, result);
            // los ingredientes siguen intactos
            Assert.AreEqual(1, _inventory.CountOf("mat_madera"));
            Assert.AreEqual(1, _inventory.CountOf("mat_piedra"));
        }

        // ── 3. Estación equivocada ───────────────────────────────────────────

        [Test]
        public void Craft_EstacionEquivocada_DevuelveWrongStation()
        {
            _inventory.Add("mat_madera", 2);
            _inventory.Add("mat_piedra", 1);

            // recipe_test_basica es Hand, pedimos Bench
            var result = _service.Craft("recipe_test_basica", CraftStation.Bench);

            Assert.AreEqual(CraftError.WrongStation, result);
        }

        [Test]
        public void CanCraft_EstacionEquivocada_DevuelveWrongStation()
        {
            var result = _service.CanCraft("recipe_test_basica", CraftStation.Kitchen);

            Assert.AreEqual(CraftError.WrongStation, result);
        }

        // ── 4. Nivel de isla insuficiente ────────────────────────────────────

        [Test]
        public void Craft_NivelInsuficiente_DevuelveLocked()
        {
            _inventory.Add("mat_madera", 1);
            // recipe_test_nivel5 pide unlockLevel 5, la isla está a nivel 3
            var result = _service.Craft("recipe_test_nivel5", CraftStation.Hand);

            Assert.AreEqual(CraftError.Locked, result);
        }

        [Test]
        public void CanCraft_NivelInsuficiente_DevuelveLocked()
        {
            var result = _service.CanCraft("recipe_test_nivel5", CraftStation.Hand);

            Assert.AreEqual(CraftError.Locked, result);
        }

        // ── 5. Mochila llena: devuelve InventoryFull y no gasta nada ────────
        //     Esta es la prueba que evita que el jugador pierda materiales.

        [Test]
        public void Craft_MochilaLlena_DevuelveInventoryFullYNoGastaIngredientes()
        {
            _inventory.Add("mat_madera", 2);
            _inventory.Add("mat_piedra", 1);

            // simular mochila sin huecos: SlotCount a 0 para que WouldFit falle
            _inventory.SlotCount = 0;

            var result = _service.Craft("recipe_test_basica", CraftStation.Hand);

            Assert.AreEqual(CraftError.InventoryFull, result);
            // los ingredientes siguen donde estaban
            Assert.AreEqual(2, _inventory.CountOf("mat_madera"));
            Assert.AreEqual(1, _inventory.CountOf("mat_piedra"));
            // el resultado NO se coló
            Assert.AreEqual(0, _inventory.CountOf("tool_azada"));
        }

        // ── 6. Receta con varios ingredientes los gasta todos ─────────────────

        [Test]
        public void Craft_VariosIngredientes_LosGastaTodosEnCantidadJusta()
        {
            _inventory.Add("mat_madera", 2);
            _inventory.Add("mat_piedra", 1);
            _inventory.Add("mat_fibra", 1);
            _inventory.Add("mat_savia", 1);

            var result = _service.Craft("recipe_test_cuatro", CraftStation.Bench);

            Assert.AreEqual(CraftError.Ok, result);
            // se gastaron las cantidades exactas
            Assert.AreEqual(0, _inventory.CountOf("mat_madera"));
            Assert.AreEqual(0, _inventory.CountOf("mat_piedra"));
            Assert.AreEqual(0, _inventory.CountOf("mat_fibra"));
            Assert.AreEqual(0, _inventory.CountOf("mat_savia"));
            // y el resultado entró
            Assert.AreEqual(1, _inventory.CountOf("tool_guadana"));
        }

        // ── 7. outputQuantity mayor que 1 ────────────────────────────────────

        [Test]
        public void Craft_OutputQuantityDos_MeteDosUnidades()
        {
            _inventory.Add("mat_fibra", 1);

            var result = _service.Craft("recipe_test_doble", CraftStation.Hand);

            Assert.AreEqual(CraftError.Ok, result);
            Assert.AreEqual(2, _inventory.CountOf("tool_azada"));
        }

        // ── 8. AvailableAt lista lo correcto ─────────────────────────────────

        [Test]
        public void AvailableAt_ListaRecetasDeLaEstacionAunqueFaltenMateriales()
        {
            // sin meter nada en el inventario

            var disponibles = _service.AvailableAt(CraftStation.Hand, 3).ToList();

            // recipe_test_basica (1), recipe_test_doble (1), recipe_test_triple (1) son Hand y nivel ≤ 3
            Assert.AreEqual(3, disponibles.Count);
            Assert.IsTrue(disponibles.Any(r => r.RecipeId == "recipe_test_basica"));
            Assert.IsTrue(disponibles.Any(r => r.RecipeId == "recipe_test_doble"));
            Assert.IsTrue(disponibles.Any(r => r.RecipeId == "recipe_test_triple"));
        }

        [Test]
        public void AvailableAt_NoListaRecetasDeNivelSuperior()
        {
            var disponibles = _service.AvailableAt(CraftStation.Hand, 3).ToList();

            // recipe_test_nivel5 es Hand pero unlockLevel 5, con isla 3 no sale
            Assert.IsFalse(disponibles.Any(r => r.RecipeId == "recipe_test_nivel5"));
        }

        [Test]
        public void AvailableAt_ConNivelSuficiente_SiListaLaDeNivelAlto()
        {
            _island.Level = 5;

            var disponibles = _service.AvailableAt(CraftStation.Hand, 5).ToList();

            Assert.IsTrue(disponibles.Any(r => r.RecipeId == "recipe_test_nivel5"));
        }

        // ── 9. Receta desconocida no rompe nada ──────────────────────────────

        [Test]
        public void Craft_RecetaDesconocida_DevuelveUnknownRecipe()
        {
            var result = _service.Craft("receta_que_no_existe", CraftStation.Hand);

            Assert.AreEqual(CraftError.UnknownRecipe, result);
        }

        [Test]
        public void CanCraft_RecetaDesconocida_DevuelveUnknownRecipe()
        {
            var result = _service.CanCraft("receta_que_no_existe", CraftStation.Hand);

            Assert.AreEqual(CraftError.UnknownRecipe, result);
        }

        // ── 10. ItemCrafted se publica una vez y solo si sale bien ───────────

        [Test]
        public void Craft_ConExito_PublicaItemCraftedUnaSolaVez()
        {
            _inventory.Add("mat_madera", 2);
            _inventory.Add("mat_piedra", 1);

            int fired = 0;
            ItemCrafted lastEvent = default;

            void Handler(ItemCrafted evt)
            {
                fired++;
                lastEvent = evt;
            }

            EventBus.Subscribe<ItemCrafted>(Handler);

            try
            {
                _service.Craft("recipe_test_basica", CraftStation.Hand);

                Assert.AreEqual(1, fired, "ItemCrafted debe publicarse exactamente una vez");
                Assert.AreEqual("recipe_test_basica", lastEvent.RecipeId);
                Assert.AreEqual("tool_azada", lastEvent.OutputId);
                Assert.AreEqual(1, lastEvent.Quantity);
            }
            finally
            {
                EventBus.Unsubscribe<ItemCrafted>(Handler);
            }
        }

        [Test]
        public void Craft_ConFallo_NoPublicaItemCrafted()
        {
            _inventory.Add("mat_madera", 1); // insuficiente: faltan 2

            int fired = 0;

            void Handler(ItemCrafted evt)
            {
                fired++;
            }

            EventBus.Subscribe<ItemCrafted>(Handler);

            try
            {
                var result = _service.Craft("recipe_test_basica", CraftStation.Hand);

                Assert.AreEqual(CraftError.MissingIngredients, result);
                Assert.AreEqual(0, fired, "ItemCrafted no debe publicarse si falló el crafteo");
            }
            finally
            {
                EventBus.Unsubscribe<ItemCrafted>(Handler);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DOBLES DE PRUEBA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Doble de IInventoryService para los tests de crafteo.
    /// Guarda items en un diccionario y permite simular mochila llena.
    /// </summary>
    public class FakeInventoryForCrafting : IInventoryService
    {
        readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        /// <summary>Número de huecos. Poner a 0 para simular mochila llena.</summary>
        public int SlotCount { get; set; } = 20;

        public void Add(string catalogId, int quantity)
        {
            if (_items.TryGetValue(catalogId, out int existing))
                _items[catalogId] = existing + quantity;
            else
                _items[catalogId] = quantity;
        }

        public int CountOf(string catalogId)
        {
            return _items.TryGetValue(catalogId, out int count) ? count : 0;
        }

        public bool TryTake(string catalogId, int quantity = 1)
        {
            if (!_items.TryGetValue(catalogId, out int count) || count < quantity)
                return false;
            _items[catalogId] = count - quantity;
            if (_items[catalogId] <= 0)
                _items.Remove(catalogId);
            return true;
        }

        public StoreResult TryStore(string catalogId, int quantity, out int leftover)
        {
            // en tests, si hay hueco (SlotCount > 0) se acepta todo
            Add(catalogId, quantity);
            leftover = 0;
            return StoreResult.Ok;
        }

        // ── necesarios para WouldFit ─────────────────────────────────────────

        public int StackLimitOf(string catalogId) => 99;

        public ItemStack At(int slot) => default;

        // ── stubs ────────────────────────────────────────────────────────────

        public int SelectedSlot => 0;
        public int HotbarSize => 10;
        public ItemStack InHand => default;
        public ToolKind ToolInHand => ToolKind.None;
        public int ToolTierInHand => 1;
        public IReadOnlyList<ItemStack> Slots => new List<ItemStack>();

        public void Select(int slot) { }
        public bool TryConsumeSelected(int quantity = 1) => false;
        public void Swap(int a, int b) { }
        public bool Drop(int slot) => false;
    }

    /// <summary>
    /// Doble de IIslandService para los tests de crafteo.
    /// Solo necesitamos el nivel de isla para las comprobaciones de Locked.
    /// </summary>
    public class FakeIslandForCrafting : IIslandService
    {
        public int Level = 3;

        public IslandState State => new IslandState { Level = Level };

        // ── stubs ────────────────────────────────────────────────────────────

        public IReadOnlyList<string> ZoneIds => new List<string>();

        public ZonePurpose PurposeOf(string zoneId) => ZonePurpose.Home;

        public bool TryGetSpawnPoint(string zoneId, out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }

        public IEnumerable<string> ReachableFrom(string zoneId) => Enumerable.Empty<string>();

        public bool IsUnlocked(string buildingId) => false;

        public bool Unlock(string buildingId) => false;

        public void SendTo(string islanderId, string zoneId) { }
    }
}
