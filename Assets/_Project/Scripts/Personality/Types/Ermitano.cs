using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Personality.Runtime;

namespace Nimbo.Personality.Types
{
    /// <summary>
    /// Tipo 0 — calmado, reservado, independiente y práctico.
    /// «La soledad no me pesa; me aligera.»
    /// </summary>
    /// <remarks>
    /// Este fichero es la plantilla de los otros quince: misma forma, distinto
    /// contenido. Los números salen de <c>Docs/Contratos/personalidades.json</c> y el
    /// test <c>PersonalityTablesMatchJson</c> comprueba que no se han separado.
    /// </remarks>
    public sealed class Ermitano : PersonalityBehaviourBase
    {
        public Ermitano() : base(Build()) { }

        private static PersonalityDefinition Build() => new PersonalityDefinition
        {
            Id = "PT_ERMITANIO",
            TypeIndex = 0,
            DisplayName = "Ermitaño",
            Tagline = "La soledad no me pesa; me aligera.",

            WalkSpeed = 0.7f,
            IdleDwell = 4.5f,
            MoodDecay = 1.2f,

            NeedDecay =
            {
                [NeedKind.Hunger] = 0.9f,
                [NeedKind.Energy] = 0.7f,
                [NeedKind.Social] = 0.7f,
                [NeedKind.Hygiene] = 0.9f,
            },

            RequestWeights =
            {
                [RequestKind.Food] = 1.0f,
                [RequestKind.Object] = 1.5f,
                [RequestKind.Clothes] = 0.5f,
                [RequestKind.Advice] = 0.3f,
                [RequestKind.Favor] = 0.6f,
                [RequestKind.Complaint] = 1.0f,
                [RequestKind.SocialIntro] = 0.5f,
                [RequestKind.Activity] = 0.5f,
                [RequestKind.IslandBuilding] = 0.6f,
                [RequestKind.Confession] = 0.5f,
                [RequestKind.Reconcile] = 1.0f,
            },

            Reactions =
            {
                [PersonalityReaction.GiftLoved] = Emotion.Happy,      // no salta: sonríe
                [PersonalityReaction.GiftDisliked] = Emotion.Neutral, // ni se molesta en fingir
                [PersonalityReaction.GiftNeutral] = Emotion.Neutral,
                [PersonalityReaction.Introduced] = Emotion.Worried,
                [PersonalityReaction.Ignored] = Emotion.Happy,        // que lo dejen en paz le gusta
                [PersonalityReaction.RequestGranted] = Emotion.Happy,
                [PersonalityReaction.RequestRefused] = Emotion.Neutral,
                [PersonalityReaction.QuarrelStarted] = Emotion.Angry,
                [PersonalityReaction.Reconciled] = Emotion.Neutral,
                [PersonalityReaction.LeveledUp] = Emotion.Proud,
                [PersonalityReaction.Complimented] = Emotion.Surprised,
                [PersonalityReaction.WokenUp] = Emotion.Angry,
            },

            Lines =
            {
                [LineMood.Happy] = new[]
                {
                    "Así, en silencio… está bien.",
                    "Hoy el ruido no me encontró. Buen día.",
                },
                [LineMood.Bored] = new[]
                {
                    "Ya me miraron demasiado. Hora de irme.",
                    "¿Falta mucho? Tengo un rincón esperándome.",
                },
                [LineMood.Angry] = new[]
                {
                    "Te dije que no me tocaras las cosas.",
                    "No necesito explicaciones. Necesito distancia.",
                },
                [LineMood.Meeting] = new[]
                {
                    "…podemos conversar. Despacio.",
                    "No esperes que te cuente mi vida en un minuto.",
                },
            },

            Biases =
            {
                new AffinityBias("PT_ARTESANO", 5),
                new AffinityBias("PT_POETA", 7),
                new AffinityBias("PT_AFABLE", 3),
                new AffinityBias("PT_FIESTERO", -6),
                new AffinityBias("PT_ENTUSIASTA", -4),
            },

            Voice = new VoiceConfig
            {
                Pitch = VoicePitch.Low,
                Speed = 0.85f,
                Warble = 0.1f,
                Nasal = 0.15f,
            },

            SignatureEmotions = new[] { Emotion.Neutral, Emotion.Bored, Emotion.Worried },
        };
    }
}
