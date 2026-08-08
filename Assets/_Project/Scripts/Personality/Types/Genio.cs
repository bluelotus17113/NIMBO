using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 11 — Las reglas son para los que no pueden reescribirlas.
    /// </summary>
    public sealed class Genio : PersonalityBehaviourBase
    {
        public Genio() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_GENIO",
            TypeIndex = 11,
            DisplayName = "Genio",
            Tagline = "Las reglas son para los que no pueden reescribirlas.",

            WalkSpeed = 1.2f,
            IdleDwell = 1.0f,
            MoodDecay = 1.2f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.2f,
                [NeedKind.Energy] = 1.3f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.6f,
                [RequestKind.Object] = 1.8f,
                [RequestKind.Clothes] = 0.4f,
                [RequestKind.Advice] = 0.8f,
                [RequestKind.Favor] = 0.5f,
                [RequestKind.Complaint] = 1.4f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.5f,
                [RequestKind.Reconcile] = 0.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Surprised,  // no esperaba que alguien acertara
                [PersonalityReaction.GiftDisliked] = Emotion.Bored,
                [PersonalityReaction.GiftNeutral] = Emotion.Bored,
                [PersonalityReaction.Introduced] = Emotion.Bored,
                [PersonalityReaction.Ignored] = Emotion.Angry,  // que ignoren su intelecto es imperdonable
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
                    "¡Funciona! Y nadie lo vio venir. Como siempre.",
                    "La idea era tan buena que se escribió sola.",
                },
                [LineMood.Bored] = new[]
                {
                    "Otro problema trivial. ¿Alguien trae algo difícil?",
                    "Mi cabeza va a ochenta y esto va a tres.",
                },
                [LineMood.Angry] = new[]
                {
                    "No me subestimes. Es lo único que no perdono.",
                    "Si no entiendes mi solución, el problema es tuyo.",
                },
                [LineMood.Meeting] = new[]
                {
                    "Sorpréndeme en diez segundos. Cronómetro andando.",
                    "Dime algo incorrecto pero interesante. Así empiezo.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_AUDAZ", 7),
                new AffinityBias("PT_ENTUSIASTA", 6),
                new AffinityBias("PT_VISIONARIO", 7),
                new AffinityBias("PT_AFABLE", -4),
                new AffinityBias("PT_ANFITRION", -3),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.15f,
                Warble = 0.1f,
                Nasal = 0.2f,
            },

            SignatureEmotions = new[] { Emotion.Proud, Emotion.Angry, Emotion.Ecstatic },
        };
    }
}
