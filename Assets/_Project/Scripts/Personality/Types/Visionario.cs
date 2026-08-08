using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 9 — Lo que otros ven imposible yo lo estoy construyendo.
    /// </summary>
    public sealed class Visionario : PersonalityBehaviourBase
    {
        public Visionario() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_VISIONARIO",
            TypeIndex = 9,
            DisplayName = "Visionario",
            Tagline = "Lo que otros ven imposible yo lo estoy construyendo.",

            WalkSpeed = 1.1f,
            IdleDwell = 1.8f,
            MoodDecay = 1.1f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.1f,
                [NeedKind.Energy] = 1.2f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.7f,
                [RequestKind.Object] = 1.7f,
                [RequestKind.Clothes] = 0.5f,
                [RequestKind.Advice] = 1.2f,
                [RequestKind.Favor] = 0.6f,
                [RequestKind.Complaint] = 0.9f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 0.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Happy,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Bored,
                [PersonalityReaction.Introduced] = Emotion.Neutral,
                [PersonalityReaction.Ignored] = Emotion.Angry,  // sus ideas merecen audiencia
                [PersonalityReaction.RequestGranted] = Emotion.Proud,
                [PersonalityReaction.RequestRefused] = Emotion.Angry,
                [PersonalityReaction.QuarrelStarted] = Emotion.Angry,
                [PersonalityReaction.Reconciled] = Emotion.Neutral,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Proud,
                [PersonalityReaction.WokenUp] = Emotion.Neutral,  // siempre está pensando, dormir es un trámite
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "El prototipo funciona. Mañana lo cuento; hoy lo vivo.",
                    "Por fin alguien lo entendió. Uno basta.",
                },
                [LineMood.Bored] = new[]
                {
                    "Esto ya lo resolví mentalmente hace una hora.",
                    "Conversación de mantenimiento. No aporta.",
                },
                [LineMood.Angry] = new[]
                {
                    "No me digas que es imposible. Dime que no lo ves.",
                    "Cada vez que me frenan, doblo la apuesta.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Qué construirías si nadie te dijera que no?",
                    "Dime algo que no sepa. Lo demás sobra.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ATLETA", 4),
                new AffinityBias("PT_EXPLORADOR", 5),
                new AffinityBias("PT_GENIO", 7),
                new AffinityBias("PT_AFABLE", -3),
                new AffinityBias("PT_FIESTERO", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.15f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Proud, Emotion.Angry, Emotion.Surprised },
        };
    }
}
