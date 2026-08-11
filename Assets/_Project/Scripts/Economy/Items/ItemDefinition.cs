using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;

namespace Nimbo.Economy.Items
{
    /// <summary>
    /// Una entrada del catálogo, inmutable tras construirse. Implementa
    /// <see cref="IItemDefinition"/> y expone los campos específicos de cada familia
    /// como propiedades extra que el resto del módulo de economía sí puede leer.
    /// </summary>
    public class ItemDefinition : IItemDefinition
    {
        public string CatalogId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public ItemCategory Category { get; }

        /// <summary>Qué herramienta es. Lo dice el catálogo, no el nombre del id.</summary>
        public ToolKind Tool { get; }
        public int Price { get; }
        public int UnlockLevel { get; }

        /// <summary>Casillas horizontales. Vale 1 salvo en muebles.</summary>
        public int FootprintX { get; }

        /// <summary>Casillas verticales. Vale 1 salvo en muebles.</summary>
        public int FootprintY { get; }

        // ── comida ──
        public int HungerRestore { get; }
        public string FoodKind { get; }

        // ── ropa ──
        public string Slot { get; }
        public string Style { get; }
        public string[] Palette { get; }

        // ── muebles ──
        public string Layer { get; }
        public string Function { get; }
        public Dictionary<string, int> NeedBonus { get; }

        // ── acabados ──
        public string Surface { get; }
        public string BaseColor { get; }
        public string Pattern { get; }

        public ItemDefinition(CatalogItemJson json, ItemCategory category)
        {
            Tool = System.Enum.TryParse<ToolKind>(json.toolKind, ignoreCase: true, out var tool)
                ? tool : ToolKind.None;
            CatalogId = json.catalogId;
            DisplayName = json.displayName;
            Description = json.description;
            Category = category;
            Price = json.price;
            UnlockLevel = json.unlockLevel;
            FootprintX = category == ItemCategory.Furniture ? json.footprintX : 1;
            FootprintY = category == ItemCategory.Furniture ? json.footprintY : 1;

            HungerRestore = json.hungerRestore;
            FoodKind = json.foodKind;
            Slot = json.slot;
            Style = json.style;
            Palette = json.palette?.ToArray();
            Layer = json.layer;
            Function = json.function;
            NeedBonus = json.needBonus;
            Surface = json.surface;
            BaseColor = json.baseColor;
            Pattern = json.pattern;
        }

        public override string ToString() => $"[{Category}] {CatalogId} ({Price}⭐)";
    }
}
