using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Player;
using Nimbo.Items;
using NUnit.Framework;

namespace Nimbo.Tests
{
    public class InventoryTests
    {
        // ── doble mínimo de IEconomyService ──────────────────────────────────

        /// <summary>
        /// Doble anidado a propósito: los namespace de C# se fusionan entre ficheros,
        /// y "TestEconomyService" y "TestIslanderRegistry" ya existen sueltos en
        /// HomeUpgradeTests.cs y EconomyTests.cs respectivamente.
        /// </summary>
        class FakeEconomyService : IEconomyService
        {
            readonly System.Collections.Generic.Dictionary<string, IItemDefinition> _items = new();

            public FakeEconomyService WithItem(string catalogId, ItemCategory category)
            {
                _items[catalogId] = new FakeItemDef(catalogId, category);
                return this;
            }

            public IItemDefinition GetItem(string catalogId) =>
                _items.TryGetValue(catalogId, out var def) ? def : null;

            // ── lo que los tests no usan ──────────────────────────────────
            public Wallet Wallet => default;
            public Inventory Inventory => null;
            public System.Collections.Generic.IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category) =>
                System.Linq.Enumerable.Empty<IItemDefinition>();
            public void AddCoins(long amount, string reason) { }
            public bool TrySpend(long amount, string reason) => true;
            public bool TryBuy(string catalogId, int quantity = 1) => true;
            public System.Collections.Generic.IReadOnlyList<string> StockOf(string shopId) =>
                System.Array.Empty<string>();
            public bool GiveTo(string islanderId, string catalogId) => true;
        }

        /// <summary>Definición mínima para los tests: solo identificador y categoría.</summary>
        class FakeItemDef : IItemDefinition
        {
            public string CatalogId { get; }
            public string DisplayName => CatalogId;
            public string Description => "";
            public ItemCategory Category { get; }
            public int Price => 0;
            public int UnlockLevel => 0;
            public int FootprintX => 1;
            public int FootprintY => 1;

            public FakeItemDef(string catalogId, ItemCategory category)
            {
                CatalogId = catalogId;
                Category = category;
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        PlayerState NewPlayer()
        {
            return new PlayerState
            {
                DisplayName = "TestPlayer",
                Bag = new Inventory(),
                SelectedSlot = 0,
            };
        }

        FakeEconomyService NewEconomy()
        {
            return new FakeEconomyService()
                .WithItem("manzana", ItemCategory.Food)
                .WithItem("madera", ItemCategory.Material)
                .WithItem("azada", ItemCategory.Tool)
                .WithItem("pico", ItemCategory.Tool)
                .WithItem("semilla_trigo", ItemCategory.Seed);
        }

        // ── 1. mochila nueva: 24 huecos, todos vacíos ────────────────────────

        [Test]
        public void NewBag_Has24EmptySlots()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            Assert.AreEqual(24, service.SlotCount);
            Assert.AreEqual(24, service.Slots.Count);

            for (int i = 0; i < 24; i++)
            {
                var slot = service.At(i);
                Assert.AreEqual(0, slot.Quantity, $"El hueco {i} debería estar vacío");
            }
        }

        // ── 2. guardar algo lo deja en el primer hueco libre ─────────────────

        [Test]
        public void TryStore_SingleItem_GoesToSlotZero()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            var result = service.TryStore("manzana", 5, out var leftover);

            Assert.AreEqual(StoreResult.Ok, result);
            Assert.AreEqual(0, leftover);
            Assert.AreEqual("manzana", service.At(0).CatalogId);
            Assert.AreEqual(5, service.At(0).Quantity);
            // El resto siguen vacíos
            Assert.AreEqual(0, service.At(1).Quantity);
        }

        // ── 3. guardar más de lo mismo rellena la pila antes de abrir otra ───

