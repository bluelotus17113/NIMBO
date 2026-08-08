using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Economy.Items
{
    /// <summary>
    /// El catálogo completo, cargado una vez desde <c>Resources/Config/catalogo_*.json</c>.
    /// Solo lectura después del constructor.
    /// </summary>
    public class ItemCatalog
    {
        readonly Dictionary<string, ItemDefinition> _byId = new Dictionary<string, ItemDefinition>();
        readonly Dictionary<ItemCategory, List<ItemDefinition>> _byCategory =
            new Dictionary<ItemCategory, List<ItemDefinition>>();

        public IReadOnlyList<ItemDefinition> All { get; }

        /// <summary>Constructor de producción: carga los cuatro JSON desde Resources.</summary>
        public ItemCatalog()
        {
            LoadJson(Resources.Load<TextAsset>("Config/catalogo_comida").text, "Food");
            LoadJson(Resources.Load<TextAsset>("Config/catalogo_ropa").text, "Clothing");
            LoadJson(Resources.Load<TextAsset>("Config/catalogo_muebles").text, "Furniture");
            LoadJson(Resources.Load<TextAsset>("Config/catalogo_acabados").text, "Finishes");
            All = _byId.Values.ToList().AsReadOnly();
        }

        /// <summary>Constructor de test: recibe los JSON en crudo.</summary>
        public ItemCatalog(string foodJson, string clothingJson, string furnitureJson, string finishesJson)
        {
            LoadJson(foodJson, "Food");
            LoadJson(clothingJson, "Clothing");
            LoadJson(furnitureJson, "Furniture");
            LoadJson(finishesJson, "Finishes");
            All = _byId.Values.ToList().AsReadOnly();
        }

        void LoadJson(string json, string jsonCategory)
        {
            if (string.IsNullOrEmpty(json)) return;

            var root = JsonConvert.DeserializeObject<CatalogRootJson>(json);
            if (root?.items == null) return;

            foreach (var item in root.items)
            {
                ItemCategory cat = ResolveCategory(jsonCategory, item);
                var def = new ItemDefinition(item, cat);

                if (_byId.ContainsKey(def.CatalogId))
                {
                    Debug.LogError($"ItemCatalog: catalogId duplicado '{def.CatalogId}' — se ignora");
                    continue;
                }

                _byId[def.CatalogId] = def;

                if (!_byCategory.TryGetValue(cat, out var list))
                {
                    list = new List<ItemDefinition>();
                    _byCategory[cat] = list;
                }
                list.Add(def);
            }
        }

        static ItemCategory ResolveCategory(string jsonCategory, CatalogItemJson item) => jsonCategory switch
        {
            "Food" => ItemCategory.Food,
            "Clothing" => ItemCategory.Clothing,
            "Furniture" => ItemCategory.Furniture,
            "Finishes" => item.surface == "floor" ? ItemCategory.Flooring : ItemCategory.Wallpaper,
            _ => ItemCategory.Furniture,
        };

        /// <summary>Devuelve null y registra error si el id no existe.</summary>
        public IItemDefinition GetItem(string catalogId)
        {
            if (_byId.TryGetValue(catalogId, out var def)) return def;
            Debug.LogError($"ItemCatalog: '{catalogId}' no existe en el catálogo");
            return null;
        }

        public IEnumerable<IItemDefinition> ItemsOfCategory(ItemCategory category)
        {
            if (_byCategory.TryGetValue(category, out var list)) return list;
            return Enumerable.Empty<IItemDefinition>();
        }

        /// <summary>Para tests: saber cuántos items hay sin pasar por All.</summary>
        public int Count => _byId.Count;
    }
}
