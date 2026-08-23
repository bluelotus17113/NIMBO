using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Economy;
using Nimbo.Data.Housing;
using Nimbo.Data.Player;
using Nimbo.Data.Save;
using Nimbo.Housing;
using Nimbo.Housing.Catalog;
using Nimbo.Player;
using Nimbo.UI.Player;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Los acabados de vivienda existen de verdad: los ids por defecto están en el
    /// catálogo, ampliar la casa no estrena casillas con un id inventado, y el panel
    /// de amueblar aplica los que llevas en la mochila.
    /// </summary>
    /// <remarks>
    /// Esta prueba es la que habría cazado la enfermedad original: <c>RoomLayout</c>
    /// arrancaba con <c>wall_liso_crema</c> y <c>floor_madera_clara</c>, ids que no
    /// estaban en ningún catálogo. No rompía nada porque quien pintaba ignoraba el
    /// id —y ahí estaba exactamente el problema.
    /// </remarks>
    public class AcabadosDeViviendaTests
    {
        FurnitureCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            // El catálogo de verdad, el de Resources: aquí lo que se prueba es que
            // los ids que el código usa por defecto existen en el JSON que vende las
            // tiendas, no en un montaje de test que los definiría para siempre sí.
            _catalog = new FurnitureCatalog();
        }

        [TearDown]
        public void TearDown() => ServiceRegistry.Clear();

        // ── 1. Los ids por defecto existen ────────────────────────────────────────

        [Test]
        public void LosAcabadosPorDefectoExistenEnElCatalogo()
        {
            Assert.IsTrue(_catalog.TryGetFinish(RoomLayout.DefaultWallpaper, out var pared),
                          $"el papel por defecto '{RoomLayout.DefaultWallpaper}' no está en catalogo_acabados.json");
            Assert.AreEqual("wall", pared.Surface,
                            "el papel por defecto debe ser un acabado de pared");

            Assert.IsTrue(_catalog.TryGetFinish(RoomLayout.DefaultFloor, out var suelo),
                          $"el suelo por defecto '{RoomLayout.DefaultFloor}' no está en catalogo_acabados.json");
            Assert.AreEqual("floor", suelo.Surface,
                            "el suelo por defecto debe ser un acabado de suelo");
        }

        [Test]
        public void LaCasaDeEstrenoEstrenaAcabadosQueExisten()
        {
            var room = RoomLayout.Starter();

            Assert.IsTrue(_catalog.TryGetFinish(room.WallpaperNorth, out var norte) &&
                          norte.Surface == "wall",
                          $"la pared norte de serie ('{room.WallpaperNorth}') no es un papel del catálogo");
            Assert.IsTrue(_catalog.TryGetFinish(room.WallpaperWest, out var oeste) &&
                          oeste.Surface == "wall",
                          $"la pared oeste de serie ('{room.WallpaperWest}') no es un papel del catálogo");

            Assert.Greater(room.FloorTiles.Count, 0, "la casa de estreno sale sin suelo puesto");
            foreach (var tile in room.FloorTiles)
                Assert.IsTrue(_catalog.TryGetFinish(tile, out var finish) && finish.Surface == "floor",
                              $"'{tile}' no es un suelo del catálogo");
        }

        // ── 2. Ampliar no estrena casillas con un id inventado ────────────────────

        [Test]
        public void AmpliarLaCasaRellenaLoNuevoConUnAcabadoQueExiste()
        {
            var save = new SaveGame();
            save.Wallet = new Wallet { Coins = 100000 };
            save.Player.Home = RoomLayout.Starter();

            // Sin mochila el servicio deja pasar la obra: es el camino corto de los
            // tests, y aquí lo que se mira es el remapeo del suelo, no la obra.
            var service = new HomeUpgradeService(save, null, new EconomiaDePrueba(100000), null);

            Assert.IsTrue(service.UpgradePlayerHome(), "no se pudo ampliar la cabaña de prueba");

            foreach (var tile in save.Player.Home.FloorTiles)
                Assert.IsTrue(_catalog.TryGetFinish(tile, out var finish) && finish.Surface == "floor",
                              $"tras ampliar hay una casilla con '{tile}', que no es un suelo del catálogo");
        }

        // ── 3. El panel aplica acabados y los gasta ───────────────────────────────

        [Test]
        public void ElPanelAplicaUnPapelGuardadoYGastaLaUnidad()
        {
            const string papel = "wall_cielo_diurno";

            var economy = new EconomiaDePrueba(1000,
                new ObjetoDePrueba(papel, ItemCategory.Wallpaper, "Pared cielo diurno"));
            economy.Bag.Add(papel, 1);

            var player = new PlayerService(new PlayerState(), new GameClock());
            player.State.Home = RoomLayout.Starter();

            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<IHousingService>(new HousingService(_catalog));
            ServiceRegistry.Register(player);

            int reformas = 0;
            Action<RoomEdited> contar = _ => reformas++;
            EventBus.Subscribe(contar);
            try
            {
                var panel = new FurnishPanel();
                panel.Show();

                Assert.IsTrue(panel.TryApplyFinish(papel), "el panel no aplicó el papel teniendo la unidad");
                Assert.AreEqual(papel, player.State.Home.WallpaperNorth, "la pared norte no cambió");
                Assert.AreEqual(papel, player.State.Home.WallpaperWest, "la pared oeste no cambió");
                Assert.AreEqual(0, economy.Bag.CountOf(papel), "aplicar no gastó la unidad");
                Assert.AreEqual(1, reformas,
                                "aplicar un acabado debe avisar de la reforma: de ese aviso vive el repintado y los logros de decoración");
            }
            finally
            {
                EventBus.Unsubscribe(contar);
            }
        }

        [Test]
        public void ElPanelAplicaUnSueloEnteroYGastaLaUnidad()
        {
            const string suelo = "floor_suelo_de_baldosa_damero";

            var economy = new EconomiaDePrueba(1000,
                new ObjetoDePrueba(suelo, ItemCategory.Flooring, "Suelo de baldosa damero"));
            economy.Bag.Add(suelo, 1);

            var player = new PlayerService(new PlayerState(), new GameClock());
            player.State.Home = RoomLayout.Starter();

            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<IHousingService>(new HousingService(_catalog));
            ServiceRegistry.Register(player);

            var panel = new FurnishPanel();
            panel.Show();

            Assert.IsTrue(panel.TryApplyFinish(suelo), "el panel no aplicó el suelo teniendo la unidad");

            var home = player.State.Home;
            for (int y = 0; y < home.Height; y++)
                for (int x = 0; x < home.Width; x++)
                    Assert.AreEqual(suelo, home.FloorAt(new GridCoord(x, y)),
                                    $"la casilla ({x},{y}) no quedó con el suelo aplicado");

            Assert.AreEqual(0, economy.Bag.CountOf(suelo), "aplicar no gastó la unidad");
        }

        [Test]
        public void AplicarSinUnidadesOYaPuestoNoGastaNiCambiaNada()
        {
            const string papel = "wall_verde_botanico";

            var economy = new EconomiaDePrueba(1000,
                new ObjetoDePrueba(papel, ItemCategory.Wallpaper, "Pared verde botánico"));
            var player = new PlayerService(new PlayerState(), new GameClock());
            player.State.Home = RoomLayout.Starter();

            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<IHousingService>(new HousingService(_catalog));
            ServiceRegistry.Register(player);

            var panel = new FurnishPanel();
            panel.Show();

            // Sin unidades: ni aplica ni gasta.
            Assert.IsFalse(panel.TryApplyFinish(papel), "aplicó un acabado que no se llevaba");
            Assert.AreEqual(RoomLayout.DefaultWallpaper, player.State.Home.WallpaperNorth);

            // Ya aplicado en toda la superficie: repintar lo igual no cuesta pintura.
            economy.Bag.Add(papel, 1);
            Assert.IsTrue(panel.TryApplyFinish(papel));
            economy.Bag.Add(papel, 1);
            Assert.IsFalse(panel.TryApplyFinish(papel),
                           "repintar lo mismo volvió a gastar una unidad");
            Assert.AreEqual(1, economy.Bag.CountOf(papel),
                            "quedan más unidades gastadas de las que cuestan dos aplicaciones");
        }

        [Test]
        public void LoQueNoEsUnAcabadoNoSeAplicaComoTal()
        {
            // Un mueble sigue siendo colocable casilla a casilla; TryApplyFinish es
            // solo para papeles y suelos. Devolver falso aquí es lo que impide que un
            // clic despistado convierta una silla en una reforma.
            const string silla = "furn_silla_de_madera_sencilla";

            var economy = new EconomiaDePrueba(1000,
                new ObjetoDePrueba(silla, ItemCategory.Furniture, "Silla"));
            economy.Bag.Add(silla, 1);

            var player = new PlayerService(new PlayerState(), new GameClock());
            player.State.Home = RoomLayout.Starter();

            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<IHousingService>(new HousingService(_catalog));
            ServiceRegistry.Register(player);

            var panel = new FurnishPanel();
            panel.Show();

            Assert.IsFalse(panel.TryApplyFinish(silla), "un mueble no se aplica como acabado");
            Assert.AreEqual(1, economy.Bag.CountOf(silla), "el mueble se gastó como si fuera pintura");
        }
    }

    /// <summary>Economía de prueba: monedas, mochila real y un catálogo mínimo.</summary>
    sealed class EconomiaDePrueba : IEconomyService
    {
        // Sin readonly: Wallet es un struct y AddCoins/TrySpend mutan sus campos.
        Wallet _wallet;
        readonly Dictionary<string, IItemDefinition> _items = new Dictionary<string, IItemDefinition>();

        /// <summary>La mochila es la de verdad: Add/Remove/CountOf son el código de producción.</summary>
        public readonly Inventory Bag = new Inventory();

        public EconomiaDePrueba(long coins, params IItemDefinition[] items)
        {
            _wallet = new Wallet { Coins = coins };
            foreach (var item in items) _items[item.CatalogId] = item;
        }

        public Wallet Wallet => _wallet;
        public Inventory Inventory => Bag;

        public IItemDefinition GetItem(string catalogId) =>
            _items.TryGetValue(catalogId, out var item) ? item : null;

        public IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category)
        {
            foreach (var item in _items.Values)
                if (item.Category == category) yield return item;
        }

        public void AddCoins(long amount, string reason) => _wallet.Coins += amount;

        public bool TrySpend(long amount, string reason)
        {
            if (_wallet.Coins < amount) return false;
            _wallet.Coins -= amount;
            return true;
        }

        public bool TryBuy(string catalogId, int quantity = 1) => false;
        public IReadOnlyList<string> StockOf(string shopId) => Array.Empty<string>();
        public bool GiveTo(string islanderId, string catalogId) => false;
    }

    /// <summary>Objeto de catálogo de prueba: solo lo que el panel lee.</summary>
    sealed class ObjetoDePrueba : IItemDefinition
    {
        public ObjetoDePrueba(string id, ItemCategory category, string name)
        {
            CatalogId = id;
            Category = category;
            DisplayName = name;
        }

        public string CatalogId { get; }
        public string DisplayName { get; }
        public string Description => "";
        public ItemCategory Category { get; }
        public int Price => 10;
        public int UnlockLevel => 1;
        public int FootprintX => 1;
        public int FootprintY => 1;
        public int HungerRestore => 0;
        public ToolKind Tool => ToolKind.None;
        public int ToolTier => 1;

        // Ropa (se pidió al contrato mientras se escribía esta prueba): a un acabado
        // no le aplica nada de esto.
        public string Slot => null;
        public string Style => null;
        public string[] Palette => null;
    }
}
