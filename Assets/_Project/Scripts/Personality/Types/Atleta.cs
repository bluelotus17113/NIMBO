using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 1 — Un objetivo, un cuerpo, ningún atajo.
    /// </summary>
    public sealed class Atleta : PersonalityBehaviourBase
    {
        public Atleta() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ATLETA",
            TypeIndex = 1,
            DisplayName = "Atleta",
            Tagline = "Un objetivo, un cuerpo, ningún atajo.",

            WalkSpeed = 1.3f,
            IdleDwell = 1.2f,
            MoodDecay = 1.0f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 1.2f,
                [NeedKind.Energy] = 1.4f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 1.1f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.4f,
                [RequestKind.Object] = 1.2f,
                [RequestKind.Clothes] = 0.5f,
                [RequestKind.Advice] = 0.4f,
                [RequestKind.Favor] = 0.7f,
                [RequestKind.Complaint] = 0.5f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 1.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 0.5f,
                [RequestKind.Reconcile] = 0.5f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Proud,
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral,
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Neutral,
                [PersonalityReaction.Ignored] = Emotion.Angry,  // no soporta que lo pasen por alto
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
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
                    "¿Viste mi marca? La rompí yo solo.",
                    "Hoy el cuerpo respondió. Eso vale.",
                },
                [LineMood.Bored] = new[]
                {
                    "Esto no entrena nada. Me voy a correr.",
                    "¿Cuánto más hay que esperar? Tengo series.",
                },
                [LineMood.Angry] = new[]
                {
                    "No me digas que no puedo. Eso me toca a mí decidirlo.",
                    "Baja el tono o lo arreglamos en la pista.",
                },
                [LineMood.Meeting] = new[]
                {
                    "¿Qué entrenas? Así sé con quién hablo.",
                    "No doy muchas vueltas. Si quieres algo, dilo.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_AUDAZ", 6),
                new AffinityBias("PT_LIDER", 7),
                new AffinityBias("PT_VISIONARIO", 4),
                new AffinityBias("PT_CUENTISTA", -4),
                new AffinityBias("PT_ROMANTICO", -3),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Mid,
                Speed = 1.15f,
                Warble = 0.1f,
                Nasal = 0.1f,
            },

            SignatureEmotions = new[] { Emotion.Proud, Emotion.Angry, Emotion.Happy },
        };
    }
}
