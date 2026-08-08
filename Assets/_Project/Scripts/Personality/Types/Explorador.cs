using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 13 — El mapa se acaba donde empieza mi curiosidad.
    /// </summary>
    public sealed class Explorador : PersonalityBehaviourBase
    {
        public Explorador() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_EXPLORADOR",
            TypeIndex = 13,
            DisplayName = "Explorador",
            Tagline = "El mapa se acaba donde empieza mi curiosidad.",

            WalkSpeed = 1.2f,
            IdleDwell = 1.3f,
            MoodDecay = 0.9f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.1f,
                [NeedKind.Energy] = 1.3f,
                [NeedKind.Social] = 1.1f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.1f,
                [RequestKind.Object] = 1.5f,
                [RequestKind.Clothes] = 0.7f,
                [RequestKind.Advice] = 1.0f,
                [RequestKind.Favor] = 1.0f,
                [RequestKind.Complaint] = 0.4f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Ecstatic,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Happy,
                [PersonalityReaction.Ignored] = Emotion.Neutral,  // él ya está mirando otra cosa
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Neutral,  // ya encontrará otro camino
                [PersonalityReaction.QuarrelStarted] = Emotion.Bored,  // discutir no lleva a ninguna parte nueva
                [PersonalityReaction.Reconciled] = Emotion.Happy,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Surprised,
                [PersonalityReaction.WokenUp] = Emotion.Happy,  // otra oportunidad de explorar
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Encontré un sitio que no está en los mapas.",
                    "Hoy toca ruta nueva. Las viejas ya me las sé.",
                },
                [LineMood.Bored] = new[]
                {
                    "Otra vez el mismo paisaje. Necesito horizonte.",
                    "Si no hay nada que descubrir, ¿para qué salir?",
                },
                [LineMood.Angry] = new[]
                {
                    "No me ates a un sitio. Yo no funciono así.",
                    "Cada frontera que pones es una que voy a cruzar.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿De dónde vienes? Lo importante es el camino.",
                    "Cuéntame algo que solo se aprenda viajando.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ENTUSIASTA", 6),
                new AffinityBias("PT_LIDER", 5),
                new AffinityBias("PT_VISIONARIO", 5),
                new AffinityBias("PT_ARTESANO", -3),
                new AffinityBias("PT_CUENTISTA", -3),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Low,
                Speed = 1.0f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Happy, Emotion.Surprised, Emotion.Worried },
        };
    }
}
