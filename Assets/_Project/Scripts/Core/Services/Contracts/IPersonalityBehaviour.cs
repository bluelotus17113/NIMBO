using System.Collections.Generic;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Situaciones a las que un tipo de personalidad reacciona de forma propia.</summary>
    public enum PersonalityReaction
    {
        GiftLoved = 0,
        GiftDisliked = 1,
        GiftNeutral = 2,
        Introduced = 3,
        Ignored = 4,
        RequestGranted = 5,
        RequestRefused = 6,
        QuarrelStarted = 7,
        Reconciled = 8,
        LeveledUp = 9,
        Complimented = 10,
        WokenUp = 11,
    }

    /// <summary>En qué tono habla. Cada tipo trae sus frases para cada uno.</summary>
    public enum LineMood
    {
        Happy = 0,
        Bored = 1,
        Angry = 2,
        Meeting = 3,
    }

    /// <summary>
    /// Lo que distingue a un tipo de personalidad de otro. Hay dieciséis
    /// implementaciones, una por fichero, en <c>Nimbo.Personality.Types</c>.
    /// </summary>
    /// <remarks>
    /// Todo lo que hay aquí es consulta pura: un comportamiento no muta al habitante,
    /// solo responde preguntas sobre él. Quien aplica el resultado es el módulo que
    /// pregunta. Así los dieciséis ficheros no pueden pisarse entre ellos ni pisar
    /// la simulación.
    /// </remarks>
    public interface IPersonalityBehaviour
    {
        /// <summary>Identificador estable, tipo <c>PT_ERMITANIO</c>. No cambia nunca.</summary>
        string Id { get; }

        /// <summary>De 0 a 15. Tiene que coincidir con el signo de los cuatro ejes.</summary>
        int TypeIndex { get; }

        string DisplayName { get; }
        string Tagline { get; }

        /// <summary>Multiplicador de velocidad al andar, en torno a 1.</summary>
        float WalkSpeedMultiplier { get; }

        /// <summary>Segundos que se queda parado entre destino y destino.</summary>
        float IdleDwellSeconds { get; }

        /// <summary>Cuánto más deprisa se le vacía esa necesidad. 1 = como todo el mundo.</summary>
        float NeedDecayMultiplier(NeedKind need);

        /// <summary>
        /// Lo rápido que su ánimo vuelve al nivel que le corresponde. Alto es
        /// voluble; bajo es de los que se quedan enfurruñados media semana.
        /// </summary>
        float MoodDecayMultiplier { get; }

        /// <summary>Sus tres emociones habituales, para las animaciones de reposo.</summary>
        IReadOnlyList<Emotion> SignatureEmotions { get; }

        /// <summary>Cuánto más probable es que pida eso. 0 = nunca lo pide.</summary>
        float RequestWeight(RequestKind kind);

        /// <summary>La cara que pone ante una situación.</summary>
        Emotion ReactTo(PersonalityReaction reaction);

        /// <summary>Una de sus frases para ese tono. El azar entra por parámetro para poder fijarlo en tests.</summary>
        string PickLine(LineMood mood, ref Rng rng);

        /// <summary>
        /// Lo que suma o resta a la afinidad con alguien de ese otro tipo, antes de
        /// contar nada de lo que hayan hecho juntos. Es simétrico por contrato.
        /// </summary>
        int AffinityBiasToward(int otherTypeIndex);

        /// <summary>La voz que le pega. El creador de personajes la usa como punto de partida.</summary>
        VoiceConfig VoiceTemplate { get; }
    }
}
