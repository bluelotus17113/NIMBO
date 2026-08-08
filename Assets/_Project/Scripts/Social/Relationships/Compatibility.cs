using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Social.Relationships
{
    /// <summary>
    /// Cuánto pegan dos personalidades, de −1 a +1. Es la fórmula de
    /// <c>Docs/Contratos/relaciones.md</c>.
    /// </summary>
    /// <remarks>
    /// Los cuatro ejes no pesan igual: <c>Attitude</c> (independiente ↔ sociable) pesa
    /// casi el doble que <c>Energy</c>, porque dos personas con distinto nivel de
    /// energía conviven bien y una que quiere estar sola con otra que quiere estar
    /// acompañada, no.
    /// </remarks>
    public static class Compatibility
    {
        private const float EnergyWeight = 0.20f;
        private const float ExpressionWeight = 0.20f;
        private const float AttitudeWeight = 0.35f;
        private const float OutlookWeight = 0.25f;

        /// <summary>Solo por los ejes, sin contar los sesgos entre tipos. De −1 a +1.</summary>
        public static float Between(in PersonalityProfile a, in PersonalityProfile b)
        {
            float distance =
                Mathf.Abs(a.Energy - b.Energy) * EnergyWeight +
                Mathf.Abs(a.Expression - b.Expression) * ExpressionWeight +
                Mathf.Abs(a.Attitude - b.Attitude) * AttitudeWeight +
                Mathf.Abs(a.Outlook - b.Outlook) * OutlookWeight;

            // La distancia máxima por eje es 2 (de −1 a +1) y los pesos suman 1, así
            // que `distance` va de 0 a 2 y esto la lleva a [+1, −1].
            return 1f - distance;
        }

        /// <summary>
        /// La compatibilidad completa: los ejes más lo que sus dos tipos opinan el uno
        /// del otro. El sesgo entre tipos va de −8 a +8 y aquí cuenta como ±0.4, para
        /// que pueda inclinar la balanza sin decidirla él solo.
        /// </summary>
        public static float Full(in PersonalityProfile a, in PersonalityProfile b, int typeBias) =>
            Mathf.Clamp(Between(a, b) + typeBias * 0.05f, -1f, 1f);
    }
}