        [Test]
        public void TryStore_SameItem_FillsExistingStackFirst()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 10, out _);
            // Segunda tanda: debería sumarse a la pila del hueco 0, no ir al 1
            service.TryStore("manzana", 5, out _);

            Assert.AreEqual("manzana", service.At(0).CatalogId);
            Assert.AreEqual(15, service.At(0).Quantity);
            Assert.AreEqual(0, service.At(1).Quantity, "No debería haber abierto un segundo hueco");
        }

        // ── 4. pasado 99 abre una pila nueva en otro hueco ───────────────────

        [Test]
        public void TryStore_Over99_OpensNewStack()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 80, out _);
            // Con 80 en el hueco 0, añadir 30 más: 19 caben (hasta 99), 11 van a otro hueco
            var result = service.TryStore("manzana", 30, out var leftover);

            Assert.AreEqual(StoreResult.Ok, result);
            Assert.AreEqual(0, leftover);
            Assert.AreEqual(99, service.At(0).Quantity, "El primer hueco debería estar al tope");
            Assert.AreEqual("manzana", service.At(1).CatalogId);
            Assert.AreEqual(11, service.At(1).Quantity);
        }

        // ── 5. mochila llena: devuelve Full ──────────────────────────────────

        [Test]
        public void TryStore_FullBag_ReturnsFull()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            // Llenamos los 24 huecos con 99 manzanas cada uno
            for (int i = 0; i < 24; i++)
            {
                var r = service.TryStore("manzana", 99, out _);
                Assert.AreEqual(StoreResult.Ok, r, $"Llenando hueco {i}");
            }

            // Un hueco más no cabe
            var result = service.TryStore("manzana", 1, out var leftover);

            Assert.AreEqual(StoreResult.Full, result);
            Assert.AreEqual(1, leftover, "Lo que no cupo debería volver en leftover");
        }

        // ── 6. media mochila: Partial y leftover ─────────────────────────────

        [Test]
        public void TryStore_Partial_ReturnsPartialWithLeftover()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            // Dejamos solo 2 huecos libres
            for (int i = 0; i < 22; i++)
            {
                service.TryStore("madera", 99, out _);
            }

            // Quedan 2 huecos × 99 = 198. Pedimos 250: deberían sobrar 52
            var result = service.TryStore("madera", 250, out var leftover);

            Assert.AreEqual(StoreResult.Partial, result);
            Assert.AreEqual(52, leftover);
            Assert.AreEqual(24 * 99, service.CountOf("madera"));
        }

        // ── 7. huecos posicionales: vaciar uno no compacta la lista ──────────

        [Test]
        public void RemoveSlot_DoesNotCompact_EmptySlotStaysInPlace()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            // Ponemos tres objetos distintos en los tres primeros huecos
            service.TryStore("manzana", 1, out _);       // hueco 0
            service.TryStore("madera", 1, out _);        // hueco 1
            service.TryStore("semilla_trigo", 1, out _); // hueco 2

            // Vaciamos el hueco del medio
            service.Drop(1);

            // El hueco 1 debe estar vacío
            Assert.AreEqual(0, service.At(1).Quantity, "El hueco 1 debería estar vacío");

            // El hueco 2 sigue teniendo lo suyo: no se ha movido
            Assert.AreEqual("semilla_trigo", service.At(2).CatalogId,
                "El hueco 2 NO debería haberse movido: los huecos son posicionales");
            Assert.AreEqual(1, service.At(2).Quantity);

            // El hueco 0 también sigue igual
            Assert.AreEqual("manzana", service.At(0).CatalogId);
            Assert.AreEqual(1, service.At(0).Quantity);
        }

        // ── 8. las herramientas no se apilan ─────────────────────────────────

        [Test]
        public void TryStore_Tools_DoNotStack()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("azada", 1, out _);
            service.TryStore("azada", 1, out _);

            Assert.AreEqual("azada", service.At(0).CatalogId);
            Assert.AreEqual(1, service.At(0).Quantity,
                "Una herramienta no puede tener cantidad > 1");
            Assert.AreEqual("azada", service.At(1).CatalogId,
                "La segunda azada debería ocupar su propio hueco");
            Assert.AreEqual(1, service.At(1).Quantity);
        }

        // ── 9. Swap intercambia, y con hueco vacío también ───────────────────

        [Test]
        public void Swap_ExchangesTwoSlots()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 5, out _);   // hueco 0
            service.TryStore("madera", 10, out _);   // hueco 1

            service.Swap(0, 1);

            Assert.AreEqual("madera", service.At(0).CatalogId);
            Assert.AreEqual(10, service.At(0).Quantity);
            Assert.AreEqual("manzana", service.At(1).CatalogId);
            Assert.AreEqual(5, service.At(1).Quantity);
        }

        [Test]
        public void Swap_WithEmptySlot_Works()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 5, out _);   // hueco 0, hueco 1 vacío

            service.Swap(0, 1);

            Assert.AreEqual(0, service.At(0).Quantity, "El hueco 0 debería estar vacío tras el swap");
            Assert.AreEqual("manzana", service.At(1).CatalogId);
            Assert.AreEqual(5, service.At(1).Quantity);
        }

        // ── 10. ToolInHand ────────────────────────────────────────────────────

        [Test]
        public void ToolInHand_ReturnsTool_WhenSelectedSlotIsTool()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("azada", 1, out _);      // va al hueco 0, que es el seleccionado
            Assert.AreEqual(ToolKind.Hoe, service.ToolInHand);
        }

        [Test]
        public void ToolInHand_ReturnsNone_WhenSelectedSlotIsNotTool()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 1, out _);    // comida, no herramienta
            Assert.AreEqual(ToolKind.None, service.ToolInHand);
        }

        [Test]
        public void ToolInHand_ReturnsNone_WhenSelectedSlotIsEmpty()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            Assert.AreEqual(ToolKind.None, service.ToolInHand);
        }

        // ── 11. TryTake de algo que no lleva devuelve falso ──────────────────

        [Test]
        public void TryTake_ItemNotCarried_ReturnsFalseAndDoesNotTouch()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 5, out _);

            var taken = service.TryTake("madera", 1);

            Assert.IsFalse(taken);
            Assert.AreEqual(5, service.CountOf("manzana"),
                "Las manzanas no deberían haberse tocado");
            Assert.AreEqual(0, service.CountOf("madera"));
        }

        // ── 12. mochila corta se normaliza a 24 ──────────────────────────────

        [Test]
        public void Constructor_NormalizesShortBag_To24()
        {
            var player = NewPlayer();
            // Simulamos partida vieja: la mochila solo guardó 8 huecos
            player.Bag.Stacks.Clear();
            for (int i = 0; i < 8; i++)
                player.Bag.Stacks.Add(new ItemStack("manzana", 1));

            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            Assert.AreEqual(24, service.Slots.Count,
                "La mochila debería tener 24 huecos tras normalizar");
            // Los primeros 8 conservan su contenido
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual("manzana", service.At(i).CatalogId);
                Assert.AreEqual(1, service.At(i).Quantity);
            }
            // Los 16 restantes están vacíos
            for (int i = 8; i < 24; i++)
                Assert.AreEqual(0, service.At(i).Quantity, $"El hueco {i} debería estar vacío");
        }

        // ── pruebas extra de robustez ────────────────────────────────────────

        [Test]
        public void TryStore_ZeroQuantity_ReturnsOk()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            var result = service.TryStore("manzana", 0, out var leftover);
            Assert.AreEqual(StoreResult.Ok, result);
            Assert.AreEqual(0, leftover);
        }

        [Test]
        public void TryStore_UnknownItem_ReturnsUnknownItem()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            var result = service.TryStore("objeto_que_no_existe", 1, out _);
            Assert.AreEqual(StoreResult.UnknownItem, result);
        }

        [Test]
        public void TryConsumeSelected_ReducesQuantity()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.TryStore("manzana", 5, out _);           // hueco 0 seleccionado
            var consumed = service.TryConsumeSelected(2);

            Assert.IsTrue(consumed);
            Assert.AreEqual(3, service.At(0).Quantity);
        }

        [Test]
        public void TryTake_RemovesFromLastSlotsFirst()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            // Dos pilas de manzanas: 10 en hueco 0 y 10 en hueco 1
            service.TryStore("manzana", 10, out _);
            service.TryStore("manzana", 10, out _);
            // Nos aseguramos de que están en huecos distintos
            service.TryStore("madera", 1, out _);  // empuja la segunda pila
            // Esperamos: hueco 0 = 10, hueco 1 = 1 madera, hueco 2 = 10 — no, no es así.
            // TryStore rellena primero pilas existentes, luego huecos vacíos.
            // Primera pasada: hueco 0 = 99 manzanas.
            // Pero pusimos 10, luego 10. StackLimit de Food es 99, así que ambas caben en hueco 0.
            // Necesito forzar dos pilas separadas. La forma más limpia: usar Swap y Drop.
            // O mejor: usar el límite de pila. Con 99 en hueco 0, la segunda tanda abre hueco 1.

            // Vamos de nuevo con un approach más limpio:
            // Lleno hueco 0 hasta 99, luego añado 10 más que van a hueco 1
            var p2 = NewPlayer();
            var e2 = NewEconomy();
            var s2 = new InventoryService(p2, e2);
            s2.TryStore("manzana", 99, out _);   // hueco 0 lleno
            s2.TryStore("manzana", 10, out _);   // hueco 1: 10 manzanas

            // Ahora sacamos 5: deberían salir del hueco 1 (el último)
            s2.TryTake("manzana", 5);

            Assert.AreEqual(99, s2.At(0).Quantity, "El hueco 0 no debería haberse tocado");
            Assert.AreEqual(5, s2.At(1).Quantity, "Debería haber sacado del hueco 1");
        }

        [Test]
        public void Drop_OutOfRange_ReturnsFalse()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            Assert.IsFalse(service.Drop(-1));
            Assert.IsFalse(service.Drop(24));
        }

        [Test]
        public void Select_ClampsToHotbar()
        {
            var player = NewPlayer();
            var economy = NewEconomy();
            var service = new InventoryService(player, economy);

            service.Select(15); // fuera de la barra
            Assert.AreEqual(9, service.SelectedSlot, "Debería recortar al último de la barra");

            service.Select(-5);
            Assert.AreEqual(0, service.SelectedSlot, "Debería recortar al primero de la barra");
        }
    }
}
