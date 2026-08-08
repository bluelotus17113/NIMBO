using System;
using UnityEngine;

namespace Nimbo.Data.Islanders
{
    public enum NeedKind
    {
        Hunger = 0,   // saciedad: 100 = lleno, 0 = hambriento
        Energy = 1,   // descanso
        Social = 2,   // compañía
        Hygiene = 3,  // aseo
        Fun = 4,      // entretenimiento
    }

    public enum NeedBand
    {
        Critical = 0, // [0, 15)
        Low = 1,      // [15, 40)
        Normal = 2,   // [40, 80)
        Full = 3,     // [80, 100]
    }

    /// <summary>
    /// Las cinco necesidades de un habitante, cada una de 0 a 100. Alto es bueno:
    /// 100 de Hunger es "acaba de comer", no "se muere de hambre". Se eligió así
    /// para que todas las barras de la interfaz se lean igual — llena es buena.
    /// </summary>
    [Serializable]
    public struct NeedState
    {
        public const float Min = 0f;
        public const float Max = 100f;
        public const int Count = 5;

        [Range(Min, Max)] public float Hunger;
        [Range(Min, Max)] public float Energy;
        [Range(Min, Max)] public float Social;
        [Range(Min, Max)] public float Hygiene;
        [Range(Min, Max)] public float Fun;

        public static NeedState Fresh => new NeedState
        {
            Hunger = 80f, Energy = 90f, Social = 70f, Hygiene = 85f, Fun = 75f,
        };

        public float this[NeedKind kind]
        {
            get => kind switch
            {
                NeedKind.Hunger => Hunger,
                NeedKind.Energy => Energy,
                NeedKind.Social => Social,
                NeedKind.Hygiene => Hygiene,
                NeedKind.Fun => Fun,
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
                    case NeedKind.Fun: Fun = v; break;
                }
            }
        }

        public static NeedBand BandOf(float value)
        {
            if (value < 15f) return NeedBand.Critical;
            if (value < 40f) return NeedBand.Low;
            if (value < 80f) return NeedBand.Normal;
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

        public float Average => (Hunger + Energy + Social + Hygiene + Fun) / Count;
    }
}
