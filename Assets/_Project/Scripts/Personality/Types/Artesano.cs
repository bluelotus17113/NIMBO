using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 2 — Cada cosa rota merece una segunda vida.
    /// </summary>
    public sealed class Artesano : PersonalityBehaviourBase
    {
        public Artesano() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ARTESANO",
            TypeIndex = 2,
            DisplayName = "Artesano",
            Tagline = "Cada cosa rota merece una segunda vida.",

            WalkSpeed = 0.8f,
            IdleDwell = 3.0f,
            MoodDecay = 1.0f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.9f,
                [NeedKind.Energy] = 0.8f,
                [NeedKind.Social] = 0.8f,
                [NeedKind.Hygiene] = 1.0f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.8f,
                [RequestKind.Object] = 1.8f,
                [RequestKind.Clothes] = 0.9f,
                [RequestKind.Advice] = 0.6f,
                [RequestKind.Favor] = 1.0f,
                [RequestKind.Complaint] = 0.4f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Happy,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Neutral,
                [PersonalityReaction.Ignored] = Emotion.Neutral,
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Neutral,
                [PersonalityReaction.QuarrelStarted] = Emotion.Sad,  // odia el conflicto
                [PersonalityReaction.Reconciled] = Emotion.Happy,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Proud,
                [PersonalityReaction.WokenUp] = Emotion.Sleepy,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Quedó justo como lo imaginé. Mira.",
                    "Hoy las manos no me temblaron. Día redondo.",
                },
                [LineMood.Bored] = new[]
                {
                    "Si no hay nada que arreglar, me lo invento.",
                    "Mis herramientas llevan dos horas quietas. Eso es raro.",
                },
                [LineMood.Angry] = new[]
                {
                    "Eso no se rompió solo. Y yo no miento.",
                    "No me toques el taller. Último aviso.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Tienes algo que necesite arreglo? Así empezamos.",
                    "Cuéntame mientras trabajo. No me molesta.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ANFITRION", 5),
                new AffinityBias("PT_ARTISTA", 7),
                new AffinityBias("PT_ERMITANIO", 5),
                new AffinityBias("PT_EXPLORADOR", -3),
                new AffinityBias("PT_LIDER", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.0f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Neutral, Emotion.Proud, Emotion.Sad },
        };
    }
}
