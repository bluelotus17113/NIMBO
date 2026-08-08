using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 7 — Si no hay música, la pongo; si no hay baile, lo invento.
    /// </summary>
    public sealed class Fiestero : PersonalityBehaviourBase
    {
        public Fiestero() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_FIESTERO",
            TypeIndex = 7,
            DisplayName = "Fiestero",
            Tagline = "Si no hay música, la pongo; si no hay baile, lo invento.",

            WalkSpeed = 1.3f,
            IdleDwell = 1.0f,
            MoodDecay = 0.7f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.2f,
                [NeedKind.Energy] = 1.4f,
                [NeedKind.Social] = 1.4f,
                [NeedKind.Hygiene] = 1.0f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.3f,
                [RequestKind.Object] = 0.9f,
                [RequestKind.Clothes] = 1.2f,
                [RequestKind.Advice] = 0.5f,
                [RequestKind.Favor] = 1.2f,
                [RequestKind.Complaint] = 0.5f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Ecstatic,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,  // ni se entera, ya está en otra cosa
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Ecstatic,
                [PersonalityReaction.Ignored] = Emotion.Angry,  // que no le presten atención le quema
                [PersonalityReaction.RequestGranted] = Emotion.Ecstatic,
                [PersonalityReaction.RequestRefused] = Emotion.Angry,
                [PersonalityReaction.QuarrelStarted] = Emotion.Angry,
                [PersonalityReaction.Reconciled] = Emotion.Happy,
                [PersonalityReaction.LeveledUp] = Emotion.Ecstatic,
                [PersonalityReaction.Complimented] = Emotion.Ecstatic,
                [PersonalityReaction.WokenUp] = Emotion.Happy,  // siempre listo para la fiesta
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "¡Esto está que arde! ¡Y lo que falta!",
                    "No sé qué celebramos, ¡pero celébralo conmigo!",
                },
                [LineMood.Bored] = new[]
                {
                    "¿Nadie se mueve? Esto es un funeral.",
                    "Tres minutos sin ruido. Mi récord personal.",
                },
                [LineMood.Angry] = new[]
                {
                    "¡No me cortes el rollo! Estábamos en lo mejor.",
                    "Si vienes a aguar la fiesta, la puerta está allí.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¡Otro más! Cuéntame rápido: ¿sabes bailar?",
                    "Tú tienes cara de que te gusta la marcha. ¿Acierto?",
                },
            },

            Biases =
            {
                new AffinityBias("PT_AUDAZ", 5),
                new AffinityBias("PT_ENTUSIASTA", 7),
                new AffinityBias("PT_LIDER", 6),
                new AffinityBias("PT_ERMITANIO", -6),
                new AffinityBias("PT_VISIONARIO", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.High,
                Speed = 1.15f,
                Warble = 0.3f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Ecstatic, Emotion.Surprised, Emotion.Angry },
        };
    }
}
