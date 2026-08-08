using System;
using System.Collections.Generic;

namespace Nimbo.Data.World
{
    public enum Weather { Clear = 0, Cloudy = 1, Rain = 2, Storm = 3, Fog = 4, Aurora = 5 }

    /// <summary>Un edificio de la isla y en qué estado de desbloqueo está.</summary>
    [Serializable]
    public struct BuildingState
    {
        public string BuildingId;
        public bool Unlocked;
        public int Tier;              // ampliaciones: 0 recién abierto
        public long UnlockedMinute;
    }

    /// <summary>
    /// El estado de la isla flotante. La v1 tiene una isla; la lista de islas existe
    /// desde ya para que añadir la segunda no obligue a tocar el guardado.
    /// </summary>
    [Serializable]
    public class IslandState
    {
        public string IslandId = "nimbo_primera";
        public string IslandName = "Isla Nimbo";

        public List<BuildingState> Buildings = new List<BuildingState>();

        public Weather Weather = Weather.Clear;
        public int PopulationCap = 12;

        /// <summary>
        /// Nivel de la isla, de 1 a 10. Sale de la suma de niveles de sus habitantes.
        /// </summary>
        /// <remarks>
        /// Se guarda aunque sea derivado, y a propósito: es el número que decide qué
        /// hay abierto, y recalcularlo al vuelo en mitad de una carga —cuando el censo
        /// aún no está montado— daría un cero que cerraría media isla. Lo recalcula
        /// <c>IslandService</c> cuando alguien sube de nivel.
        /// </remarks>
        public int Level = 1;

        /// <summary>Suma de niveles que hace falta para cada nivel de isla, del 1 al 10.</summary>
        public static readonly int[] LevelThresholds =
            { 0, 10, 25, 50, 80, 120, 170, 230, 300, 400 };

        public const int MaxLevel = 10;

        /// <summary>El nivel de isla que corresponde a esa suma de niveles de habitantes.</summary>
        public static int LevelForTotal(int totalIslanderLevels)
        {
            for (int level = MaxLevel; level >= 1; level--)
                if (totalIslanderLevels >= LevelThresholds[level - 1]) return level;
            return 1;
        }

        /// <summary>Ids de habitantes que viven aquí, en orden de llegada.</summary>
        public List<string> ResidentIds = new List<string>();

        public bool IsUnlocked(string buildingId)
        {
            for (int i = 0; i < Buildings.Count; i++)
                if (Buildings[i].BuildingId == buildingId) return Buildings[i].Unlocked;
            return false;
        }

        public bool IsFull => ResidentIds.Count >= PopulationCap;
    }
}
