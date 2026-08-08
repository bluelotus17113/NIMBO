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
        /// <c>100·n·√n</c>, la curva de <c>Docs/Contratos/progresion.md</c>: el 1→2
        /// cuesta 100 y el 49→50 unos 34.000, con unos 353.000 en total hasta el 50.
        /// </remarks>
        public static float RequiredFor(int level) => 100f * level * Mathf.Sqrt(level);

        public float RequiredNext => RequiredFor(Level);

        public float NormalizedProgress => Mathf.Clamp01(Experience / Mathf.Max(1f, RequiredNext));

        public bool IsMaxLevel => Level >= MaxLevel;
    }
}
