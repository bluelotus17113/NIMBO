using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

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
        //
        // Los identificadores de las tres primeras son los mismos que declara el
        // plano de la isla (`IslandLayout.FirstIsland`, campo `ShopId`) y los que
        // pasa la interfaz al abrir la tienda. Estuvo semanas roto por hablar cada
        // capa un idioma distinto: la interfaz pedía «tienda_comida», aquí solo se
        // conocía «NimboMart», `StockOf` devolvía vacío sin error ni aviso, y nadie
        // podía comprar nada en ninguna partida. El idioma lo pone el mundo —la zona
        // es lo que existe primero— y el catálogo lo habla; el nombre bonito va en
        // `DisplayName` y no en el id, igual que `zona_*` y `seed_*` en el resto del
        // proyecto.

        /// <remarks>
        /// Vende también semillas, y esto no es un adorno: **no había forma de comprar
        /// una**. El protagonista arrancaba con ocho de un solo cultivo y, cuando se le
        /// acababan, el huerto se quedaba para siempre en tierra labrada. Media capa de
        /// granja dependía de un regalo de bienvenida.
        ///
        /// En la tienda de comida y no en una propia porque las semillas son comida que
        /// aún no ha crecido, y porque una zona nueva pediría vecinos y desbloqueos que
        /// dejarían el huerto parado igual pero más tarde.
        /// </remarks>
        public static readonly ShopDefinition NimboMart = new ShopDefinition(
            "tienda_comida", "NimboMart", 12,
            ItemCategory.Food, ItemCategory.Consumable, ItemCategory.Gift, ItemCategory.Seed);

        public static readonly ShopDefinition MueblesNimbo = new ShopDefinition(
            "tienda_muebles", "Muebles Nimbo", 8,
            ItemCategory.Furniture);

        public static readonly ShopDefinition BoutiqueCeleste = new ShopDefinition(
            "tienda_ropa", "Boutique Celeste", 10,
            ItemCategory.Clothing);

        // Sin zona todavía: abren en fases 3 y 4 (Docs/Contratos/progresion.md) y no
        // hay sitio donde caer en la primera isla. Cuando la tengan, su id tendrá que
        // ser el que esa zona declare, como las tres de arriba.
        public static readonly ShopDefinition AntiguedadesNimbo = new ShopDefinition(
            "Antigüedades Nimbo", "Antigüedades Nimbo", 4,
            ItemCategory.Decoration, ItemCategory.Wallpaper, ItemCategory.Flooring);

        public static readonly ShopDefinition MercadoFlotante = new ShopDefinition(
            "Mercado flotante", "Mercado flotante", 6,
            ItemCategory.Food, ItemCategory.Clothing, ItemCategory.Furniture,
            ItemCategory.Decoration, ItemCategory.Gift, ItemCategory.Consumable,
            ItemCategory.Wallpaper, ItemCategory.Flooring, ItemCategory.Seed);

        public static readonly IReadOnlyList<ShopDefinition> All = new List<ShopDefinition>
        {
            NimboMart, MueblesNimbo, BoutiqueCeleste, AntiguedadesNimbo, MercadoFlotante,
        };

        /// <summary>
        /// Devuelve null y registra error si el id no existe.
        /// </summary>
        /// <remarks>
        /// Antes devolvía null en silencio, y ese silencio es exactamente por lo que
        /// la costura rota con la interfaz pasó semanas sin doler: una tienda pedida
        /// con un id desconocido se veía igual que una tienda sin surtido. Un id
        /// inventado es un fallo de datos, no un caso normal — misma respuesta que
        /// <c>ItemCatalog.GetItem</c>—. Se devuelve null igualmente para que el juego
        /// siga en pie mientras el error queda visto en consola.
        /// </remarks>
        public static ShopDefinition Get(string shopId)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].ShopId == shopId) return All[i];

            Debug.LogError($"ShopDefinition: '{shopId}' no es ninguna tienda conocida");
            return null;
        }
    }
}
