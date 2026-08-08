using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Core.Services
{
    /// <summary>
    /// La lista de quién vive en la isla, con un índice por id.
    /// </summary>
    /// <remarks>
    /// Vive en Core y no en un módulo de juego porque la necesitan los nueve: si la
    /// tuviera la simulación, todos los demás dependerían de la simulación para leer
    /// un nombre. Aquí no tiene ninguna lógica de juego, solo búsqueda.
    /// </remarks>
    public sealed class IslanderRegistry : IIslanderRegistry
    {
        private readonly List<IslanderData> _all = new List<IslanderData>();
        private readonly Dictionary<string, IslanderData> _byId = new Dictionary<string, IslanderData>();

        public IslanderRegistry() { }

        public IslanderRegistry(IEnumerable<IslanderData> islanders)
        {
            foreach (var islander in islanders) Add(islander);
        }

        public IReadOnlyList<IslanderData> All => _all;
        public int Count => _all.Count;

        public IslanderData Get(string islanderId) =>
            islanderId != null && _byId.TryGetValue(islanderId, out var islander) ? islander : null;

        public bool TryGet(string islanderId, out IslanderData islander)
        {
            if (islanderId == null) { islander = null; return false; }
            return _byId.TryGetValue(islanderId, out islander);
        }

        public bool Exists(string islanderId) => islanderId != null && _byId.ContainsKey(islanderId);

        public IEnumerable<IslanderData> InZone(string zoneId)
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].CurrentZoneId == zoneId) yield return _all[i];
        }

        public void Add(IslanderData islander)
        {
            if (islander == null || string.IsNullOrEmpty(islander.Id))
            {
                Debug.LogError("IslanderRegistry: habitante sin id, no se añade");
                return;
            }
            if (_byId.ContainsKey(islander.Id))
            {
                Debug.LogWarning($"IslanderRegistry: {islander.Id} ya estaba, se ignora");
                return;
            }

            _all.Add(islander);
            _byId[islander.Id] = islander;
            islander.Relationships?.RebuildIndex();
        }

        public void Remove(string islanderId)
        {
            if (!_byId.TryGetValue(islanderId, out var islander)) return;
            _all.Remove(islander);
            _byId.Remove(islanderId);
        }

        public void Clear()
        {
            _all.Clear();
            _byId.Clear();
        }
    }
}
