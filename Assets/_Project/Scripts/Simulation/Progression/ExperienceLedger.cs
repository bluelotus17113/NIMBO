using Nimbo.Core.Events;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Simulation.Progression
{
    /// <summary>
    /// Reparte experiencia y sube de nivel. Aparte del servicio de simulación porque
    /// subir de nivel encadena recompensas, y eso conviene poder probarlo solo.
    /// </summary>
    public sealed class ExperienceLedger
    {
        public void Grant(IslanderData islander, float amount)
        {
            if (amount <= 0f || islander.Progression.IsMaxLevel) return;

            islander.Progression.Experience += amount;

            // Con un bucle, y no con un if: una recompensa gorda puede saltar dos
            // niveles de golpe y el jugador esperaría ver las dos.
            while (!islander.Progression.IsMaxLevel &&
                   islander.Progression.Experience >= islander.Progression.RequiredNext)
            {
                islander.Progression.Experience -= islander.Progression.RequiredNext;
                islander.Progression.Level++;
                islander.Progression.PendingRewards++;

                EventBus.Publish(new IslanderLeveledUp(islander.Id, islander.Progression.Level));
            }

            if (islander.Progression.IsMaxLevel)
                islander.Progression.Experience = 0f;
        }

        /// <summary>Recoge las recompensas pendientes. Devuelve cuántas había.</summary>
        public int ClaimRewards(IslanderData islander)
        {
            int pending = islander.Progression.PendingRewards;
            islander.Progression.PendingRewards = 0;
            return pending;
        }

        /// <summary>Experiencia total acumulada desde el nivel 1. La usa la interfaz de resumen.</summary>
        public static float TotalExperienceAt(int level, float intoLevel)
        {
            float total = intoLevel;
            for (int n = 1; n < level; n++) total += ProgressionState.RequiredFor(n);
            return Mathf.Max(0f, total);
        }
    }
}
