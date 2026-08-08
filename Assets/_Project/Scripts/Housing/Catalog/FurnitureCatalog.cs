using System.Collections.Generic;
using Nimbo.Data.Housing;
using UnityEngine;

namespace Nimbo.Housing.Catalog
{
    /// <summary>
    /// Carga <c>catalogo_muebles.json</c> y <c>catalogo_acabados.json</c> desde
    /// <c>Resources/Config/</c> y los indexa por <c>catalogId</c> para consulta
    /// rápida durante la edición.
    /// </summary>
    public class FurnitureCatalog
    {
        readonly Dictionary<string, FurnitureEntry> _furniture = new Dictionary<string, FurnitureEntry>();
        readonly Dictionary<string, FinishEntry> _finishes = new Dictionary<string, FinishEntry>();

        /// <summary>Carga los dos JSON desde Resources/Config.</summary>
        public FurnitureCatalog()
        {
            LoadFurniture();
            LoadFinishes();
        }

        /// <summary>Constructor para tests: recibe el JSON en crudo y no toca Resources.</summary>
        public FurnitureCatalog(string furnitureJson, string finishesJson)
        {
            ParseFurniture(furnitureJson);
            ParseFinishes(finishesJson);
        }

        // ------------------------------------------------------------------- consultas

        public bool TryGetFurniture(string catalogId, out FurnitureEntry entry)
            => _furniture.TryGetValue(catalogId, out entry);

        public bool TryGetFinish(string catalogId, out FinishEntry entry)
            => _finishes.TryGetValue(catalogId, out entry);

        /// <summary>El mueble existe en cualquiera de los dos catálogos.</summary>
        public bool Exists(string catalogId)
            => _furniture.ContainsKey(catalogId) || _finishes.ContainsKey(catalogId);

        public FurnitureEntry GetFurnitureOrThrow(string catalogId)
        {
            if (_furniture.TryGetValue(catalogId, out var e)) return e;
            throw new KeyNotFoundException($"FurnitureCatalog: '{catalogId}' no es un mueble.");
        }

        // ------------------------------------------------------------------- carga

        void LoadFurniture()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_muebles");
            if (asset == null)
            {
                Debug.LogWarning("FurnitureCatalog: no se encontró Resources/Config/catalogo_muebles");
                return;
            }
            ParseFurniture(asset.text);
        }

        void LoadFinishes()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_acabados");
            if (asset == null)
            {
                Debug.LogWarning("FurnitureCatalog: no se encontró Resources/Config/catalogo_acabados");
                return;
            }
            ParseFinishes(asset.text);
        }

        void ParseFurniture(string json)
        {
            var data = JsonUtility.FromJson<FurnitureList>(json);
            if (data?.items == null) return;

            foreach (var raw in data.items)
            {
                var entry = new FurnitureEntry(
                    raw.catalogId,
                    raw.displayName,
                    raw.description,
                    raw.price,
                    raw.unlockLevel,
                    raw.tags,
                    ParseLayer(raw.layer),
                    raw.footprintX,
                    raw.footprintY,
                    raw.function,
                    ParseNeedBonus(raw.needBonus));
                _furniture[entry.CatalogId] = entry;
            }
        }

        void ParseFinishes(string json)
        {
            var data = JsonUtility.FromJson<FinishList>(json);
            if (data?.items == null) return;

            foreach (var raw in data.items)
            {
                var entry = new FinishEntry(
                    raw.catalogId,
                    raw.displayName,
                    raw.description,
                    raw.price,
                    raw.unlockLevel,
                    raw.tags,
                    raw.surface,
                    raw.baseColor,
                    raw.pattern);
                _finishes[entry.CatalogId] = entry;
            }
        }

        static PlacementLayer ParseLayer(string s)
        {
            switch (s)
            {
                case "Floor":     return PlacementLayer.Floor;
                case "Rug":       return PlacementLayer.Rug;
                case "Furniture": return PlacementLayer.Furniture;
                case "Surface":   return PlacementLayer.Surface;
                case "Counter":   return PlacementLayer.Surface; // el JSON los llama Counter
                case "WallMounted": return PlacementLayer.WallMounted;
                case "Ceiling":   return PlacementLayer.Ceiling;
                default:
                    Debug.LogWarning($"FurnitureCatalog: layer desconocida '{s}', usando Furniture");
                    return PlacementLayer.Furniture;
            }
        }

        static Dictionary<string, float> ParseNeedBonus(NeedBonusRaw raw)
        {
            var d = new Dictionary<string, float>();
            if (raw == null) return d;
            if (raw.hunger != 0) d["hunger"] = raw.hunger;
            if (raw.energy != 0) d["energy"] = raw.energy;
            if (raw.social != 0) d["social"] = raw.social;
            if (raw.hygiene != 0) d["hygiene"] = raw.hygiene;
            // mood no es una necesidad: se ignora aunque apareciera
            return d;
        }

        // ------------------------------------------------------------------- tipos internos para JsonUtility

        [System.Serializable]
        class FurnitureList { public List<FurnitureRaw> items; }

        [System.Serializable]
        class FurnitureRaw
        {
            public string catalogId;
            public string displayName;
            public string description;
            public int price;
            public int unlockLevel;
            public List<string> tags;
            public string layer;
            public int footprintX;
            public int footprintY;
            public string function;
            public NeedBonusRaw needBonus;
        }

        [System.Serializable]
        class NeedBonusRaw
        {
            public float hunger;
            public float energy;
            public float social;
            public float hygiene;
            // mood se ignora a propósito
        }

        [System.Serializable]
        class FinishList { public List<FinishRaw> items; }

        [System.Serializable]
        class FinishRaw
        {
            public string catalogId;
            public string displayName;
            public string description;
            public int price;
            public int unlockLevel;
            public List<string> tags;
            public string surface;
            public string baseColor;
            public string pattern;
        }
    }

    /// <summary>
    /// Un acabado de pared o suelo. Misma forma que <see cref="FurnitureEntry"/>
    /// pero con atributos de superficie (<c>BaseColor</c>, <c>Pattern</c>) en vez
    /// de footprint y needBonus.
    /// </summary>
    public class FinishEntry
    {
        public readonly string CatalogId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Price;
        public readonly int UnlockLevel;
        public readonly System.Collections.Generic.List<string> Tags;
        public readonly string Surface;
        public readonly string BaseColor;
        public readonly string Pattern;

        public FinishEntry(
            string catalogId, string displayName, string description,
            int price, int unlockLevel,
            System.Collections.Generic.List<string> tags,
            string surface, string baseColor, string pattern)
        {
            CatalogId = catalogId;
            DisplayName = displayName;
            Description = description;
            Price = price;
            UnlockLevel = unlockLevel;
            Tags = tags ?? new System.Collections.Generic.List<string>();
            Surface = surface;
            BaseColor = baseColor;
            Pattern = pattern;
        }
    }
}
