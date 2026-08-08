using System;
using System.Collections.Generic;
using Nimbo.Core.Util;
using Nimbo.Economy.Items;

namespace Nimbo.Economy.Shops
{
    /// <summary>
    /// Genera el stock diario de una tienda con azar determinista: mismo día → mismos
    /// objetos. Usa <c>Rng.FromSeed</c> para que no haga falta guardarlo en la partida.
    /// </summary>
    public static class ShopStock
    {
        /// <summary>
        /// Devuelve los <c>catalogId</c> que esa tienda vende hoy. El resultado es
        /// estable: dos llamadas con el mismo día devuelven lo mismo.
        /// </summary>
        public static IReadOnlyList<string> Generate(ShopDefinition shop, int day, ItemCatalog catalog)
        {
            if (shop == null || catalog == null) return Array.Empty<string>();

            // Agrupar todos los items de las categorías que la tienda maneja.
            var pool = new List<string>();
            for (int i = 0; i < shop.Categories.Count; i++)
            {
                foreach (var item in catalog.ItemsOfCategory(shop.Categories[i]))
                    pool.Add(item.CatalogId);
            }

            if (pool.Count == 0) return Array.Empty<string>();

            // Azar con semilla: mismo shop + mismo día → misma secuencia.
            var rng = Rng.FromSeed($"{shop.ShopId}:{day}");
            rng.Shuffle(pool);

            int count = Math.Min(shop.StockSlots, pool.Count);
            return pool.GetRange(0, count).AsReadOnly();
        }
    }
}
