using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 3 — El riesgo no se calcula: se prueba.
    /// </summary>
    public sealed class Audaz : PersonalityBehaviourBase
    {
        public Audaz() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_AUDAZ",
            TypeIndex = 3,
            DisplayName = "Audaz",
            Tagline = "El riesgo no se calcula: se prueba.",

            WalkSpeed = 1.2f,
            IdleDwell = 1.0f,
            MoodDecay = 1.1f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.0f,
                [NeedKind.Energy] = 1.3f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 1.0f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.2f,
                [RequestKind.Object] = 1.1f,
                [RequestKind.Clothes] = 0.4f,
                [RequestKind.Advice] = 0.3f,
                [RequestKind.Favor] = 1.4f,
                [RequestKind.Complaint] = 0.5f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 1.0f,
                [RequestKind.Reconcile] = 0.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Ecstatic,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Bored,  // si no emociona, no vale
                [PersonalityReaction.Introduced] = Emotion.Happy,
                [PersonalityReaction.Ignored] = Emotion.Angry,
                [PersonalityReaction.RequestGranted] = Emotion.Ecstatic,
                [PersonalityReaction.RequestRefused] = Emotion.Angry,
                [PersonalityReaction.QuarrelStarted] = Emotion.Ecstatic,  // le encanta el conflicto
                [PersonalityReaction.Reconciled] = Emotion.Neutral,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Proud,
                [PersonalityReaction.WokenUp] = Emotion.Neutral,  // siempre alerta
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "¡Lo hice! Y parecía imposible. Mejor así.",
                    "Ese subidón no se compra. Se salta y ya.",
                },
                [LineMood.Bored] = new[]
                {
                    "Esto es demasiado seguro. ¿Dónde está el filo?",
                    "Si nadie va a moverse, me muevo yo solo.",
                },
                [LineMood.Angry] = new[]
                {
                    "¿Miedo? Guárdatelo. Yo no freno por eso.",
                    "No me digas que espere. La ventana se cierra.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿De qué eres capaz? Cuéntame algo que asuste.",
                    "A ver, dime lo más loco que has hecho.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ATLETA", 6),
                new AffinityBias("PT_FIESTERO", 5),
                new AffinityBias("PT_GENIO", 7),
                new AffinityBias("PT_POETA", -3),
                new AffinityBias("PT_ROMANTICO", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.15f,
                Warble = 0.2f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Ecstatic, Emotion.Proud, Emotion.Angry },
        };
    }
}
