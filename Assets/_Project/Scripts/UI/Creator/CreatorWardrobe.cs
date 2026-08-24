using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.UI.Creator
{
    /// <summary>Una prenda que el creador puede ofrecer, con lo mínimo para elegirla y verla.</summary>
    public sealed class GarmentOption
    {
        public string CatalogId;
        public string DisplayName;
        public string Description;
        public string Slot;
        public string[] Palette;
        public int UnlockLevel;
        public int Price;

        /// <summary>El color principal de la prenda, o un neutro si el catálogo no trae paleta.</summary>
        public Color MainColor => ColourAt(0);

        /// <summary>El color secundario. Si no hay, el principal — igual que hace el arte.</summary>
        public Color SecondColor
        {
            get
            {
                var second = ColourAt(1);
                return second == MainColor ? MainColor : second;
            }
        }

        private Color ColourAt(int index)
        {
            if (Palette != null && Palette.Length > index &&
                ColorUtility.TryParseHtmlString(Palette[index], out var parsed))
                return parsed;

            // Mismo recurso que OutfitLook.ColourOf: mismo id, mismo color en
            // cualquier isla. Aquí casi nunca se llega (el catálogo de ropa trae
            // paleta completa), pero una prenda sin color no puede pintar nada.
            uint hash = 2166136261u;
            string id = CatalogId ?? "";
            for (int i = 0; i < id.Length; i++)
            {
                hash ^= id[i];
                hash *= 16777619u;
            }
            return Color.HSVToRGB((hash & 0xFF) / 255f, 0.45f, 0.85f);
        }
    }

    /// <summary>
    /// De dónde saca el creador las prendas del primer nivel.
    /// </summary>
    /// <remarks>
    /// Hay dos caminos y no por capricho: con partida cargada el catálogo vive en
    /// <c>IEconomyService</c>; pero el creador es también la primera pantalla de una
    /// partida nueva, y los servicios se registran al terminar de crear al
    /// protagonista (<c>GameBootstrap.StartGame</c>), o sea después de esta pantalla.
    /// Sin el camino alternativo, el jugador estrenaría el juego sin poder vestir a
    /// su personaje — exactamente el tipo de agujero silencioso que este proyecto
    /// caza a golpes.
    ///
    /// El filtro es <c>UnlockLevel &lt;= 1</c> y no «las cuatro primeras»: el nivel
    /// de desbloqueo es dato del catálogo y la progresión manda. Hoy salen cuatro
    /// prendas; si mañana el catálogo estrena otra de nivel 1, aquí aparece sola.
    /// </remarks>
    public static class CreatorWardrobe
    {
        private const string RopaResource = "Config/catalogo_ropa";

        /// <summary>Las prendas que alguien de nivel 1 ya puede llevar puestas.</summary>
        public static List<GarmentOption> UnlockedAtStart()
        {
            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
                return FromCatalog(economy.ItemsOfCategory(ItemCategory.Clothing));

            return FromResources();
        }

        private static List<GarmentOption> FromCatalog(
            IEnumerable<IItemDefinition> items)
        {
            var options = new List<GarmentOption>();

            foreach (var item in items)
            {
                if (item.UnlockLevel > 1) continue;
                options.Add(new GarmentOption
                {
                    CatalogId = item.CatalogId,
                    DisplayName = item.DisplayName,
                    Description = item.Description,
                    Slot = item.Slot,
                    Palette = item.Palette,
                    UnlockLevel = item.UnlockLevel,
                    Price = item.Price,
                });
            }
            return options;
        }

        /// <summary>
        /// El mismo catálogo, leído de Resources cuando aún no hay economía.
        /// </summary>
        /// <remarks>
        /// El esquema canónico vive en <c>Nimbo.Economy.Items.CatalogJson</c>, que
        /// este ensamblado no ve. Aquí solo se repiten los seis campos que la
        /// elección necesita; si el catálogo cambia de forma, este lector se rompe
        /// en el arranque del creador y no en silencio: JsonUtility deja a null la
        /// lista y la tarjeta enseña «sin prendas», que se ve enseguida.
        /// </remarks>
        private static List<GarmentOption> FromResources()
        {
            var options = new List<GarmentOption>();

            var asset = Resources.Load<TextAsset>(RopaResource);
            if (asset == null) return options;

            var root = JsonUtility.FromJson<CatalogoJson>(asset.text);
            if (root?.items == null) return options;

            foreach (var item in root.items)
            {
                if (item.unlockLevel > 1) continue;
                options.Add(new GarmentOption
                {
                    CatalogId = item.catalogId,
                    DisplayName = item.displayName,
                    Description = item.description,
                    Slot = item.slot,
                    Palette = item.palette?.ToArray(),
                    UnlockLevel = item.unlockLevel,
                    Price = item.price,
                });
            }
            return options;
        }

        [System.Serializable]
        private class CatalogoJson
        {
            public List<PrendaJson> items;
        }

        [System.Serializable]
        private class PrendaJson
        {
            public string catalogId;
            public string displayName;
            public string description;
            public int price;
            public int unlockLevel;
            public string slot;
            public List<string> palette;
        }
    }
}
