using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Farming;
using Nimbo.Farming;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests del huerto. Cubren los 13 casos del encargo:
    /// siembra, riego, crecimiento, recogida, mochila llena, regrows, fuera de parcela,
    /// y que nada se muere aunque no riegues.
    /// </summary>
    public class FarmingTests
    {
        // ── JSON mínimo para tests ──────────────────────────────────────────

        const string TEST_CROPS_JSON = @"{
            ""version"": 1,
            ""items"": [
                { ""seedId"": ""seed_prueba"", ""cropId"": ""crop_prueba"", ""displayName"": ""Prueba"",
                  ""daysToGrow"": 3, ""yield"": 2, ""regrows"": false },
                { ""seedId"": ""seed_regrows"", ""cropId"": ""crop_regrows"", ""displayName"": ""Regrows"",
                  ""daysToGrow"": 5, ""yield"": 3, ""regrows"": true },
                { ""seedId"": ""seed_rapido"", ""cropId"": ""crop_rapido"", ""displayName"": ""Rápido"",
                  ""daysToGrow"": 2, ""yield"": 1, ""regrows"": false },
                { ""seedId"": ""seed_lento"", ""cropId"": ""crop_lento"", ""displayName"": ""Lento"",
                  ""daysToGrow"": 8, ""yield"": 4, ""regrows"": false }
            ]
        }";

        // ── SetUp / TearDown ────────────────────────────────────────────────

        CropCatalog _catalog;
        FakeInventoryForFarming _inventory;
        FarmState _state;
        FarmingService _service;

        [SetUp]
        public void SetUp()
        {
            _catalog = new CropCatalog(TEST_CROPS_JSON);
            _inventory = new FakeInventoryForFarming();
            _state = new FarmState();
            _service = new FarmingService(_catalog, _state, _inventory);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // ── helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// La casilla con la que se prueba todo.
        /// </summary>
        /// <remarks>
        /// Era la (0,0), y dejó de valer al ponerle nivel a la parcela (§12.3): con
        /// Cultivo 1 solo se puede trabajar el 4×3 del medio, y la esquina de arriba a
        /// la izquierda pide nivel 8. Esta está dentro desde el primer día y sigue
        /// dentro en todos los escalones, así que estas pruebas hablan del huerto y no
        /// de la progresión — para eso está <c>ProgresionTests</c>.
        /// </remarks>
        const int Cx = 3;
        const int Cy = 2;

        /// <summary>Avanza N días, regando la casilla cada día.</summary>
        void WaterAndAdvance(int x, int y, int days)
        {
            for (int i = 0; i < days; i++)
            {
                _service.Water(x, y);
                _service.AdvanceDay();
            }
        }

        // ── 1. Huerto nuevo: todo Wild ──────────────────────────────────────

        [Test]
        public void NewFarm_AllWild()
        {
            Assert.AreEqual(8, _service.Width);
            Assert.AreEqual(6, _service.Height);

            for (int y = 0; y < _service.Height; y++)
            {
                for (int x = 0; x < _service.Width; x++)
                {
                    var tile = _service.TileAt(x, y);
                    Assert.IsNotNull(tile, $"TileAt({x},{y}) devolvió null");
                    Assert.AreEqual(TileState.Wild, tile.State,
                        $"la casilla ({x},{y}) debería estar Wild");
                }
            }
        }

        // ── 2. Labrar deja Tilled; labrar dos veces no rompe ────────────────

        [Test]
        public void Till_FromWild_BecomesTilled()
        {
            var error = _service.Till(Cx, Cy);
            Assert.AreEqual(FarmError.Ok, error);
            Assert.AreEqual(TileState.Tilled, _service.TileAt(Cx, Cy).State);
        }

        [Test]
        public void Till_Twice_DoesNotBreak()
        {
            _service.Till(Cx, Cy);
            var error = _service.Till(Cx, Cy);
            Assert.AreEqual(FarmError.Ok, error,
                "labrar dos veces no debe ser un error");
            Assert.AreEqual(TileState.Tilled, _service.TileAt(Cx, Cy).State);
        }

        // ── 3. Sembrar sin labrar devuelve NotTilled ────────────────────────

        [Test]
        public void Plant_OnWild_ReturnsNotTilled()
        {
            _inventory.Add("seed_prueba", 5);
            var error = _service.Plant(Cx, Cy, "seed_prueba");
            Assert.AreEqual(FarmError.NotTilled, error);
            Assert.AreEqual(TileState.Wild, _service.TileAt(Cx, Cy).State,
                "la casilla no debe cambiar");
        }

        // ── 4. Sembrar gasta una semilla ────────────────────────────────────

        [Test]
        public void Plant_ConsumesOneSeed()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 3);

            var error = _service.Plant(Cx, Cy, "seed_prueba");
            Assert.AreEqual(FarmError.Ok, error);
            Assert.AreEqual(2, _inventory.CountOf("seed_prueba"),
                "debería quedar una semilla menos");
            Assert.AreEqual(TileState.Planted, _service.TileAt(Cx, Cy).State);
            Assert.AreEqual("seed_prueba", _service.TileAt(Cx, Cy).SeedId);
        }

        // ── 5. Sembrar sin llevar semillas devuelve NoSeed ──────────────────

        [Test]
        public void Plant_WithoutSeed_ReturnsNoSeed()
        {
            _service.Till(Cx, Cy);
            // No añadimos nada al inventario.

            var error = _service.Plant(Cx, Cy, "seed_prueba");
            Assert.AreEqual(FarmError.NoSeed, error);
            Assert.AreEqual(TileState.Tilled, _service.TileAt(Cx, Cy).State,
                "la casilla debe seguir Tilled, sin cambiar");
        }

        // ── 6. Sembrar, regar y pasar los días → Ready ──────────────────────

        [Test]
        public void PlantWaterAdvance_BecomesReady()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");

            // seed_prueba tarda 3 días. Regamos y avanzamos 3 veces.
            WaterAndAdvance(Cx, Cy, 3);

            Assert.AreEqual(TileState.Ready, _service.TileAt(Cx, Cy).State,
                "tras 3 días de riego debería estar Ready");
        }

        // ── 7. Sin regar 5 días: sigue Planted, no se muere ─────────────────

        [Test]
        public void PlantNoWater_StaysPlanted_DoesNotDie()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");

            // Avanzamos 5 días sin regar.
            for (int i = 0; i < 5; i++)
            {
                _service.AdvanceDay();
            }

            Assert.AreEqual(TileState.Planted, _service.TileAt(Cx, Cy).State,
                "sin regar la planta no crece pero sigue viva");
            Assert.AreEqual(0, _service.TileAt(Cx, Cy).GrowthDays,
                "sin riego GrowthDays no avanza");
        }

        // ── 8. Regar día sí día no tarda el doble ───────────────────────────

        [Test]
        public void WaterEveryOtherDay_TakesTwiceAsLong()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");

            // Día 1: riego + avance → GrowthDays=1
            _service.Water(Cx, Cy);
            _service.AdvanceDay();

            // Día 2: sin regar → no avanza
            _service.AdvanceDay();
            Assert.AreEqual(1, _service.TileAt(Cx, Cy).GrowthDays);

            // Día 3: riego + avance → GrowthDays=2
            _service.Water(Cx, Cy);
            _service.AdvanceDay();

            // Día 4: sin regar → no avanza
            _service.AdvanceDay();

            // Día 5: riego + avance → GrowthDays=3 → Ready
            _service.Water(Cx, Cy);
            _service.AdvanceDay();

            Assert.AreEqual(TileState.Ready, _service.TileAt(Cx, Cy).State,
                "regando días alternos, tarda 5 días reales en llegar a Ready (3 de riego)");
        }

        // ── 9. Recoger mete el cultivo en la mochila ────────────────────────

        [Test]
        public void Harvest_StoresCropInInventory()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");
            WaterAndAdvance(Cx, Cy, 3);

            int harvested = _service.Harvest(Cx, Cy, out var error);

            Assert.AreEqual(FarmError.Ok, error);
            Assert.AreEqual(2, harvested,
                "seed_prueba tiene yield 2");
            Assert.AreEqual(2, _inventory.CountOf("crop_prueba"),
                "la mochila debe tener 2 crop_prueba");
        }

        // ── 10. Regrows vuelve a Planted; sin regrows deja Tilled ───────────

        [Test]
        public void Harvest_Regrows_BackToPlanted()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_regrows", 1);
            _service.Plant(Cx, Cy, "seed_regrows");
            WaterAndAdvance(Cx, Cy, 5); // 5 días para seed_regrows

            _service.Harvest(Cx, Cy, out var error);

            Assert.AreEqual(FarmError.Ok, error);
            Assert.AreEqual(TileState.Planted, _service.TileAt(Cx, Cy).State,
                "con regrows, tras recoger vuelve a Planted");
            Assert.AreEqual(2, _service.TileAt(Cx, Cy).GrowthDays,
                "GrowthDays debería ser la mitad de 5 → 2");
            Assert.AreEqual("seed_regrows", _service.TileAt(Cx, Cy).SeedId,
                "el SeedId se mantiene para que siga dando");
        }

        [Test]
        public void Harvest_NoRegrows_LeavesTilled()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");
            WaterAndAdvance(Cx, Cy, 3);

            _service.Harvest(Cx, Cy, out var error);

            Assert.AreEqual(FarmError.Ok, error);
            Assert.AreEqual(TileState.Tilled, _service.TileAt(Cx, Cy).State,
                "sin regrows, tras recoger la casilla queda Tilled");
            Assert.AreEqual("", _service.TileAt(Cx, Cy).SeedId,
                "el SeedId se vacía al no tener regrows");
        }

        // ── 11. AdvanceDay ×10 no rompe ni pasa de Ready ────────────────────

        [Test]
        public void AdvanceDay_TenTimes_DoesNotBreak()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");

            // Regamos una sola vez y avanzamos 10 días.
            _service.Water(Cx, Cy);
            for (int i = 0; i < 10; i++)
            {
                _service.AdvanceDay();
            }

            // Solo el primer día avanza (Watered se seca tras AdvanceDay).
            // Así que GrowthDays = 1 y sigue Planted.
            Assert.AreEqual(TileState.Planted, _service.TileAt(Cx, Cy).State,
                "10 avances no rompen la casilla");

            // Probemos ahora con Ready: llevamos a Ready y avanzamos más.
            WaterAndAdvance(Cx, Cy, 2); // con esto ya son 3 días de riego → Ready
            Assert.AreEqual(TileState.Ready, _service.TileAt(Cx, Cy).State);

            // Avanzar más días no la saca de Ready.
            for (int i = 0; i < 10; i++)
            {
                _service.AdvanceDay();
            }

            Assert.AreEqual(TileState.Ready, _service.TileAt(Cx, Cy).State,
                "una casilla Ready no debe cambiar con más avances");
        }

        // ── 12. Fuera de la parcela → OutOfBounds ───────────────────────────

        [Test]
        public void OutOfBounds_AllOperations()
        {
            // Coordenadas fuera del 8×6.
            Assert.AreEqual(FarmError.OutOfBounds, _service.Till(-1, 0));
            Assert.AreEqual(FarmError.OutOfBounds, _service.Till(8, 0));
            Assert.AreEqual(FarmError.OutOfBounds, _service.Till(0, -1));
            Assert.AreEqual(FarmError.OutOfBounds, _service.Till(0, 6));

            Assert.AreEqual(FarmError.OutOfBounds,
                _service.Plant(8, 0, "seed_prueba"));

            Assert.AreEqual(FarmError.OutOfBounds,
                _service.Water(-1, 3));

            _service.Harvest(99, 99, out var harvestError);
            Assert.AreEqual(FarmError.OutOfBounds, harvestError);

            Assert.AreEqual(FarmError.OutOfBounds,
                _service.Clear(8, 8));
        }

        // ── 13. Mochila llena → InventoryFull, la planta sigue Ready ────────

        [Test]
        public void Harvest_InventoryFull_PlantStaysReady()
        {
            _service.Till(Cx, Cy);
            _inventory.Add("seed_prueba", 1);
            _service.Plant(Cx, Cy, "seed_prueba");
            WaterAndAdvance(Cx, Cy, 3);

            // Bloqueamos la mochila para que rechace cualquier intento.
            _inventory.RejectEverything = true;

            int harvested = _service.Harvest(Cx, Cy, out var error);

            Assert.AreEqual(FarmError.InventoryFull, error);
            Assert.AreEqual(0, harvested,
                "con la mochila llena no se recoge nada");
            Assert.AreEqual(TileState.Ready, _service.TileAt(Cx, Cy).State,
                "la planta debe seguir Ready para que el jugador vuelva a por ella");
            Assert.AreEqual("seed_prueba", _service.TileAt(Cx, Cy).SeedId,
                "el SeedId no debe perderse");
        }
    }

    /// <summary>
    /// Doble de IInventoryService para los tests del huerto.
    /// Guarda items en un diccionario y puede simular mochila llena.
    /// </summary>
    public class FakeInventoryForFarming : IInventoryService
    {
        readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        public bool RejectEverything { get; set; }

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
            return true;
        }

        public StoreResult TryStore(string catalogId, int quantity, out int leftover)
        {
            if (RejectEverything)
            {
                leftover = quantity;
                return StoreResult.Full;
            }

            // Sin límite en tests salvo cuando está explícitamente bloqueado.
            Add(catalogId, quantity);
            leftover = 0;
            return StoreResult.Ok;
        }

        // ── stubs: no los usa el huerto ─────────────────────────────────────

        public int SlotCount => 20;
        public int SelectedSlot => 0;
        public int HotbarSize => 10;

        public ItemStack At(int slot) => default;
        public ItemStack InHand => default;
        public ToolKind ToolInHand => ToolKind.None;
        public int ToolTierInHand => 1;

        public void Select(int slot) { }
        public bool TryConsumeSelected(int quantity = 1) => false;
        public void Swap(int a, int b) { }
        public bool Drop(int slot) => false;
        public int StackLimitOf(string catalogId) => 99;
        public IReadOnlyList<ItemStack> Slots => new List<ItemStack>();
    }
}
