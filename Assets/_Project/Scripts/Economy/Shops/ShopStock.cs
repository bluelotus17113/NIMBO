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

            var rng = Rng.FromSeed($"{shop.ShopId}:{day}");   // mismo día → mismo surtido

            // Uno de cada familia antes que nada.
            //
            // Con un sorteo plano sobre todo el montón, una familia pequeña al lado de
            // una grande no sale casi nunca: las doce semillas contra las cuarenta y
            // cinco comidas de la tienda salían dos días de cada tres sin una sola
            // semilla, y el huerto se quedaba parado esperando surtido. Una tienda que
            // declara que vende algo tiene que venderlo.
            var stock = new List<string>();
            var pool = new List<string>();

            for (int i = 0; i < shop.Categories.Count; i++)
            {
                var family = new List<string>();
                foreach (var item in catalog.ItemsOfCategory(shop.Categories[i]))
                    family.Add(item.CatalogId);

                if (family.Count == 0) continue;

                rng.Shuffle(family);

                if (stock.Count < shop.StockSlots)
                {
                    stock.Add(family[0]);
                    family.RemoveAt(0);
                }
                pool.AddRange(family);
            }

            if (stock.Count == 0 && pool.Count == 0) return Array.Empty<string>();

            // El resto de huecos, al azar entre todo lo que queda.
            rng.Shuffle(pool);
            int rest = Math.Min(shop.StockSlots - stock.Count, pool.Count);
            if (rest > 0) stock.AddRange(pool.GetRange(0, rest));

            return stock.AsReadOnly();
        }
    }
}
