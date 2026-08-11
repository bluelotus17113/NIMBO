using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Crafting
{
    /// <summary>
    /// Catálogo de recetas de crafteo, cargado una vez desde
    /// <c>Resources/Config/catalogo_recetas.json</c>. Solo lectura tras el constructor.
    /// </summary>
    public class RecipeCatalog
    {
        readonly Dictionary<string, Recipe> _byId = new Dictionary<string, Recipe>();
        readonly List<Recipe> _all = new List<Recipe>();

        public IReadOnlyList<Recipe> All { get; }

        /// <summary>Constructor de producción: carga desde Resources.</summary>
        public RecipeCatalog()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_recetas");
            if (asset != null)
            {
                LoadJson(asset.text);
            }
            else
            {
                Debug.LogError("RecipeCatalog: no se encontró Resources/Config/catalogo_recetas.json");
            }
            All = new ReadOnlyCollection<Recipe>(_all);
        }

        /// <summary>Constructor de test: recibe el JSON en crudo, sin pasar por Resources.</summary>
        public RecipeCatalog(string rawJson)
        {
            if (!string.IsNullOrEmpty(rawJson))
            {
                LoadJson(rawJson);
            }
            All = new ReadOnlyCollection<Recipe>(_all);
        }

        void LoadJson(string json)
        {
            // JSON roto no debe tirar excepción: catálogo vacío y error en consola
            CatalogRootJson root;
            try
            {
                root = JsonConvert.DeserializeObject<CatalogRootJson>(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"RecipeCatalog: JSON mal formado — {ex.Message}");
                return;
            }

            if (root?.items == null) return;

            foreach (var item in root.items)
            {
                if (string.IsNullOrEmpty(item.recipeId))
                {
                    Debug.LogError("RecipeCatalog: receta sin recipeId — se ignora");
                    continue;
                }

                if (_byId.ContainsKey(item.recipeId))
                {
                    Debug.LogError($"RecipeCatalog: recipeId duplicado '{item.recipeId}' — se ignora");
                    continue;
                }

                var ingredients = new List<CraftIngredient>(item.ingredients?.Length ?? 0);
                if (item.ingredients != null)
                {
                    foreach (var ing in item.ingredients)
                    {
                        ingredients.Add(new CraftIngredient(ing.catalogId, ing.quantity));
                    }
                }

                var recipe = new Recipe(
                    item.recipeId,
                    item.displayName ?? "",
                    item.description ?? "",
                    ParseStation(item.station),
                    ingredients.AsReadOnly(),
                    item.outputId ?? "",
                    item.outputQuantity,
                    item.unlockLevel
                );

                _byId[item.recipeId] = recipe;
                _all.Add(recipe);
            }
        }

        static CraftStation ParseStation(string station)
        {
            return station switch
            {
                "Hand" => CraftStation.Hand,
                "Bench" => CraftStation.Bench,
                "Kitchen" => CraftStation.Kitchen,
                _ => CraftStation.Hand,
            };
        }

        /// <summary>Devuelve false si la receta no existe.</summary>
        public bool TryGet(string recipeId, out Recipe recipe)
        {
            return _byId.TryGetValue(recipeId, out recipe);
        }

        // ── tipos internos para deserializar JSON ──────────────────────────

        class CatalogRootJson
        {
            public int version;
            public RecipeItemJson[] items;
        }

        class RecipeItemJson
        {
            public string recipeId;
            public string displayName;
            public string description;
            public string station;
            public CraftIngredientJson[] ingredients;
            public string outputId;
            public int outputQuantity = 1;
            public int unlockLevel = 1;
        }

        class CraftIngredientJson
        {
            public string catalogId;
            public int quantity = 1;
        }
    }
}
