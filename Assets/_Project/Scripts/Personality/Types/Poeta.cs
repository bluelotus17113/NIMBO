using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 8 — Las palabras que no digo son las que más pesan.
    /// </summary>
    public sealed class Poeta : PersonalityBehaviourBase
    {
        public Poeta() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_POETA",
            TypeIndex = 8,
            DisplayName = "Poeta",
            Tagline = "Las palabras que no digo son las que más pesan.",

            WalkSpeed = 0.7f,
            IdleDwell = 4.0f,
            MoodDecay = 1.3f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.7f,
                [NeedKind.Energy] = 0.7f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.5f,
                [RequestKind.Object] = 1.6f,
                [RequestKind.Clothes] = 0.4f,
                [RequestKind.Advice] = 1.0f,
                [RequestKind.Favor] = 0.7f,
                [RequestKind.Complaint] = 1.3f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Surprised,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Worried,
                [PersonalityReaction.Ignored] = Emotion.Happy,  // el silencio es su idioma
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Neutral,
                [PersonalityReaction.QuarrelStarted] = Emotion.Sad,
                [PersonalityReaction.Reconciled] = Emotion.Surprised,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Surprised,
                [PersonalityReaction.WokenUp] = Emotion.Sad,  // despertar rompe el sueño donde estaba el verso
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Hoy el cielo pesa menos. Algo hice bien.",
                    "Encontré la palabra justa. Eso basta.",
                },
                [LineMood.Bored] = new[]
                {
                    "El silencio es bueno… pero hoy sobra.",
                    "Ni una nube, ni un verso. Páramo total.",
                },
                [LineMood.Angry] = new[]
                {
                    "Tus palabras talan más que un hacha. Cállate.",
                    "No necesito que me entiendas. Necesito que te vayas.",
                },
                [LineMood.Meeting] = new[]
                {
                    "Dime algo breve. Lo breve salva.",
                    "No esperes que te cuente todo. Lo importante cabe en tres líneas.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ARTISTA", 6),
                new AffinityBias("PT_ERMITANIO", 7),
                new AffinityBias("PT_ROMANTICO", 4),
                new AffinityBias("PT_AUDAZ", -3),
                new AffinityBias("PT_CUENTISTA", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Low,
                Speed = 0.85f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Sad, Emotion.Neutral, Emotion.Surprised },
        };
    }
}
