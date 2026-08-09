using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Simulation.Progression
{
    /// <summary>Raíz del JSON de logros, con versión y lista.</summary>
    [Serializable]
    internal class AchievementCatalogRootJson
    {
        public int version;
        public List<AchievementItemJson> items;
    }

    /// <summary>Un logro tal cual viene en el JSON.</summary>
    [Serializable]
    internal class AchievementItemJson
    {
        public string achievementId;
        public string displayName;
        public string description;
        public string kind;
        public int goal;
        public long reward;
        public bool hidden;
    }

    /// <summary>
    /// El catálogo de logros, cargado una vez desde
    /// <c>Resources/Config/catalogo_logros.json</c>. Solo lectura después del
    /// constructor.
    /// </summary>
    /// <remarks>
    /// Tiene dos constructores a propósito: el de producción carga Resources y el
    /// de pruebas recibe el JSON en crudo para no depender de Unity.
    /// Si el JSON no carga, catálogo vacío y <c>Debug.LogError</c>: una lista de
    /// logros rota no puede impedir que arranque la partida.
    /// </remarks>
    public class AchievementCatalog
    {
        readonly Dictionary<string, AchievementDefinition> _byId =
            new Dictionary<string, AchievementDefinition>();

        public IReadOnlyList<AchievementDefinition> Catalog { get; }

        /// <summary>Constructor de producción: carga desde Resources.</summary>
        public AchievementCatalog()
        {
            var asset = Resources.Load<TextAsset>("Config/catalogo_logros");
            if (asset != null) LoadJson(asset.text);
            else Debug.LogError("AchievementCatalog: no se encontró Config/catalogo_logros.json");
            Catalog = _byId.Values.ToList().AsReadOnly();
        }

        /// <summary>Constructor de test: recibe el JSON en crudo.</summary>
        public AchievementCatalog(string json)
        {
            if (!string.IsNullOrEmpty(json)) LoadJson(json);
            Catalog = _byId.Values.ToList().AsReadOnly();
        }

        void LoadJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            var root = JsonConvert.DeserializeObject<AchievementCatalogRootJson>(json);
            if (root?.items == null)
            {
                Debug.LogError("AchievementCatalog: JSON inválido o sin items");
                return;
            }

            foreach (var item in root.items)
            {
                if (string.IsNullOrEmpty(item.achievementId))
                {
                    Debug.LogError("AchievementCatalog: logro sin achievementId — se ignora");
                    continue;
                }

                if (_byId.ContainsKey(item.achievementId))
                {
                    Debug.LogError(
                        $"AchievementCatalog: achievementId duplicado '{item.achievementId}' — se ignora");
                    continue;
                }

                var kind = ParseKind(item.kind);
                var def = new AchievementDefinition(
                    item.achievementId, item.displayName ?? "",
                    item.description ?? "", kind, item.goal, item.reward, item.hidden);

                _byId[def.AchievementId] = def;
            }
        }

        static AchievementKind ParseKind(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return AchievementKind.Life;
            return kind switch
            {
                "Life" => AchievementKind.Life,
                "Social" => AchievementKind.Social,
                "Home" => AchievementKind.Home,
                "Money" => AchievementKind.Money,
                "Play" => AchievementKind.Play,
                "Island" => AchievementKind.Island,
                "Odd" => AchievementKind.Odd,
                _ => AchievementKind.Life,
            };
        }

        public bool TryGetDefinition(string achievementId,
                                     out AchievementDefinition definition)
        {
            if (!string.IsNullOrEmpty(achievementId))
                return _byId.TryGetValue(achievementId, out definition);
            definition = default;
            return false;
        }

        /// <summary>Para tests: saber cuántos logros hay sin pasar por Catalog.</summary>
        public int Count => _byId.Count;
    }
}
