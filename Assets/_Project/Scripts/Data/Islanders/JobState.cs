using System;

namespace Nimbo.Data.Islanders
{
    /// <summary>
    /// Los oficios de la isla. Cada uno pega con unos ejes de personalidad, así que
    /// un habitante rinde más en el que le corresponde y eso se nota en el sueldo.
    /// </summary>
    public enum JobKind
    {
        None = 0,
        Cook = 1,        // cocinero: práctico y sociable
        Shopkeeper = 2,  // tendero: sociable y expresivo
        Gardener = 3,    // jardinero: calmado y práctico
        Artist = 4,      // artista: soñador y expresivo
        Builder = 5,     // constructor: enérgico y práctico
        Musician = 6,    // músico: expresivo y soñador
        Guide = 7,       // guía: enérgico y sociable
        Librarian = 8,   // bibliotecario: calmado y reservado
    }

    /// <summary>
    /// En qué trabaja un habitante y cómo le va.
    /// </summary>
    /// <remarks>
    /// El trabajo es la fuente principal de ingresos del jugador y por eso está en los
    /// datos del habitante y no en un registro aparte: si se muda o se va de la isla,
    /// su empleo se va con él.
    /// </remarks>
    [Serializable]
    public struct JobState
    {
        public const int MaxRank = 5;

        public JobKind Kind;

        /// <summary>De 1 a 5. Sube con los días trabajados y multiplica el sueldo.</summary>
        public int Rank;

        /// <summary>Turnos completados en el puesto actual.</summary>
        public int ShiftsWorked;

        /// <summary>Día de juego del último turno, para no pagar dos veces el mismo.</summary>
        public int LastShiftDay;

        /// <summary>Lo que ha ganado en total. Sale en su ficha.</summary>
        public long TotalEarned;

        public bool HasJob => Kind != JobKind.None;

        public static JobState Unemployed => new JobState { Kind = JobKind.None, Rank = 1 };

        /// <summary>Turnos que hacen falta para el siguiente ascenso.</summary>
        public int ShiftsForNextRank => Rank >= MaxRank ? int.MaxValue : 6 + Rank * 4;

        public bool CanPromote => Rank < MaxRank && ShiftsWorked >= ShiftsForNextRank;
    }
}
