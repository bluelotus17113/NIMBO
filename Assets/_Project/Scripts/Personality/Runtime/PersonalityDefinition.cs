using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;

namespace Nimbo.Personality.Runtime
{
    /// <summary>Lo que un tipo congenia (o choca) con otro. El signo lo dice todo.</summary>
    public readonly struct AffinityBias
    {
        public readonly string OtherId;
        public readonly int Amount;   // positivo congenia, negativo choca

        public AffinityBias(string otherId, int amount)
        {
            OtherId = otherId;
            Amount = amount;
        }
    }

    /// <summary>
    /// La ficha rellenable de un tipo de personalidad. Cada uno de los dieciséis
    /// ficheros de <c>Nimbo.Personality.Types</c> devuelve una de estas y ya está.
    /// </summary>
    /// <remarks>
    /// Existe para que los dieciséis ficheros sean idénticos en forma y solo distintos
    /// en contenido. Si un tipo necesitara un campo que aquí no hay, el sitio de
    /// añadirlo es este fichero — y entonces lo tienen los dieciséis.
    /// </remarks>
    public sealed class PersonalityDefinition
    {
        public string Id;
        public int TypeIndex;
        public string DisplayName;
        public string Tagline;

        public float WalkSpeed = 1f;
        public float IdleDwell = 3f;

        /// <summary>Multiplicador de decaimiento por necesidad. Lo que falte vale 1.</summary>
        public Dictionary<NeedKind, float> NeedDecay = new Dictionary<NeedKind, float>();

        /// <summary>Lo rápido que su felicidad vuelve a su nivel de fondo. 1 = normal.</summary>
        public float MoodDecay = 1f;

        /// <summary>Peso de cada tipo de petición. Lo que falte vale 1; 0 = no lo pide nunca.</summary>
        public Dictionary<RequestKind, float> RequestWeights = new Dictionary<RequestKind, float>();

        /// <summary>La cara que pone en cada situación. Lo que falte cae en <see cref="Emotion.Neutral"/>.</summary>
        public Dictionary<PersonalityReaction, Emotion> Reactions =
            new Dictionary<PersonalityReaction, Emotion>();

        /// <summary>Sus frases, dos por tono como mínimo.</summary>
        public Dictionary<LineMood, string[]> Lines = new Dictionary<LineMood, string[]>();

        /// <summary>Con quién congenia y con quién choca. Tiene que ser simétrico entre tipos.</summary>
        public List<AffinityBias> Biases = new List<AffinityBias>();

        public VoiceConfig Voice = VoiceConfig.Default;

        /// <summary>Sus tres emociones habituales, para animaciones de reposo.</summary>
        public Emotion[] SignatureEmotions = { Emotion.Neutral, Emotion.Happy, Emotion.Bored };
    }
}
