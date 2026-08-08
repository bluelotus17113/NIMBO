using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;

namespace Nimbo.Economy.Shops
{
    /// <summary>
    /// Qué vende una tienda, cuántos slots de stock saca cada día y cómo se llama.
    /// Los valores por defecto salen de <c>Docs/Contratos/economia.md</c> §6.
    /// </summary>
    public class ShopDefinition
    {
        public string ShopId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ItemCategory> Categories { get; }
        public int StockSlots { get; }

        public ShopDefinition(string shopId, string displayName, int stockSlots,
            params ItemCategory[] categories)
        {
            ShopId = shopId;
            DisplayName = displayName;
            StockSlots = stockSlots;
            Categories = categories;
        }

        // ── Las cinco tiendas del contrato ──────────────────────────────────────

        public static readonly ShopDefinition NimboMart = new ShopDefinition(
            "NimboMart", "NimboMart", 12,
            ItemCategory.Food, ItemCategory.Consumable, ItemCategory.Gift);

        public static readonly ShopDefinition MueblesNimbo = new ShopDefinition(
            "Muebles Nimbo", "Muebles Nimbo", 8,
            ItemCategory.Furniture);

        public static readonly ShopDefinition BoutiqueCeleste = new ShopDefinition(
            "Boutique Celeste", "Boutique Celeste", 10,
            ItemCategory.Clothing);

        public static readonly ShopDefinition AntiguedadesNimbo = new ShopDefinition(
            "Antigüedades Nimbo", "Antigüedades Nimbo", 4,
            ItemCategory.Decoration, ItemCategory.Wallpaper, ItemCategory.Flooring);

        public static readonly ShopDefinition MercadoFlotante = new ShopDefinition(
            "Mercado flotante", "Mercado flotante", 6,
            ItemCategory.Food, ItemCategory.Clothing, ItemCategory.Furniture,
            ItemCategory.Decoration, ItemCategory.Gift, ItemCategory.Consumable,
            ItemCategory.Wallpaper, ItemCategory.Flooring);

        public static readonly IReadOnlyList<ShopDefinition> All = new List<ShopDefinition>
        {
            NimboMart, MueblesNimbo, BoutiqueCeleste, AntiguedadesNimbo, MercadoFlotante,
        };

        public static ShopDefinition Get(string shopId)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].ShopId == shopId) return All[i];
            return null;
        }
    }
}
