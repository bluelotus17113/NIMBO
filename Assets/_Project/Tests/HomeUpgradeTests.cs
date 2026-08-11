using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Housing;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Housing;
using NUnit.Framework;

namespace Nimbo.Tests
{
    public class HomeUpgradeTests
    {
        SaveGame _save;
        HomeRecord _home;
        IslanderData _islander;
        TestIslanderRegistry _registry;
        TestEconomyService _economy;
        HomeUpgradeService _service;

        const string IslanderId = "test_islander";
        const string BuildingId = "test_building";
        const int UnitIndex = 0;

        [SetUp]
        public void SetUp()
        {
            _save = new SaveGame();
            _save.Wallet = new Wallet { Coins = 10000 };

            _home = new HomeRecord
            {
                BuildingId = BuildingId,
                UnitIndex = UnitIndex,
                Layout = RoomLayout.Starter(),
            };
            _save.Homes.Add(_home);

            _islander = new IslanderData();
            _islander.Identity.Id = IslanderId;
            _islander.Home = new HomeAssignment { BuildingId = BuildingId, UnitIndex = UnitIndex };

            _registry = new TestIslanderRegistry(_islander);
            _economy = new TestEconomyService(10000);
            _service = new HomeUpgradeService(_save, _registry, _economy);
        }

        // ── 1. Una casa nueva está en nivel 0 y mide 8×8 ─────────────────────────

        [Test]
        public void NewHome_IsLevel0_AndSizeIs8x8()
        {
            Assert.AreEqual(0, _service.LevelOf(IslanderId));
            Assert.AreEqual(8, _service.SizeOfLevel(0));
            Assert.AreEqual(8, _home.Layout.Width);
            Assert.AreEqual(8, _home.Layout.Height);
        }

        // ── 2. Ampliar con dinero sube a nivel 1, 11×11 y cobra 1.200 ────────────

        [Test]
        public void Upgrade_WithEnoughCoins_GoesToLevel1_Size11x11_Charges1200()
        {
            long before = _economy.Wallet.Coins;

            bool result = _service.Upgrade(IslanderId);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _service.LevelOf(IslanderId));
            Assert.AreEqual(1, _home.UpgradeLevel);
            Assert.AreEqual(11, _home.Layout.Width);
            Assert.AreEqual(11, _home.Layout.Height);
            Assert.AreEqual(1200, _economy.LastSpent);
            Assert.AreEqual(before - 1200, _economy.Wallet.Coins);
        }

        // ── 3. Sin dinero devuelve NotEnoughCoins, no cobra y no toca nada ────────

        [Test]
        public void Upgrade_WithoutEnoughCoins_ReturnsFalse_DoesNotCharge_LeavesHouseIntact()
        {
            _economy = new TestEconomyService(100); // no llega a 1.200
            _service = new HomeUpgradeService(_save, _registry, _economy);

            var rejection = _service.CanUpgrade(IslanderId);
            Assert.AreEqual(UpgradeRejection.NotEnoughCoins, rejection);

            bool result = _service.Upgrade(IslanderId);
            Assert.IsFalse(result);
            Assert.AreEqual(100, _economy.Wallet.Coins); // no cobró nada
            Assert.AreEqual(0, _service.LevelOf(IslanderId));
            Assert.AreEqual(8, _home.Layout.Width);
            Assert.AreEqual(8, _home.Layout.Height);
        }

        // ── 4. Dos ampliaciones seguidas llegan a 14×14; la tercera es MaxedOut ───

        [Test]
        public void TwoUpgrades_Reach14x14_ThirdReturnsMaxedOut()
        {
            _economy = new TestEconomyService(10000);
            _service = new HomeUpgradeService(_save, _registry, _economy);

            // primera
            Assert.IsTrue(_service.Upgrade(IslanderId));
            Assert.AreEqual(1, _service.LevelOf(IslanderId));
            Assert.AreEqual(11, _home.Layout.Width);

            // segunda
            Assert.IsTrue(_service.Upgrade(IslanderId));
            Assert.AreEqual(2, _service.LevelOf(IslanderId));
            Assert.AreEqual(14, _home.Layout.Width);

            // tercera: rechazada
            Assert.AreEqual(UpgradeRejection.MaxedOut, _service.CanUpgrade(IslanderId));
            Assert.IsFalse(_service.Upgrade(IslanderId));
            Assert.AreEqual(2, _service.LevelOf(IslanderId));
        }

        // ── 5. Habitante sin casa devuelve NoHome ──────────────────────────────────

        [Test]
        public void IslanderWithoutHome_ReturnsNoHome()
        {
            var homeless = new IslanderData();
            homeless.Identity.Id = "homeless";
            homeless.Home = default(HomeAssignment); // sin casa

            var registry = new TestIslanderRegistry(homeless);
            var service = new HomeUpgradeService(_save, registry, _economy);

            Assert.AreEqual(UpgradeRejection.NoHome, service.CanUpgrade("homeless"));
            Assert.IsFalse(service.Upgrade("homeless"));
        }

        // ── 6. La del suelo: patrón que distingue filas, comprobar casilla a casilla

