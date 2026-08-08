using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Nivel y experiencia de un habitante. Subir de nivel es lo que hace que el
    /// jugador siga atendiendo peticiones cuando ya tiene todo lo que quería.
    /// </summary>
    [Serializable]
    public struct ProgressionState
    {
        public const int MaxLevel = 50;

        public int Level;
        public float Experience;        // dentro del nivel actual
        public int PendingRewards;      // recompensas ganadas y no recogidas

        public static ProgressionState Fresh => new ProgressionState { Level = 1, Experience = 0f };

        /// <summary>Experiencia para pasar de <paramref name="level"/> al siguiente.</summary>
        /// <remarks>
        /// Cuadrática suave: nivel 1→2 cuesta 100, y el 49→50 unos 3.700. Con las
        /// fuentes de experiencia del diseño eso son semanas de juego, no meses.
        /// </remarks>
        public static float RequiredFor(int level) => 100f + 1.5f * level * level + 20f * level;

        public float RequiredNext => RequiredFor(Level);

        public float NormalizedProgress => Mathf.Clamp01(Experience / Mathf.Max(1f, RequiredNext));

        public bool IsMaxLevel => Level >= MaxLevel;
    }
}
