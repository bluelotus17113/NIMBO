using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 12 — El mundo está bien, pero podría estar mejor con dos.
    /// </summary>
    public sealed class Romantico : PersonalityBehaviourBase
    {
        public Romantico() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ROMANTICO",
            TypeIndex = 12,
            DisplayName = "Romántico",
            Tagline = "El mundo está bien, pero podría estar mejor con dos.",

            WalkSpeed = 0.8f,
            IdleDwell = 3.0f,
            MoodDecay = 1.1f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.9f,
                [NeedKind.Energy] = 0.8f,
                [NeedKind.Social] = 1.2f,
                [NeedKind.Hygiene] = 1.1f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.9f,
                [RequestKind.Object] = 1.1f,
                [RequestKind.Clothes] = 1.2f,
                [RequestKind.Advice] = 1.3f,
                [RequestKind.Favor] = 1.4f,
                [RequestKind.Complaint] = 0.6f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Love,
                [PersonalityReaction.GiftDisliked] = Emotion.Sad,
                [PersonalityReaction.GiftNeutral] = Emotion.Happy,
                [PersonalityReaction.Introduced] = Emotion.Love,  // cada nuevo encuentro es una posibilidad
                [PersonalityReaction.Ignored] = Emotion.Sad,  // la indiferencia es lo contrario del amor
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Sad,
                [PersonalityReaction.QuarrelStarted] = Emotion.Sad,
                [PersonalityReaction.Reconciled] = Emotion.Love,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Love,
                [PersonalityReaction.WokenUp] = Emotion.Sleepy,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Me miró distinto. Con eso ya duermo.",
                    "Hoy el mundo fue amable. Hay que agradecer.",
                },
                [LineMood.Bored] = new[]
                {
                    "Falta algo… y no sé qué. Eso es lo peor.",
                    "Otro atardecer sin nadie al lado. Qué desperdicio.",
                },
                [LineMood.Angry] = new[]
                {
                    "Me prometiste y no cumpliste. Eso no se olvida.",
                    "No juegues con lo que yo pongo en serio.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Crees en las señales? Yo sí. Y tú llegaste.",
                    "Háblame de alguien a quien quieras. Así te conozco.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_AFABLE", 6),
                new AffinityBias("PT_CUENTISTA", 5),
                new AffinityBias("PT_POETA", 4),
                new AffinityBias("PT_ATLETA", -3),
                new AffinityBias("PT_AUDAZ", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 0.85f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Love, Emotion.Sad, Emotion.Happy },
        };
    }
}
