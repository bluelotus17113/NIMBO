using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 5 — Apunta alto, habla claro, no dejes a nadie atrás.
    /// </summary>
    public sealed class Lider : PersonalityBehaviourBase
    {
        public Lider() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_LIDER",
            TypeIndex = 5,
            DisplayName = "Líder",
            Tagline = "Apunta alto, habla claro, no dejes a nadie atrás.",

            WalkSpeed = 1.2f,
            IdleDwell = 1.5f,
            MoodDecay = 0.9f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.2f,
                [NeedKind.Energy] = 1.2f,
                [NeedKind.Social] = 1.3f,
                [NeedKind.Hygiene] = 1.2f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.9f,
                [RequestKind.Object] = 1.0f,
                [RequestKind.Clothes] = 1.1f,
                [RequestKind.Advice] = 1.6f,
                [RequestKind.Favor] = 1.3f,
                [RequestKind.Complaint] = 0.8f,
                [RequestKind.SocialIntro] = 1.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 0.5f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Happy,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Proud,
                [PersonalityReaction.Ignored] = Emotion.Angry,  // ignorar al líder es desacato
                [PersonalityReaction.RequestGranted] = Emotion.Proud,
                [PersonalityReaction.RequestRefused] = Emotion.Angry,
                [PersonalityReaction.QuarrelStarted] = Emotion.Angry,
                [PersonalityReaction.Reconciled] = Emotion.Neutral,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Proud,
                [PersonalityReaction.WokenUp] = Emotion.Angry,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "El equipo funcionó. Eso es lo que importa.",
                    "Hoy todo salió según el plan. Mañana, mejor.",
                },
                [LineMood.Bored] = new[]
                {
                    "Sin objetivos claros, esto se desmorona.",
                    "Alguien tiene que poner orden. Como siempre.",
                },
                [LineMood.Angry] = new[]
                {
                    "Te di una responsabilidad y la tiraste.",
                    "No grites. Di lo que hay que arreglar y arréglalo.",
                },
                [LineMood.Meeting] = new[]
                {
                    "Cuéntame en qué eres bueno. Quizá te necesite.",
                    "Bienvenido. Aquí cada uno tiene un puesto.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ATLETA", 7),
                new AffinityBias("PT_EXPLORADOR", 5),
                new AffinityBias("PT_FIESTERO", 6),
                new AffinityBias("PT_ARTESANO", -4),
                new AffinityBias("PT_ARTISTA", -5),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Low,
                Speed = 1.0f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Proud, Emotion.Angry, Emotion.Happy },
        };
    }
}
