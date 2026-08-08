using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 6 — Mi casa es tuya, y tu historia también.
    /// </summary>
    public sealed class Anfitrion : PersonalityBehaviourBase
    {
        public Anfitrion() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ANFITRION",
            TypeIndex = 6,
            DisplayName = "Anfitrión",
            Tagline = "Mi casa es tuya, y tu historia también.",

            WalkSpeed = 0.9f,
            IdleDwell = 2.0f,
            MoodDecay = 0.8f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.0f,
                [NeedKind.Energy] = 0.9f,
                [NeedKind.Social] = 1.3f,
                [NeedKind.Hygiene] = 1.1f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.4f,
                [RequestKind.Object] = 0.8f,
                [RequestKind.Clothes] = 1.0f,
                [RequestKind.Advice] = 1.0f,
                [RequestKind.Favor] = 1.3f,
                [RequestKind.Complaint] = 0.3f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Love,
                [PersonalityReaction.GiftDisliked] = Emotion.Sad,  // tanto esfuerzo para nada
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Ecstatic,  // ¡gente nueva!
                [PersonalityReaction.Ignored] = Emotion.Sad,  // lo ignoren es su peor pesadilla
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Sad,
                [PersonalityReaction.QuarrelStarted] = Emotion.Worried,
                [PersonalityReaction.Reconciled] = Emotion.Love,
                [PersonalityReaction.LeveledUp] = Emotion.Happy,
                [PersonalityReaction.Complimented] = Emotion.Love,
                [PersonalityReaction.WokenUp] = Emotion.Sleepy,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "La mesa llena, las risas… así se vive.",
                    "Vinieron todos. No sabes lo que eso significa.",
                },
                [LineMood.Bored] = new[]
                {
                    "La casa está muy callada. Hay que invitar a alguien.",
                    "Sin gente alrededor, la comida no sabe igual.",
                },
                [LineMood.Angry] = new[]
                {
                    "En mi mesa no se falta al respeto. Punto.",
                    "Te abrí la puerta y me la cerraste en la cara.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¡Pasa, pasa! Justo iba a preparar algo.",
                    "Cuéntame de dónde vienes. Me encantan las historias nuevas.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_AFABLE", 5),
                new AffinityBias("PT_ARTESANO", 5),
                new AffinityBias("PT_CUENTISTA", 6),
                new AffinityBias("PT_ARTISTA", -3),
                new AffinityBias("PT_GENIO", -3),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.0f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Love, Emotion.Surprised, Emotion.Sad },
        };
    }
}