        [Test]
        public void Upgrade_RemapsFloor_OriginalTilesStayAtSameCoordinates()
        {
            const string finishA = "floor_alfombra_roja";
            const string finishB = "floor_madera_clara";

            // Pintar la casa de 8×8: fila y=0 con acabado A, el resto con B.
            // Así cada fila tiene un acabado distinto que permite ver si algún índice
            // se ha descuadrado tras el remapeo.
            _home.Layout.FillFloor(finishB);
            for (int x = 0; x < 8; x++)
                _home.Layout.FloorTiles[_home.Layout.CellIndex(new GridCoord(x, 0))] = finishA;

            Assert.IsTrue(_service.Upgrade(IslanderId));
            Assert.AreEqual(11, _home.Layout.Width);
            Assert.AreEqual(11, _home.Layout.Height);

            // Cada coordenada (x,y) de la rejilla original debe conservar su acabado
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    string expected = y == 0 ? finishA : finishB;
                    string actual = _home.Layout.FloorAt(new GridCoord(x, y));
                    Assert.AreEqual(expected, actual,
                        $"({x},{y}) debía ser {expected} tras ampliar, pero es {actual}");
                }
            }

            // Verificación extra: la fila 0 de las 3 columnas nuevas también debe ser B
            // (no se hereda A porque esas columnas no existían)
            for (int x = 8; x < 11; x++)
                Assert.AreEqual(finishB, _home.Layout.FloorAt(new GridCoord(x, 0)));
        }

        // ── 7. Los muebles siguen en las mismas coordenadas ────────────────────────

        [Test]
        public void Upgrade_PlacedObjects_StayAtSameCoordinates()
        {
            var sofa = new PlacedObject
            {
                InstanceId = "sofa_1",
                CatalogId = "sofa",
                Origin = new GridCoord(2, 3),
                Facing = Facing.East,
                Layer = PlacementLayer.Furniture,
            };
            _home.Layout.Objects.Add(sofa);

            Assert.IsTrue(_service.Upgrade(IslanderId));

            Assert.AreEqual(1, _home.Layout.Objects.Count,
                "la lista de objetos no debe cambiar de tamaño");
            Assert.AreEqual(2, _home.Layout.Objects[0].Origin.X);
            Assert.AreEqual(3, _home.Layout.Objects[0].Origin.Y);
            Assert.AreEqual("sofa_1", _home.Layout.Objects[0].InstanceId);
        }

        // ── 8. Las casillas nuevas existen y tienen acabado (no null ni vacío) ─────

        [Test]
        public void Upgrade_NewTiles_HaveFinishNotNullOrEmpty()
        {
            Assert.IsTrue(_service.Upgrade(IslanderId));

            for (int y = 0; y < 11; y++)
            {
                for (int x = 0; x < 11; x++)
                {
                    string tile = _home.Layout.FloorAt(new GridCoord(x, y));
                    Assert.IsNotNull(tile, $"Casilla ({x},{y}) es null");
                    Assert.IsNotEmpty(tile, $"Casilla ({x},{y}) es vacía");
                }
            }
        }

        // ── 9. Upgrade publica HomeUpgraded una vez, con nivel y tamaño nuevos ─────

        [Test]
        public void Upgrade_PublishesHomeUpgraded_WithCorrectLevelAndSize()
        {
            int fireCount = 0;
            HomeUpgraded lastEvent = default;
            Action<HomeUpgraded> handler = e => { fireCount++; lastEvent = e; };

            EventBus.Subscribe(handler);
            try
            {
                Assert.IsTrue(_service.Upgrade(IslanderId));

                Assert.AreEqual(1, fireCount,
                    "HomeUpgraded debe publicarse exactamente una vez");
                Assert.AreEqual(IslanderId, lastEvent.IslanderId);
                Assert.AreEqual(1, lastEvent.Level);
                Assert.AreEqual(11, lastEvent.Size);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        // ── 10. LevelOf lee del HomeRecord, no del tamaño de la rejilla ────────────

        [Test]
        public void LevelOf_ReadsFromHomeRecord_NotFromGridSize()
        {
            Assert.IsTrue(_service.Upgrade(IslanderId));
            Assert.AreEqual(1, _service.LevelOf(IslanderId));

            // Forzar el tamaño de la rejilla a otra cosa: el nivel no debe cambiar
            // porque se lee de UpgradeLevel, no del ancho de la habitación
            _home.Layout.Width = 20;
            _home.Layout.Height = 20;

            Assert.AreEqual(1, _service.LevelOf(IslanderId),
                "el nivel se lee de HomeRecord.UpgradeLevel, no del tamaño de la rejilla");
            Assert.AreEqual(1, _home.UpgradeLevel);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // Dobles de test
    // ═══════════════════════════════════════════════════════════════════════════════
    //
    // TestIslanderRegistry ya existe en EconomyTests.cs, dentro de este mismo
    // namespace. No lo redefinimos aquí para no duplicar.

    sealed class TestEconomyService : IEconomyService
    {
        public Wallet Wallet => _wallet;
        public Inventory Inventory => null;

        /// <summary>Cuánto se gastó en la última llamada a TrySpend.</summary>
        public long LastSpent;
        /// <summary>Motivo de la última llamada a TrySpend.</summary>
        public string LastReason;

        private Wallet _wallet;

        public TestEconomyService(long coins)
        {
            _wallet = new Wallet { Coins = coins };
        }

        public IItemDefinition GetItem(string catalogId) => null;
        public IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category) { yield break; }

        public void AddCoins(long amount, string reason)
        {
            // Wallet es un struct detrás de una propiedad: tocarlo en el sitio
            // modificaría una copia, y el compilador no lo deja. Se reconstruye.
            _wallet.Coins += amount;
        }

        public bool TrySpend(long amount, string reason)
        {
            if (_wallet.Coins < amount) return false;
            _wallet.Coins -= amount;
            LastSpent = amount;
            LastReason = reason;
            return true;
        }

        public bool TryBuy(string catalogId, int quantity = 1) => false;
        public IReadOnlyList<string> StockOf(string shopId) => null;
        public bool GiveTo(string islanderId, string catalogId) => false;
    }
}
