using System.Collections.Generic;
using Nimbo.Data.Housing;

namespace Nimbo.Housing.Catalog
{
    /// <summary>
    /// Una entrada del catálogo de muebles, solo lectura. Los números vienen del JSON
    /// y no se alteran en runtime.
    /// </summary>
    public class FurnitureEntry
    {
        public readonly string CatalogId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Price;
        public readonly int UnlockLevel;
        public readonly List<string> Tags;
        public readonly PlacementLayer Layer;
        public readonly int FootprintX;
        public readonly int FootprintY;
        public readonly string Function;
        public readonly Dictionary<string, float> NeedBonus;

        public FurnitureEntry(
            string catalogId,
            string displayName,
            string description,
            int price,
            int unlockLevel,
            List<string> tags,
            PlacementLayer layer,
            int footprintX,
            int footprintY,
            string function,
            Dictionary<string, float> needBonus)
        {
            CatalogId = catalogId;
            DisplayName = displayName;
            Description = description;
            Price = price;
            UnlockLevel = unlockLevel;
            Tags = tags ?? new List<string>();
            Layer = layer;
            FootprintX = footprintX;
            FootprintY = footprintY;
            Function = function;
            NeedBonus = needBonus ?? new Dictionary<string, float>();
        }

        public float BonusFor(string needKey)
        {
            return NeedBonus.TryGetValue(needKey, out var v) ? v : 0f;
        }

        public float TotalNeedBonus()
        {
            float sum = 0f;
            foreach (var kv in NeedBonus) sum += kv.Value;
            return sum;
        }
    }
}
