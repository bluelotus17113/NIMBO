using System;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Simulation.Needs
{
    /// <summary>
    /// Todos los números del decaimiento de necesidades y del ánimo, en un sitio
    /// que se puede tocar sin recompilar.
    /// </summary>
    /// <remarks>
    /// Los valores por defecto salen de <c>Docs/Contratos/necesidades.md</c>. Si los
    /// cambias aquí, cambia también el documento: el documento es la explicación de
    /// por qué son esos, y un número sin explicación se acaba tocando a ciegas.
    /// </remarks>
    [CreateAssetMenu(fileName = "NeedsConfig", menuName = "Isla Nimbo/Configuración de necesidades")]
    public sealed class NeedsConfig : ScriptableObject
    {
        [Serializable]
        public struct DecayRate
        {
            public NeedKind Need;
            [Tooltip("Puntos por hora de juego. Negativo: baja.")]
            public float PerHour;
        }

        [Header("Decaimiento por hora de juego")]
        [SerializeField]
        private DecayRate[] _decay =
        {
            new DecayRate { Need = NeedKind.Hunger,  PerHour = -4.0f },
            new DecayRate { Need = NeedKind.Energy,  PerHour = -5.0f },
            new DecayRate { Need = NeedKind.Social,  PerHour = -1.5f },
            new DecayRate { Need = NeedKind.Hygiene, PerHour = -2.5f },
        };

        [Header("Cómo modula la personalidad")]
        [Tooltip("Cuánto desplaza el eje Energy al decaimiento de todas las necesidades.")]
        [Range(0f, 0.5f)] public float EnergyAxisInfluence = 0.15f;

        [Header("Dormir")]
        [Tooltip("Energía por hora mientras duerme en su propia cama.")]
        public float SleepRecoveryOwnBed = 12f;
        [Tooltip("Energía por hora durmiendo en un sofá.")]
        public float SleepRecoverySofa = 6f;
        [Tooltip("Lo que baja la energía mientras duerme, en vez del ritmo normal.")]
        public float SleepingEnergyDecay = -2f;

        [Header("Efectos de banda crítica")]
        [Tooltip("Energía extra que se pierde por hora con el hambre en rojo.")]
        public float StarvingEnergyDrain = -3f;
        [Tooltip("Ánimo que se pierde por hora con la soledad en rojo.")]
        public float LonelyMoodDrain = -5f;
        [Tooltip("Penalización de afinidad al interactuar con alguien sucio.")]
        [Range(0f, 1f)] public float DirtyAffinityPenalty = 0.2f;
        [Tooltip("Bonus de afinidad cuando la necesidad está en banda plena.")]
        [Range(0f, 1f)] public float FullNeedAffinityBonus = 0.1f;

        [Header("Ánimo")]
        [Tooltip("Peso de las necesidades en el ánimo. El resto lo aportan las relaciones.")]
        [Range(0f, 1f)] public float MoodNeedsWeight = 0.7f;
        [Tooltip("Puntos por hora a los que el ánimo tiende hacia su valor objetivo.")]
        public float MoodApproachPerHour = 12f;

        public float DecayPerHour(NeedKind need)
        {
            for (int i = 0; i < _decay.Length; i++)
                if (_decay[i].Need == need) return _decay[i].PerHour;
            return 0f;
        }

        /// <summary>
        /// Multiplicador total de decaimiento para ese habitante: lo que dice su tipo
        /// de personalidad, desplazado además por lo enérgico que sea.
        /// </summary>
        public float DecayMultiplier(float personalityMultiplier, float energyAxis) =>
            Mathf.Max(0.1f, personalityMultiplier + energyAxis * EnergyAxisInfluence);
    }
}
