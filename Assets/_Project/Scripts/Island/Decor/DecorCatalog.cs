using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Island.Decor
{
    /// <summary>
    /// El catálogo de adornos de la isla, cargado una vez desde
    /// <c>Resources/Config/catalogo_decoracion.json</c>. Solo lectura después del
    /// constructor.
    /// </summary>
    /// <remarks>
    /// Misma forma que <c>ItemCatalog</c>: diccionario interno, dos constructores
    /// (producción y test), y sin excepciones aunque el JSON esté roto.
    /// </remarks>
    public class DecorCatalog
    {
        readonly Dictionary<string, DecorDefinition> _byId =
            new Dictionary<string, DecorDefinition>();

        public IReadOnlyList<DecorDefinition> All { get; }

        /// <summary>Constructor de producción: carga el JSON desde Resources.</summary>
        public DecorCatalog()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_decoracion");
            if (asset == null)
            {
                Debug.LogError("DecorCatalog: no se encontró Resources/Config/catalogo_decoracion.json");
                All = Array.Empty<DecorDefinition>();
                return;
            }
            LoadJson(asset.text);
            All = _byId.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Constructor de test: recibe el JSON en crudo para no depender de
        /// <c>Resources.Load</c>.
        /// </summary>
        public DecorCatalog(string json)
        {
            LoadJson(json);
            All = _byId.Values.ToList().AsReadOnly();
        }

        void LoadJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            DecorCatalogRoot root;
            try { root = JsonConvert.DeserializeObject<DecorCatalogRoot>(json); }
            catch (Exception e)
            {
                Debug.LogError($"DecorCatalog: error al parsear el JSON — {e.Message}");
                return;
            }

            if (root?.items == null) return;

            foreach (var item in root.items)
            {
                if (string.IsNullOrEmpty(item.catalogId))
                {
                    Debug.LogError("DecorCatalog: item sin catalogId — se ignora");
                    continue;
                }

                if (!Enum.TryParse<DecorKind>(item.kind, out var kind))
                {
                    Debug.LogError(
                        $"DecorCatalog: kind '{item.kind}' no reconocido en '{item.catalogId}' — se ignora");
                    continue;
                }

                if (_byId.ContainsKey(item.catalogId))
                {
                    Debug.LogError(
                        $"DecorCatalog: catalogId duplicado '{item.catalogId}' — se ignora");
                    continue;
                }

                var def = new DecorDefinition(
                    item.catalogId,
                    item.displayName ?? "",
                    item.description ?? "",
                    kind,
                    item.price,
                    item.unlockLevel,
                    item.footprint,
                    item.charm);

                _byId[item.catalogId] = def;
            }
        }

        /// <summary>
        /// Busca un adorno por su identificador de catálogo. Devuelve false si no
        /// existe, sin loguear error — el servicio ya decide si es <c>UnknownItem</c>.
        /// </summary>
        public bool TryGetDefinition(string catalogId, out DecorDefinition definition)
        {
            return _byId.TryGetValue(catalogId, out definition);
        }

        /// <summary>Para tests: cuántos adornos hay en el catálogo.</summary>
        public int Count => _byId.Count;

        // ── helpers de serialización ────────────────────────────────────────

        [Serializable]
        class DecorCatalogRoot
        {
            public int version;
            public List<DecorCatalogItem> items;
        }

        [Serializable]
        class DecorCatalogItem
        {
            public string catalogId;
            public string displayName;
            public string description;
            public string kind;
            public int price;
            public int unlockLevel;
            public float footprint;
            public float charm;
        }
    }
}
