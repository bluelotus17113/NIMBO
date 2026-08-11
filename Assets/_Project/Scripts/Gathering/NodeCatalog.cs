using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Gathering
{
    /// <summary>
    /// Catálogo de nodos de recurso, cargado una vez desde
    /// <c>Resources/Config/catalogo_recursos.json</c>. Solo lectura después del constructor.
    /// </summary>
    public class NodeCatalog
    {
        readonly Dictionary<string, NodeDefinition> _byId =
            new Dictionary<string, NodeDefinition>();

        public IReadOnlyList<NodeDefinition> All { get; }

        /// <summary>Constructor de producción: carga el JSON desde Resources.</summary>
        public NodeCatalog()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_recursos");
            if (asset == null)
            {
                Debug.LogError("NodeCatalog: no se encontró Resources/Config/catalogo_recursos.json");
                All = Array.Empty<NodeDefinition>();
                return;
            }
            LoadJson(asset.text);
            All = _byId.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Constructor de test: recibe el JSON en crudo para no depender de
        /// <c>Resources.Load</c>.
        /// </summary>
        public NodeCatalog(string json)
        {
            LoadJson(json);
            All = _byId.Values.ToList().AsReadOnly();
        }

        void LoadJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            NodeCatalogRoot root;
            try { root = JsonConvert.DeserializeObject<NodeCatalogRoot>(json); }
            catch (Exception e)
            {
                Debug.LogError($"NodeCatalog: error al parsear el JSON — {e.Message}");
                return;
            }

            if (root?.items == null) return;

            foreach (var item in root.items)
            {
                if (string.IsNullOrEmpty(item.nodeId))
                {
                    Debug.LogError("NodeCatalog: item sin nodeId — se ignora");
                    continue;
                }

                if (!Enum.TryParse<NodeKind>(item.kind, out var kind))
                {
                    Debug.LogError(
                        $"NodeCatalog: kind '{item.kind}' no reconocido en '{item.nodeId}' — se ignora");
                    continue;
                }

                if (!Enum.TryParse<ToolKind>(item.requiredTool, out var tool))
                {
                    Debug.LogError(
                        $"NodeCatalog: requiredTool '{item.requiredTool}' no reconocido en '{item.nodeId}' — se ignora");
                    continue;
                }

                if (_byId.ContainsKey(item.nodeId))
                {
                    Debug.LogError(
                        $"NodeCatalog: nodeId duplicado '{item.nodeId}' — se ignora");
                    continue;
                }

                var def = new NodeDefinition(
                    item.nodeId,
                    item.displayName ?? "",
                    kind,
                    tool,
                    item.dropId ?? "",
                    item.minDrop,
                    item.maxDrop,
                    item.hits,
                    item.respawnDays);

                _byId[item.nodeId] = def;
            }
        }

        /// <summary>
        /// Busca un nodo por su identificador de catálogo. Devuelve false si no existe,
        /// sin loguear error — el servicio ya decide si es <c>UnknownNode</c>.
        /// </summary>
        public bool TryGetDefinition(string nodeId, out NodeDefinition definition)
        {
            return _byId.TryGetValue(nodeId, out definition);
        }

        /// <summary>Para tests: cuántos nodos hay en el catálogo.</summary>
        public int Count => _byId.Count;

        // ── helpers de serialización ────────────────────────────────────────

        [Serializable]
        class NodeCatalogRoot
        {
            public int version;
            public List<NodeCatalogItem> items;
        }

        [Serializable]
        class NodeCatalogItem
        {
            public string nodeId;
            public string displayName;
            public string description;
            public string kind;
            public string requiredTool;
            public string dropId;
            public int minDrop;
            public int maxDrop;
            public int hits;
            public int respawnDays;
        }
    }
}
