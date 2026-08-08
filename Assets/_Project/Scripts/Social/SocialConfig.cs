using System;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Social
{
    /// <summary>
    /// Umbrales y cambios de afinidad. Salen de <c>Docs/Contratos/relaciones.md</c>.
    /// </summary>
    /// <remarks>
    /// El documento usa un solo eje (Amigo … Enemigo). Aquí la amistad y la riña van
    /// en dos ejes separados, y los umbrales negativos del documento se traducen a la
    /// rama de conflicto. Se hace así porque en este género hace falta poder estar
    /// casado y enfadado a la vez, y con un solo eje eso no se puede escribir.
    /// </remarks>
    [CreateAssetMenu(fileName = "SocialConfig", menuName = "Isla Nimbo/Configuración social")]
    public sealed class SocialConfig : ScriptableObject
    {
        [Serializable]
        public struct InteractionEffect
        {
            public SocialInteraction Interaction;
            public float Affinity;
            [Tooltip("Veces al día que cuenta. 0 = sin límite.")]
            public int DailyCap;
        }

        [Header("Umbrales de amistad")]
        public float FriendThreshold = 25f;
        public float CloseFriendThreshold = 50f;
        public float BestFriendThreshold = 75f;

        [Header("Umbrales de riña")]
        public float TensionThreshold = -10f;
        public float QuarrelThreshold = -25f;
        public float FeudThreshold = -75f;

        [Header("Umbrales de romance")]
        public float CrushMinAffinity = 50f;
        public float CrushMinCompatibility = 0.4f;
        public float DatingThreshold = 80f;
        public float EngagedThreshold = 85f;
        public float BreakupThreshold = 15f;
        public float DivorceThreshold = -50f;

        [Header("Costes emocionales")]
        public float HeartbreakAffinity = -10f;
        public float BreakupAffinity = -20f;
        public float DivorceAffinity = -30f;
        public float DivorceHappiness = -20f;

        [Header("Efecto de cada interacción")]
        [SerializeField]
        private InteractionEffect[] _effects =
        {
            new InteractionEffect { Interaction = SocialInteraction.Chat,         Affinity = 3f,   DailyCap = 3 },
            new InteractionEffect { Interaction = SocialInteraction.Joke,         Affinity = 8f,   DailyCap = 2 },
            new InteractionEffect { Interaction = SocialInteraction.Compliment,   Affinity = 8f,   DailyCap = 1 },
            new InteractionEffect { Interaction = SocialInteraction.Gift,         Affinity = 15f,  DailyCap = 1 },
            new InteractionEffect { Interaction = SocialInteraction.PlayTogether, Affinity = 12f,  DailyCap = 2 },
            new InteractionEffect { Interaction = SocialInteraction.Hug,          Affinity = 10f,  DailyCap = 2 },
            new InteractionEffect { Interaction = SocialInteraction.Apologize,    Affinity = 12f,  DailyCap = 1 },
            new InteractionEffect { Interaction = SocialInteraction.Confess,      Affinity = 20f,  DailyCap = 1 },
            new InteractionEffect { Interaction = SocialInteraction.Argue,        Affinity = -15f, DailyCap = 0 },
            new InteractionEffect { Interaction = SocialInteraction.Ignore,       Affinity = -8f,  DailyCap = 0 },
        };

        [Header("Cómo pesa la compatibilidad de personalidades")]
        [Tooltip("Un cambio de afinidad se multiplica por 0.5 + compatibilidad × este valor.")]
        [Range(0f, 1f)] public float CompatibilityWeight = 0.5f;

        [Header("Enfriamiento")]
        [Tooltip("Puntos de afinidad que se pierden por día sin verse. Acerca a cero, no cruza.")]
        public float DailyDecay = 0.5f;

        public InteractionEffect EffectOf(SocialInteraction interaction)
        {
            for (int i = 0; i < _effects.Length; i++)
                if (_effects[i].Interaction == interaction) return _effects[i];

            Debug.LogWarning($"SocialConfig: no hay efecto para {interaction}");
            return new InteractionEffect { Interaction = interaction, Affinity = 0f, DailyCap = 0 };
        }
    }
}
