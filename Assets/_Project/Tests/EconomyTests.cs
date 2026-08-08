using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Economy;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.Tests
{
    public class EconomyTests
    {
        ItemCatalog _catalog;
        SaveGame _save;
        EconomyService _service;
        TestIslanderRegistry _registry;
        TestSimulationService _sim;

        bool _giftFired;
        ItemGifted _lastGift;

        // ── JSON mínimo para tests controlados ──────────────────────────────────

        const string FOOD_JSON = @"{
            ""version"": 1, ""category"": ""Food"", ""items"": [
                { ""catalogId"": ""test_snack"",   ""displayName"": ""Snack"",
                  ""description"": """", ""price"": 5,  ""unlockLevel"": 1,
                  ""tags"": [], ""hungerRestore"": 15, ""foodKind"": ""snack"" },
                { ""catalogId"": ""test_gourmet"", ""displayName"": ""Gourmet"",
                  ""description"": """", ""price"": 50, ""unlockLevel"": 30,
                  ""tags"": [], ""hungerRestore"": 50, ""foodKind"": ""meal"" }
            ]
        }";

        const string CLOTHING_JSON = @"{
            ""version"": 1, ""category"": ""Clothing"", ""items"": [
                { ""catalogId"": ""test_camiseta"", ""displayName"": ""Camiseta"",
                  ""description"": """", ""price"": 30, ""unlockLevel"": 1,
                  ""tags"": [], ""slot"": ""outfit"", ""style"": ""casual"",
                  ""palette"": [""#FFFFFF""] }
            ]
        }";

        const string FURNITURE_JSON = @"{
            ""version"": 1, ""category"": ""Furniture"", ""items"": [
                { ""catalogId"": ""test_silla"", ""displayName"": ""Silla"",
                  ""description"": """", ""price"": 10, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""seat"", ""needBonus"": {} },
                { ""catalogId"": ""test_mesa"",  ""displayName"": ""Mesa"",
                  ""description"": """", ""price"": 30, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 2, ""footprintY"": 2,
                  ""function"": ""table"", ""needBonus"": {} }
            ]
        }";

        const string FINISHES_JSON = @"{
            ""version"": 1, ""category"": ""Finishes"", ""items"": [
                { ""catalogId"": ""test_pared"", ""displayName"": ""Pared"",
                  ""description"": """", ""price"": 5,  ""unlockLevel"": 1,
                  ""tags"": [], ""surface"": ""wall"", ""baseColor"": ""#FFFFFF"", ""pattern"": ""liso"" },
                { ""catalogId"": ""test_suelo"", ""displayName"": ""Suelo"",
                  ""description"": """", ""price"": 8,  ""unlockLevel"": 1,
                  ""tags"": [], ""surface"": ""floor"", ""baseColor"": ""#A08060"", ""pattern"": ""madera"" }
            ]
        }";

        // ── setup / teardown ────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _catalog = new ItemCatalog(FOOD_JSON, CLOTHING_JSON, FURNITURE_JSON, FINISHES_JSON);
            _save = new SaveGame { ElapsedMinutes = 0 };
            _save.Wallet = new Wallet { Coins = 100 };
            _service = new EconomyService(_catalog, _save);

            var islander = new IslanderData();
            islander.Identity.Id = "test_islander";
            islander.Progression = ProgressionState.Fresh; // Level = 1
            islander.Tastes.LovedFoods.Add("test_snack");
            islander.Tastes.HatedFoods.Add("test_gourmet");

            _registry = new TestIslanderRegistry(islander);
            _sim = new TestSimulationService();
            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_sim);

            _giftFired = false;
            EventBus.Subscribe<ItemGifted>(OnItemGifted);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            EventBus.Unsubscribe<ItemGifted>(OnItemGifted);
            ServiceRegistry.Unregister<IIslanderRegistry>();
            ServiceRegistry.Unregister<ISimulationService>();
        }

        void OnItemGifted(ItemGifted evt) { _giftFired = true; _lastGift = evt; }

        // ── 1. catálogo ─────────────────────────────────────────────────────────

        [Test]
        public void Catalog_LoadsAllItems_IndexesWithoutDuplicates()
        {
            Assert.AreEqual(7, _catalog.Count);
            Assert.AreEqual(7, _catalog.All.Count);

            var ids = new HashSet<string>();
            for (int i = 0; i < _catalog.All.Count; i++)
            {
                string id = _catalog.All[i].CatalogId;
                Assert.IsFalse(ids.Contains(id), $"Duplicate catalogId: {id}");
                ids.Add(id);
            }
        }

        [Test]
        public void Catalog_LoadsRealFiles_Has225UniqueItems()
        {
            // Este test usa Resources.Load y solo funciona en el Unity Test Runner.
            var catalog = new ItemCatalog();
            Assert.AreEqual(225, catalog.Count);

            var ids = new HashSet<string>();
            for (int i = 0; i < catalog.All.Count; i++)
            {
                string id = catalog.All[i].CatalogId;
                Assert.IsFalse(ids.Contains(id), $"Duplicate catalogId: {id}");
                ids.Add(id);
            }
            Assert.AreEqual(catalog.Count, ids.Count);
        }

        [Test]
        public void GetItem_UnknownId_ReturnsNull()
        {
            // El catálogo grita cuando le piden algo que no tiene, y hace bien:
            // un id inventado es un fallo de datos, no un caso normal.
            LogAssert.Expect(LogType.Error, new Regex("no_existe"));
            Assert.IsNull(_catalog.GetItem("no_existe"));
        }

        [Test]
        public void ItemsOfCategory_Food_ReturnsOnlyFood()
        {
            var items = _catalog.ItemsOfCategory(ItemCategory.Food);
            foreach (var item in items)
                Assert.AreEqual(ItemCategory.Food, item.Category);
        }

        [Test]
        public void ItemsOfCategory_FinishesWall_ReturnsWallpaper()
        {
            int count = 0;
            foreach (var item in _catalog.ItemsOfCategory(ItemCategory.Wallpaper))
            {
                Assert.AreEqual(ItemCategory.Wallpaper, item.Category);
                count++;
            }
            Assert.GreaterOrEqual(count, 1, "debe haber al menos un acabado de pared");
        }

        [Test]
        public void ItemsOfCategory_FinishesFloor_ReturnsFlooring()
        {
            int count = 0;
            foreach (var item in _catalog.ItemsOfCategory(ItemCategory.Flooring))
            {
                Assert.AreEqual(ItemCategory.Flooring, item.Category);
                count++;
            }
            Assert.GreaterOrEqual(count, 1, "debe haber al menos un acabado de suelo");
        }

        [Test]
        public void Furniture_HasCorrectFootprint()
        {
            var mesa = _catalog.GetItem("test_mesa");
            Assert.AreEqual(2, mesa.FootprintX);
            Assert.AreEqual(2, mesa.FootprintY);

            var snack = _catalog.GetItem("test_snack");
            Assert.AreEqual(1, snack.FootprintX, "comida: footprint debe ser 1");
            Assert.AreEqual(1, snack.FootprintY, "comida: footprint debe ser 1");
        }

        // ── 2. monedero ─────────────────────────────────────────────────────────

        [Test]
        public void TrySpend_EnoughCoins_ReturnsTrueAndDeducts()
        {
            long before = _save.Wallet.Coins;
            bool result = _service.TrySpend(30, "test");
            Assert.IsTrue(result);
            Assert.AreEqual(before - 30, _save.Wallet.Coins);
            Assert.AreEqual(30, _save.Wallet.TotalSpent);
        }

        [Test]
        public void TrySpend_NotEnoughCoins_ReturnsFalseAndLeavesBalance()
        {
            long before = _save.Wallet.Coins;
            long beforeSpent = _save.Wallet.TotalSpent;
            bool result = _service.TrySpend(999, "test");
            Assert.IsFalse(result);
            Assert.AreEqual(before, _save.Wallet.Coins, "el saldo no debió cambiar");
            Assert.AreEqual(beforeSpent, _save.Wallet.TotalSpent, "totalSpent no debió cambiar");
        }

        [Test]
        public void TrySpend_Zero_ReturnsFalse()
        {
            Assert.IsFalse(_service.TrySpend(0, "test"));
            Assert.IsFalse(_service.TrySpend(-5, "test"));
        }

        [Test]
        public void AddCoins_PublishesCoinsChanged()
        {
            CoinsChanged? last = null;
            Action<CoinsChanged> handler = e => last = e;
            EventBus.Subscribe(handler);

            _service.AddCoins(25, "regalo");
            Assert.IsTrue(last.HasValue);
            Assert.AreEqual(25, last.Value.Delta);
            Assert.AreEqual("regalo", last.Value.Reason);

            EventBus.Unsubscribe(handler);
        }

        [Test]
        public void TrySpend_PublishesCoinsChanged()
        {
            CoinsChanged? last = null;
            Action<CoinsChanged> handler = e => last = e;
            EventBus.Subscribe(handler);

            _service.TrySpend(20, "compra");
            Assert.IsTrue(last.HasValue);
            Assert.AreEqual(-20, last.Value.Delta);

            EventBus.Unsubscribe(handler);
        }

        // ── 3. compra ───────────────────────────────────────────────────────────

        [Test]
        public void TryBuy_EnoughMoneyAndUnlocked_Succeeds()
        {
            bool result = _service.TryBuy("test_snack", 2);
            Assert.IsTrue(result);
            Assert.AreEqual(2, _save.Inventory.CountOf("test_snack"));
            // price 5 × 2 = 10 descontados
            Assert.AreEqual(90, _save.Wallet.Coins);
        }

        [Test]
        public void TryBuy_BlockedByLevel_FailsEvenWithMoney()
        {
            _save.Wallet = new Wallet { Coins = 5000 };
            // test_gourmet unlockLevel = 30, max islander level = 1
            bool result = _service.TryBuy("test_gourmet");
            Assert.IsFalse(result);
            Assert.AreEqual(5000, _save.Wallet.Coins,
                "el dinero no debió tocarse");
            Assert.AreEqual(0, _save.Inventory.CountOf("test_gourmet"),
                "el objeto no debió aparecer en el inventario");
        }

        [Test]
        public void TryBuy_NotEnoughMoney_Fails()
        {
            _save.Wallet = new Wallet { Coins = 3 };
            bool result = _service.TryBuy("test_snack"); // price = 5
            Assert.IsFalse(result);
            Assert.AreEqual(3, _save.Wallet.Coins);
            Assert.AreEqual(0, _save.Inventory.CountOf("test_snack"));
        }

        [Test]
        public void TryBuy_UnknownItem_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("no_existe"));
            bool result = _service.TryBuy("no_existe");
            Assert.IsFalse(result);
        }

        [Test]
        public void TryBuy_PublishesItemAcquired()
        {
            ItemAcquired? last = null;
            Action<ItemAcquired> handler = e => last = e;
            EventBus.Subscribe(handler);

            _service.TryBuy("test_silla");
            Assert.IsTrue(last.HasValue);
            Assert.AreEqual("test_silla", last.Value.CatalogId);
            Assert.AreEqual(1, last.Value.Quantity);

            EventBus.Unsubscribe(handler);
        }

        // ── 4. stock diario ─────────────────────────────────────────────────────

        [Test]
        public void StockOf_SameDay_ReturnsSameStock()
        {
            var a = _service.StockOf("NimboMart");
            var b = _service.StockOf("NimboMart");
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.AreEqual(a[i], b[i], $"posición {i}: mismo día → mismo item");
        }

        [Test]
        public void StockOf_DifferentDay_ReturnsDifferentStock()
        {
            var day1 = _service.StockOf("NimboMart");

            // Avanzar al día siguiente
            _save.ElapsedMinutes += GameClock.MinutesPerDay;
            EventBus.Publish(new DayPassed(2));

            var day2 = _service.StockOf("NimboMart");

            // Con 2 items Food × 12 slots y shuffle determinista por día,
            // el orden DEBERÍA cambiar (probabilidad de coincidencia ≈ 0).
            bool same = true;
            if (day1.Count == day2.Count)
            {
                for (int i = 0; i < day1.Count && same; i++)
                    if (day1[i] != day2[i]) same = false;
            }
            else same = false;

            Assert.IsFalse(same,
                "el stock de dos días distintos no debería ser idéntico");
        }

        [Test]
        public void StockOf_UnknownShop_ReturnsEmpty()
        {
            var stock = _service.StockOf("TiendaQueNoExiste");
            Assert.AreEqual(0, stock.Count);
        }

        // ── 5. regalar ──────────────────────────────────────────────────────────

        [Test]
        public void GiveTo_NotOwned_ReturnsFalseAndNoEvent()
        {
            bool result = _service.GiveTo("test_islander", "test_snack");
            Assert.IsFalse(result);
            Assert.IsFalse(_giftFired, "no debe publicar ItemGifted si no tiene el objeto");
        }

        [Test]
        public void GiveTo_LovedItem_AppliesPositiveHappiness()
        {
            // Dar al jugador el snack que el isleño ama
            _save.Inventory.Add("test_snack", 3);
            _sim.Reset();

            bool result = _service.GiveTo("test_islander", "test_snack");
            Assert.IsTrue(result);
            Assert.AreEqual(2, _save.Inventory.CountOf("test_snack"),
                "debe quedar una unidad menos");

            Assert.IsTrue(_giftFired);
            Assert.AreEqual("test_snack", _lastGift.CatalogId);
            Assert.AreEqual("test_islander", _lastGift.IslanderId);
            Assert.AreEqual(1, _lastGift.Opinion, "al isleño le encanta test_snack");

            Assert.AreEqual("test_islander", _sim.LastIslanderId);
            Assert.Greater(_sim.LastDelta, 0f, "regalo amado → felicidad positiva");
        }

        [Test]
        public void GiveTo_HatedItem_AppliesNegativeHappiness()
        {
            _save.Inventory.Add("test_gourmet", 2);
            _sim.Reset();

            bool result = _service.GiveTo("test_islander", "test_gourmet");
            Assert.IsTrue(result);
            Assert.AreEqual(1, _save.Inventory.CountOf("test_gourmet"));

            Assert.IsTrue(_giftFired);
            Assert.AreEqual(-1, _lastGift.Opinion);

            Assert.AreEqual("test_islander", _sim.LastIslanderId);
            Assert.Less(_sim.LastDelta, 0f, "regalo odiado → felicidad negativa");
        }

        [Test]
        public void GiveTo_NeutralItem_AppliesSmallPositiveHappiness()
        {
            _save.Inventory.Add("test_camiseta", 1);
            _sim.Reset();

            bool result = _service.GiveTo("test_islander", "test_camiseta");
            Assert.IsTrue(result);
            Assert.AreEqual(0, _save.Inventory.CountOf("test_camiseta"));

            Assert.IsTrue(_giftFired);
            Assert.AreEqual(0, _lastGift.Opinion,
                "la ropa no está en loved/hated → indiferente");

            Assert.AreEqual("test_islander", _sim.LastIslanderId);
            Assert.Greater(_sim.LastDelta, 0f, "incluso un regalo neutro da algo de felicidad");
        }

        [Test]
        public void GiveTo_UnknownIslander_ReturnsFalse()
        {
            _save.Inventory.Add("test_snack", 1);
            bool result = _service.GiveTo("no_existe", "test_snack");
            Assert.IsFalse(result);
            Assert.AreEqual(1, _save.Inventory.CountOf("test_snack"),
                "el inventario no debió cambiar");
        }

        // ── 6. integridad del inventario ────────────────────────────────────────

        [Test]
        public void TryBuy_QuantityZero_ReturnsFalse()
        {
            Assert.IsFalse(_service.TryBuy("test_snack", 0));
        }

        [Test]
        public void Inventory_AccumulatesCorrectly()
        {
            _service.TryBuy("test_snack", 1);
            _service.TryBuy("test_snack", 2);
            Assert.AreEqual(3, _save.Inventory.CountOf("test_snack"));
        }
    }

    // ── dobles de test ──────────────────────────────────────────────────────────

    sealed class TestIslanderRegistry : IIslanderRegistry
    {
        readonly List<IslanderData> _islanders = new List<IslanderData>();
        public IReadOnlyList<IslanderData> All => _islanders;
        public int Count => _islanders.Count;
        public TestIslanderRegistry(IslanderData islander) => _islanders.Add(islander);
        public IslanderData Get(string id) => _islanders.Find(i => i.Id == id);
        public bool TryGet(string id, out IslanderData i)
        {
            i = Get(id); return i != null;
        }
        public bool Exists(string id) => Get(id) != null;
        public IEnumerable<IslanderData> InZone(string zoneId) { yield break; }
        public void Add(IslanderData islander) => _islanders.Add(islander);
        public void Remove(string id) => _islanders.RemoveAll(i => i.Id == id);
    }

    sealed class TestSimulationService : ISimulationService
    {
        public string LastIslanderId;
        public float LastDelta;
        public void Reset() { LastIslanderId = null; LastDelta = 0f; }
        public void ApplyHappiness(string islanderId, float delta)
        {
            LastIslanderId = islanderId; LastDelta = delta;
        }
        public void ApplyNeed(string islanderId, NeedKind need, float delta) { }
        public void SetNeed(string islanderId, NeedKind need, float value) { }
        public void ShowEmotion(string islanderId, Emotion emotion, float seconds = 4f) { }
        public void GrantExperience(string islanderId, float amount) { }
        public void SetSimulationPaused(bool paused) { }
    }
}
