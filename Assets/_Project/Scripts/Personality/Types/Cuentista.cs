using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 14 — Cada persona es una historia; yo solo la leo en voz alta.
    /// </summary>
    public sealed class Cuentista : PersonalityBehaviourBase
    {
        public Cuentista() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_CUENTISTA",
            TypeIndex = 14,
            DisplayName = "Cuentista",
            Tagline = "Cada persona es una historia; yo solo la leo en voz alta.",

            WalkSpeed = 0.9f,
            IdleDwell = 2.0f,
            MoodDecay = 0.9f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.0f,
                [NeedKind.Energy] = 0.9f,
                [NeedKind.Social] = 1.3f,
                [NeedKind.Hygiene] = 1.0f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.8f,
                [RequestKind.Object] = 0.9f,
                [RequestKind.Clothes] = 1.0f,
                [RequestKind.Advice] = 1.5f,
                [RequestKind.Favor] = 1.3f,
                [RequestKind.Complaint] = 0.7f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.5f,
                [RequestKind.Reconcile] = 1.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Surprised,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Ecstatic,  // ¡material fresco para sus historias!
                [PersonalityReaction.Ignored] = Emotion.Sad,  // no tener público le duele
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Neutral,
                [PersonalityReaction.QuarrelStarted] = Emotion.Worried,
                [PersonalityReaction.Reconciled] = Emotion.Love,  // las reconciliaciones son los mejores finales
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Happy,
                [PersonalityReaction.WokenUp] = Emotion.Sleepy,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Me contaron una historia… y ahora es nuestra.",
                    "Hoy las palabras fluyeron solas. Día bendito.",
                },
                [LineMood.Bored] = new[]
                {
                    "Nadie tiene nada que contar. Eso sí es grave.",
                    "El silencio está bien… pero no tres horas.",
                },
                [LineMood.Angry] = new[]
                {
                    "No me cambies el final. La historia es mía.",
                    "Mentiste. Y una mentira rompe todo el relato.",
                },
                [LineMood.Meeting] = new[]
                {
                    "Siéntate. Esto empieza con un «érase una vez…».",
                    "Tú tienes una historia. Todos tienen una. Cuenta.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ANFITRION", 6),
                new AffinityBias("PT_ARTISTA", 5),
                new AffinityBias("PT_ROMANTICO", 5),
                new AffinityBias("PT_ATLETA", -4),
                new AffinityBias("PT_EXPLORADOR", -3),
                new AffinityBias("PT_POETA", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.0f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Surprised, Emotion.Love, Emotion.Sad },
        };
    }
}
