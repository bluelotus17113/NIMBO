using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 10 — Lo que siento no cabe en palabras; por eso pinto.
    /// </summary>
    public sealed class Artista : PersonalityBehaviourBase
    {
        public Artista() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ARTISTA",
            TypeIndex = 10,
            DisplayName = "Artista",
            Tagline = "Lo que siento no cabe en palabras; por eso pinto.",

            WalkSpeed = 0.8f,
            IdleDwell = 2.5f,
            MoodDecay = 1.2f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.9f,
                [NeedKind.Energy] = 0.8f,
                [NeedKind.Social] = 0.8f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 0.7f,
                [RequestKind.Object] = 1.9f,
                [RequestKind.Clothes] = 1.1f,
                [RequestKind.Advice] = 0.5f,
                [RequestKind.Favor] = 0.6f,
                [RequestKind.Complaint] = 1.1f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 1.4f,
                [RequestKind.Confession] = 1.5f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Ecstatic,
                [PersonalityReaction.GiftDisliked] = Emotion.Sad,  // le hiere en lo profundo
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Worried,
                [PersonalityReaction.Ignored] = Emotion.Sad,  // que ignoren su obra es que ignoren su alma
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Angry,
                [PersonalityReaction.QuarrelStarted] = Emotion.Angry,
                [PersonalityReaction.Reconciled] = Emotion.Love,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Ecstatic,
                [PersonalityReaction.WokenUp] = Emotion.Angry,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "El trazo salió solo. Esos son los buenos.",
                    "Hoy los colores no mienten. Todo encaja.",
                },
                [LineMood.Bored] = new[]
                {
                    "El lienzo en blanco y yo también.",
                    "Necesito que algo me rompa el cascarón.",
                },
                [LineMood.Angry] = new[]
                {
                    "No me digas cómo se siente. Lo sé antes que tú.",
                    "Criticar no es crear. Enséñame lo tuyo y hablamos.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Qué color te gusta? Así empiezo a conocerte.",
                    "Muéstrame algo que hayas hecho. Lo que sea.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ARTESANO", 7),
                new AffinityBias("PT_CUENTISTA", 5),
                new AffinityBias("PT_POETA", 6),
                new AffinityBias("PT_ANFITRION", -3),
                new AffinityBias("PT_LIDER", -5),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.0f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Surprised, Emotion.Sad, Emotion.Ecstatic },
        };
    }
}
