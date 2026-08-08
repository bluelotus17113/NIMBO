using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Save;
using Nimbo.Economy.Items;
using Nimbo.Economy.Shops;

namespace Nimbo.Economy
{
    /// <summary>
    /// Implementación completa de <see cref="IEconomyService"/>: monedero, inventario,
    /// catálogo, compra, regalo y rotación de tiendas.
    /// </summary>
    public class EconomyService : IEconomyService, IDisposable
    {
        readonly ItemCatalog _catalog;
        readonly SaveGame _save;
        int _currentDay;

        // Cache de stock: key = "{shopId}:{day}", se vacía al cambiar de día.
        readonly Dictionary<string, IReadOnlyList<string>> _stockCache =
            new Dictionary<string, IReadOnlyList<string>>();

        public Nimbo.Data.Economy.Wallet Wallet => _save.Wallet;
        public Nimbo.Data.Economy.Inventory Inventory => _save.Inventory;

        /// <summary>
        /// Constructor de producción. Recibe el catálogo ya cargado y la partida
        /// activa para leer y escribir el monedero y el inventario.
        /// </summary>
        public EconomyService(ItemCatalog catalog, SaveGame save)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _currentDay = (int)(save.ElapsedMinutes / GameClock.MinutesPerDay) + 1;
            EventBus.Subscribe<DayPassed>(OnDayPassed);
        }

        void OnDayPassed(DayPassed evt)
        {
            _currentDay = evt.Day;
            _stockCache.Clear();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<DayPassed>(OnDayPassed);
            _stockCache.Clear();
        }

        // ── catálogo ────────────────────────────────────────────────────────────

        public IItemDefinition GetItem(string catalogId) => _catalog.GetItem(catalogId);

        public IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category) =>
            _catalog.ItemsOfCategory(category);

        // ── monedero ────────────────────────────────────────────────────────────

        public void AddCoins(long amount, string reason)
        {
            if (amount == 0) return;

            var w = _save.Wallet;      // struct → copia local
            w.Coins += amount;
            if (amount > 0) w.TotalEarned += amount;
            _save.Wallet = w;          // escribir de vuelta

            EventBus.Publish(new CoinsChanged(amount, w.Coins, reason));
        }

        public bool TrySpend(long amount, string reason)
        {
            if (amount <= 0) return false;

            var w = _save.Wallet;
            if (!w.CanAfford(amount)) return false;

            w.Coins -= amount;
            w.TotalSpent += amount;
            _save.Wallet = w;

            EventBus.Publish(new CoinsChanged(-amount, w.Coins, reason));
            return true;
        }

        // ── compra ──────────────────────────────────────────────────────────────

        public bool TryBuy(string catalogId, int quantity = 1)
        {
            if (quantity <= 0) return false;

            var item = _catalog.GetItem(catalogId);
            if (item == null) return false;

            if (item.UnlockLevel > GetMaxIslanderLevel()) return false;

            long total = (long)item.Price * quantity;
            if (!TrySpend(total, $"Comprar {item.DisplayName} x{quantity}"))
                return false;

            Inventory.Add(catalogId, quantity);
            EventBus.Publish(new ItemAcquired(catalogId, quantity));
            return true;
        }

        /// <summary>
        /// Nivel más alto entre todos los habitantes de la isla.
        /// Si no hay registro o no hay habitantes, devuelve 1.
        /// </summary>
        int GetMaxIslanderLevel()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry))
                return 1;

            int max = 0;
            var all = registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Progression.Level > max)
                    max = all[i].Progression.Level;

            return max > 0 ? max : 1;
        }

        // ── tiendas ─────────────────────────────────────────────────────────────

        public IReadOnlyList<string> StockOf(string shopId)
        {
            var shop = ShopDefinition.Get(shopId);
            if (shop == null) return Array.Empty<string>();

            string key = $"{shopId}:{_currentDay}";
            if (_stockCache.TryGetValue(key, out var cached)) return cached;

            var stock = ShopStock.Generate(shop, _currentDay, _catalog);
            _stockCache[key] = stock;
            return stock;
        }

        // ── regalar ─────────────────────────────────────────────────────────────

        /// <summary>A cuánto ánimo equivale cada opinión de regalo.</summary>
        /// <remarks>Elegidos para que el rango útil de ±15 de
        /// <see cref="ISimulationService.ApplyHappiness"/> se use sin saturar:
        /// un regalo amado es un subidón (+10), uno odiado un bajón moderado (−5),
        /// y uno neutro un empujoncito (+3) porque recibir algo siempre alegra un poco.
        /// </remarks>
        const float LovedHappiness = 10f;
        const float NeutralHappiness = 3f;
        const float HatedHappiness = -5f;

        public bool GiveTo(string islanderId, string catalogId)
        {
            if (Inventory.CountOf(catalogId) <= 0) return false;
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return false;

            var islander = registry.Get(islanderId);
            if (islander == null) return false;

            int opinion = islander.Tastes.OpinionOf(catalogId);

            if (!Inventory.Remove(catalogId, 1)) return false;

            EventBus.Publish(new ItemGifted(catalogId, islanderId, opinion));

            float delta = opinion switch
            {
                1 => LovedHappiness,
                -1 => HatedHappiness,
                _ => NeutralHappiness,
            };

            if (ServiceRegistry.TryGet<ISimulationService>(out var sim))
                sim.ApplyHappiness(islanderId, delta);

            return true;
        }
    }
}
