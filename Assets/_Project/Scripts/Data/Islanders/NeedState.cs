using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Las cuatro necesidades que decaen solas.
    /// </summary>
    /// <remarks>
    /// El ánimo no está aquí a propósito: es derivado de estas cuatro más las
    /// relaciones, y vive en <see cref="MoodState"/>. Si fuese una necesidad más,
    /// habría dos sitios donde escribirlo y acabarían discrepando.
    /// </remarks>
    public enum NeedKind
    {
        Hunger = 0,   // saciedad: 100 = acaba de comer, 0 = hambriento
        Energy = 1,   // descanso
        Social = 2,   // compañía
        Hygiene = 3,  // aseo
    }

    public enum NeedBand
    {
        Critical = 0, // [0, 15]   no hace nada más hasta resolverlo
        Low = 1,      // (15, 35]  probabilidad alta de pedir ayuda
        Normal = 2,   // (35, 80]
        Full = 3,     // (80, 100] +10% de afinidad ganada
    }

    /// <summary>
    /// Las cuatro necesidades de un habitante, de 0 a 100. Alto es bueno: 100 de
    /// Hunger es "acaba de comer". Se eligió así para que todas las barras de la
    /// interfaz se lean igual — llena es buena.
    /// </summary>
    [Serializable]
    public struct NeedState
    {
        public const float Min = 0f;
        public const float Max = 100f;
        public const int Count = 4;

        public const float CriticalThreshold = 15f;
        public const float LowThreshold = 35f;
        public const float FullThreshold = 80f;

        [Range(Min, Max)] public float Hunger;
        [Range(Min, Max)] public float Energy;
        [Range(Min, Max)] public float Social;
        [Range(Min, Max)] public float Hygiene;

        public static NeedState Fresh => new NeedState
        {
            Hunger = 80f, Energy = 90f, Social = 70f, Hygiene = 85f,
        };

        public float this[NeedKind kind]
        {
            get => kind switch
            {
                NeedKind.Hunger => Hunger,
                NeedKind.Energy => Energy,
                NeedKind.Social => Social,
                NeedKind.Hygiene => Hygiene,
                _ => 0f,
            };
            set
            {
                float v = Mathf.Clamp(value, Min, Max);
                switch (kind)
                {
                    case NeedKind.Hunger: Hunger = v; break;
                    case NeedKind.Energy: Energy = v; break;
                    case NeedKind.Social: Social = v; break;
                    case NeedKind.Hygiene: Hygiene = v; break;
                }
            }
        }

        public static NeedBand BandOf(float value)
        {
            if (value <= CriticalThreshold) return NeedBand.Critical;
            if (value <= LowThreshold) return NeedBand.Low;
            if (value <= FullThreshold) return NeedBand.Normal;
            return NeedBand.Full;
        }

        public NeedBand Band(NeedKind kind) => BandOf(this[kind]);

        /// <summary>La necesidad más baja: la que el habitante intentará resolver primero.</summary>
        public NeedKind Lowest()
        {
            NeedKind worst = NeedKind.Hunger;
            float lowest = Hunger;
            for (int i = 1; i < Count; i++)
            {
                var kind = (NeedKind)i;
                float v = this[kind];
                if (v < lowest) { lowest = v; worst = kind; }
            }
            return worst;
        }

        /// <summary>True si alguna está en rojo. El habitante deja lo que esté haciendo.</summary>
        public bool HasCritical() =>
            Hunger <= CriticalThreshold || Energy <= CriticalThreshold ||
            Social <= CriticalThreshold || Hygiene <= CriticalThreshold;

        public float Average => (Hunger + Energy + Social + Hygiene) / Count;

        /// <summary>Media normalizada a [0, 1]. La usa el cálculo de ánimo.</summary>
        public float Normalized => Average / Max;
    }
}
