using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Farming
{
    /// <summary>
    /// Carga los cultivos desde <c>Resources/Config/catalogo_cultivos.json</c>.
    /// Solo lectura tras el constructor. Copia el patrón de <see cref="Nimbo.Economy.Items.ItemCatalog"/>:
    /// constructor de producción por Resources y constructor de test por JSON crudo.
    /// </summary>
    public class CropCatalog
    {
        readonly Dictionary<string, CropDefinition> _bySeedId = new Dictionary<string, CropDefinition>();

        /// <summary>Todos los cultivos, en el orden del JSON.</summary>
        public IReadOnlyList<CropDefinition> Crops { get; }

        /// <summary>Constructor de producción: carga desde Resources.</summary>
        public CropCatalog()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_cultivos");
            if (asset != null)
            {
                LoadJson(asset.text);
            }
            else
            {
                Debug.LogError("CropCatalog: no se encontró Resources/Config/catalogo_cultivos.json");
            }
            Crops = _bySeedId.Values.ToList().AsReadOnly();
        }

        /// <summary>Constructor de test: recibe el JSON en crudo para no depender de Resources.</summary>
        public CropCatalog(string rawJson)
        {
            LoadJson(rawJson);
            Crops = _bySeedId.Values.ToList().AsReadOnly();
        }

        void LoadJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            CatalogRootJson root;
            try
            {
                root = JsonConvert.DeserializeObject<CatalogRootJson>(json);
            }
            catch (JsonException e)
            {
                Debug.LogError($"CropCatalog: JSON roto — {e.Message}");
                return;
            }

            if (root?.items == null) return;

            foreach (var item in root.items)
            {
                if (string.IsNullOrEmpty(item.seedId) || string.IsNullOrEmpty(item.cropId))
                {
                    Debug.LogError("CropCatalog: cultivo sin seedId o cropId — se ignora");
                    continue;
                }

                if (_bySeedId.ContainsKey(item.seedId))
                {
                    Debug.LogError($"CropCatalog: seedId duplicado '{item.seedId}' — se ignora");
                    continue;
                }

                var def = new CropDefinition(
                    item.seedId,
                    item.cropId,
                    item.displayName ?? item.seedId,
                    item.daysToGrow,
                    item.yield,
                    item.regrows
                );

                _bySeedId[item.seedId] = def;
            }
        }

        /// <summary>Busca un cultivo por su seedId. Devuelve false si no existe.</summary>
        public bool TryGetCrop(string seedId, out CropDefinition crop)
        {
            return _bySeedId.TryGetValue(seedId, out crop);
        }

        /// <summary>Para tests: cuántos cultivos hay sin pasar por Crops.</summary>
        public int Count => _bySeedId.Count;

        // ── JSON intermedio, igual que CatalogJson en Items ──────────────────

        [System.Serializable]
        class CatalogRootJson
        {
            public int version;
            public List<CropItemJson> items;
        }

        [System.Serializable]
        class CropItemJson
        {
            public string seedId;
            public string cropId;
            public string displayName;
            public string description;
            public int daysToGrow;
            public int yield;
            public bool regrows;
        }
    }
}
