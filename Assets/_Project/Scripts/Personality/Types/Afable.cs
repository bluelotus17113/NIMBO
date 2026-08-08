using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 4 — Si alguien tiene que ceder, que empiece por mí.
    /// </summary>
    public sealed class Afable : PersonalityBehaviourBase
    {
        public Afable() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_AFABLE",
            TypeIndex = 4,
            DisplayName = "Afable",
            Tagline = "Si alguien tiene que ceder, que empiece por mí.",

            WalkSpeed = 0.8f,
            IdleDwell = 3.5f,
            MoodDecay = 0.8f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.9f,
                [NeedKind.Energy] = 0.8f,
                [NeedKind.Social] = 1.2f,
                [NeedKind.Hygiene] = 1.0f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.0f,
                [RequestKind.Object] = 0.6f,
                [RequestKind.Clothes] = 0.7f,
                [RequestKind.Advice] = 1.4f,
                [RequestKind.Favor] = 1.6f,
                [RequestKind.Complaint] = 0.2f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 0.5f,
                [RequestKind.Reconcile] = 1.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Happy,
                [PersonalityReaction.GiftDisliked] = Emotion.Sad,  // no quiere herir al que regaló
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Happy,
                [PersonalityReaction.Ignored] = Emotion.Sad,  // le duele que no le hablen
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Sad,
                [PersonalityReaction.QuarrelStarted] = Emotion.Sad,
                [PersonalityReaction.Reconciled] = Emotion.Love,  // nada le llena más que hacer las paces
                [PersonalityReaction.LeveledUp] = Emotion.Happy,
                [PersonalityReaction.Complimented] = Emotion.Love,
                [PersonalityReaction.WokenUp] = Emotion.Sleepy,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Todos tan contentos… hoy dormiré tranquilo.",
                    "Pude ayudar. Eso ya hace que valga el día.",
                },
                [LineMood.Bored] = new[]
                {
                    "Nadie necesita nada. Qué raro se siente.",
                    "Mientras no haya prisa, me tomo otro té.",
                },
                [LineMood.Angry] = new[]
                {
                    "No confundas mi calma con que no me importa.",
                    "Me empujaste tres veces. A la cuarta respondo.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Estás bien? Tienes cara de que necesitas hablar.",
                    "Ven, siéntate. No muerdo y sobra silla.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ANFITRION", 5),
                new AffinityBias("PT_ERMITANIO", 3),
                new AffinityBias("PT_ROMANTICO", 6),
                new AffinityBias("PT_GENIO", -4),
                new AffinityBias("PT_VISIONARIO", -3),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 0.85f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Neutral, Emotion.Love, Emotion.Worried },
        };
    }
}
