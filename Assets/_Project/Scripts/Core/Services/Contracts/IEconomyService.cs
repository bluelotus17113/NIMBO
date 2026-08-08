using System.Collections.Generic;
using Nimbo.Data.Economy;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Familia de un objeto del catálogo. Decide dónde se compra y qué se puede hacer con él.</summary>
    public enum ItemCategory
    {
        Food = 0,
        Clothing = 1,
        Furniture = 2,
        Decoration = 3,
        Gift = 4,
        Consumable = 5,
        Wallpaper = 6,
        Flooring = 7,
    }

    /// <summary>Una entrada del catálogo. Solo lectura: el catálogo se define en datos, no en código.</summary>
    public interface IItemDefinition
    {
        string CatalogId { get; }
        string DisplayName { get; }
        string Description { get; }
        ItemCategory Category { get; }
        int Price { get; }
        int UnlockLevel { get; }

        /// <summary>Casillas que ocupa si es mueble. (1,1) para todo lo demás.</summary>
        int FootprintX { get; }
        int FootprintY { get; }
    }

    /// <summary>Monedas, tiendas, catálogo e inventario del jugador.</summary>
    public interface IEconomyService
    {
        Wallet Wallet { get; }
        Inventory Inventory { get; }

        IItemDefinition GetItem(string catalogId);
        IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category);

        /// <summary>Suma o resta monedas. <paramref name="reason"/> sale en el registro y en la interfaz.</summary>
        void AddCoins(long amount, string reason);

        bool TrySpend(long amount, string reason);

        /// <summary>Compra y mete en el inventario. Falla si no llega el dinero o no está desbloqueado.</summary>
        bool TryBuy(string catalogId, int quantity = 1);

        /// <summary>Lo que hay hoy en esa tienda. Rota cada día.</summary>
        IReadOnlyList<string> StockOf(string shopId);

        /// <summary>Da un objeto a un habitante. Gasta el objeto y mueve su ánimo según sus gustos.</summary>
        bool GiveTo(string islanderId, string catalogId);
    }
}
