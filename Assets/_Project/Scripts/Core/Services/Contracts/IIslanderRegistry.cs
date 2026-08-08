using System.Collections.Generic;
using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Quién vive en la isla. Es la única puerta a los datos de un habitante: ningún
    /// módulo guarda su propia lista ni se pasa <see cref="IslanderData"/> por detrás.
    /// </summary>
    public interface IIslanderRegistry
    {
        IReadOnlyList<IslanderData> All { get; }
        int Count { get; }

        IslanderData Get(string islanderId);
        bool TryGet(string islanderId, out IslanderData islander);
        bool Exists(string islanderId);

        /// <summary>Los que están ahora mismo en esa zona de la isla.</summary>
        IEnumerable<IslanderData> InZone(string zoneId);

        void Add(IslanderData islander);
        void Remove(string islanderId);
    }
}
