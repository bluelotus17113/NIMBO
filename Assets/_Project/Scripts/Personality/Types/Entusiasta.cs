using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 15 — Cada día es un regalo, ¡y pienso desenvolverlo!
    /// </summary>
    public sealed class Entusiasta : PersonalityBehaviourBase
    {
        public Entusiasta() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ENTUSIASTA",
            TypeIndex = 15,
            DisplayName = "Entusiasta",
            Tagline = "Cada día es un regalo, ¡y pienso desenvolverlo!",

            WalkSpeed = 1.2f,
            IdleDwell = 1.0f,
            MoodDecay = 0.8f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.2f,
                [NeedKind.Energy] = 1.3f,
                [NeedKind.Social] = 1.3f,
                [NeedKind.Hygiene] = 1.1f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.2f,
                [RequestKind.Object] = 1.0f,
                [RequestKind.Clothes] = 1.3f,
                [RequestKind.Advice] = 1.0f,
                [RequestKind.Favor] = 1.4f,
                [RequestKind.Complaint] = 0.3f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.5f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Ecstatic,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,  // ya encontrará algo bueno
                [PersonalityReaction.GiftNeutral] = Emotion.Ecstatic,  // ¡un regalo es un regalo!
                [PersonalityReaction.Introduced] = Emotion.Ecstatic,
                [PersonalityReaction.Ignored] = Emotion.Sad,  // necesita que el mundo le devuelva la energía
                [PersonalityReaction.RequestGranted] = Emotion.Ecstatic,
                [PersonalityReaction.RequestRefused] = Emotion.Sad,
                [PersonalityReaction.QuarrelStarted] = Emotion.Worried,
                [PersonalityReaction.Reconciled] = Emotion.Ecstatic,
                [PersonalityReaction.LeveledUp] = Emotion.Ecstatic,
                [PersonalityReaction.Complimented] = Emotion.Ecstatic,
                [PersonalityReaction.WokenUp] = Emotion.Ecstatic,  // ¡ya es de día!
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "¡Qué día! ¡Qué sol! ¡Qué gente! ¡Qué todo!",
                    "Hoy pasó algo increíble. Siéntate que te cuento.",
                },
                [LineMood.Bored] = new[]
                {
                    "¿Cómo puede alguien aburrirse con todo lo que hay?",
                    "¡Hagamos algo ya! Lo que sea, pero ya.",
                },
                [LineMood.Angry] = new[]
                {
                    "¡Con lo bonito que era y lo estropeaste!",
                    "No me digas que me calme. ¡Estoy vivo!",
                },
                [LineMood.Meeting] = new[]
                {
                    "¡Hola! Ya te quiero conocer. Sí, así de rápido.",
                    "Tienes que contarme todo. Pero todo todo.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_EXPLORADOR", 6),
                new AffinityBias("PT_FIESTERO", 7),
                new AffinityBias("PT_GENIO", 6),
                new AffinityBias("PT_ERMITANIO", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.High,
                Speed = 1.15f,
                Warble = 0.3f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Ecstatic, Emotion.Surprised, Emotion.Sad },
        };
    }
}
