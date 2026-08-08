using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Los cuatro ejes continuos de los que sale la personalidad de un habitante.
    /// El orden importa: fija el bit que cada eje aporta al índice del tipo.
    /// </summary>
    public enum PersonalityAxis
    {
        Energy = 0,
        Expression = 1,
        Attitude = 2,
        Outlook = 3,
    }

    /// <summary>
    /// La personalidad tal y como se guarda: cuatro números en [-1, 1].
    /// El tipo (0-15) no se guarda nunca, se deriva. Así el creador de personajes
    /// solo mueve deslizadores y el tipo cae solo, sin poder quedar desincronizado.
    /// </summary>
    [Serializable]
    public struct PersonalityProfile : IEquatable<PersonalityProfile>
    {
        public const int TypeCount = 16;
        public const int AxisCount = 4;

        [Range(-1f, 1f)] public float Energy;      // -1 calmado    ... +1 enérgico
        [Range(-1f, 1f)] public float Expression;  // -1 reservado  ... +1 expresivo
        [Range(-1f, 1f)] public float Attitude;    // -1 independiente ... +1 sociable
        [Range(-1f, 1f)] public float Outlook;     // -1 práctico   ... +1 soñador

        public PersonalityProfile(float energy, float expression, float attitude, float outlook)
        {
            Energy = Mathf.Clamp(energy, -1f, 1f);
            Expression = Mathf.Clamp(expression, -1f, 1f);
            Attitude = Mathf.Clamp(attitude, -1f, 1f);
            Outlook = Mathf.Clamp(outlook, -1f, 1f);
        }

        /// <summary>Índice del tipo, de 0 a 15. Un bit por eje, en el orden del enum.</summary>
        public int TypeIndex =>
            (Energy > 0f ? 1 : 0) |
            (Expression > 0f ? 2 : 0) |
            (Attitude > 0f ? 4 : 0) |
            (Outlook > 0f ? 8 : 0);

        public float this[PersonalityAxis axis] => axis switch
        {
            PersonalityAxis.Energy => Energy,
            PersonalityAxis.Expression => Expression,
            PersonalityAxis.Attitude => Attitude,
            PersonalityAxis.Outlook => Outlook,
            _ => 0f,
        };

        /// <summary>
        /// Cuán marcada es la personalidad, de 0 a 1. Un habitante con los cuatro ejes
        /// cerca de cero es de su tipo "a medias" y se comporta de forma más neutra.
        /// </summary>
        public float Intensity =>
            (Mathf.Abs(Energy) + Mathf.Abs(Expression) + Mathf.Abs(Attitude) + Mathf.Abs(Outlook)) * 0.25f;

        /// <summary>Distancia entre dos personalidades, de 0 (idénticas) a 1 (opuestas).</summary>
        public static float Distance(in PersonalityProfile a, in PersonalityProfile b)
        {
            float d = Mathf.Abs(a.Energy - b.Energy)
                    + Mathf.Abs(a.Expression - b.Expression)
                    + Mathf.Abs(a.Attitude - b.Attitude)
                    + Mathf.Abs(a.Outlook - b.Outlook);
            return d * 0.125f; // cada eje aporta como mucho 2, y son cuatro
        }

        /// <summary>El perfil canónico de un tipo: cada eje a ±0.75, no a ±1.</summary>
        /// <remarks>
        /// A ±1 los 16 arquetipos salen caricaturescos y todos los del mismo tipo se
        /// mueven igual. A ±0.75 queda sitio para que dos del mismo tipo se distingan.
        /// </remarks>
        public static PersonalityProfile FromTypeIndex(int index)
        {
            const float M = 0.75f;
            return new PersonalityProfile(
                (index & 1) != 0 ? M : -M,
                (index & 2) != 0 ? M : -M,
                (index & 4) != 0 ? M : -M,
                (index & 8) != 0 ? M : -M);
        }

        public bool Equals(PersonalityProfile other) =>
            Mathf.Approximately(Energy, other.Energy) &&
            Mathf.Approximately(Expression, other.Expression) &&
            Mathf.Approximately(Attitude, other.Attitude) &&
            Mathf.Approximately(Outlook, other.Outlook);

        public override bool Equals(object obj) => obj is PersonalityProfile p && Equals(p);

        public override int GetHashCode() => HashCode.Combine(Energy, Expression, Attitude, Outlook);

        public override string ToString() =>
            $"[E {Energy:+0.00;-0.00} X {Expression:+0.00;-0.00} A {Attitude:+0.00;-0.00} O {Outlook:+0.00;-0.00}] → tipo {TypeIndex}";
    }
}
