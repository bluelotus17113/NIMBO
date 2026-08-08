using System.Collections.Generic;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Para qué sirve una zona. La IA del habitante elige destino por esto.</summary>
    public enum ZonePurpose
    {
        Home = 0,
        Food = 1,
        Shopping = 2,
        Leisure = 3,
        Nature = 4,
        Social = 5,
        Civic = 6,
    }

    /// <summary>La isla flotante: sus zonas, sus edificios y por dónde se anda.</summary>
    public interface IIslandService
    {
        IslandState State { get; }

        IReadOnlyList<string> ZoneIds { get; }

        ZonePurpose PurposeOf(string zoneId);

        /// <summary>Un punto donde plantar a un habitante dentro de esa zona.</summary>
        bool TryGetSpawnPoint(string zoneId, out Vector3 position);

        /// <summary>Zonas alcanzables desde esa. La isla es pequeña: casi todas lo son.</summary>
        IEnumerable<string> ReachableFrom(string zoneId);

        bool IsUnlocked(string buildingId);

        /// <summary>Abre un edificio. Publica <c>BuildingUnlocked</c> y no cobra nada: eso es de economía.</summary>
        bool Unlock(string buildingId);

        /// <summary>Manda a un habitante a una zona. La navegación es del módulo de isla.</summary>
        void SendTo(string islanderId, string zoneId);
    }
}
