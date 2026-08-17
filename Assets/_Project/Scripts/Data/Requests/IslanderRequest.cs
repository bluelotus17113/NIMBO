using System;

namespace Nimbo.Data.Requests
{
    /// <summary>
    /// Lo que un habitante puede pedirle al jugador. Es el bucle central del juego:
    /// el habitante pide, el jugador decide, y de ahí sale casi todo lo demás.
    /// </summary>
    public enum RequestKind
    {
        Food = 0,            // "quiero comer algo"
        Object = 1,          // quiere un objeto del catálogo
        Clothes = 2,         // quiere una prenda
        Advice = 3,          // tiene un problema y pide consejo
        Favor = 4,           // "¿puedes…?"
        Complaint = 5,       // se queja de otro habitante
        SocialIntro = 6,     // quiere conocer a alguien
        Activity = 7,        // quiere hacer algo contigo o con otro
        IslandBuilding = 8,  // pide una mejora para la isla
        Confession = 9,      // quiere declararse y pide ayuda
        Reconcile = 10,      // quiere hacer las paces

        /// <summary>«Tráeme cinco maderas». Es el encargo que manda al jugador a la isla.</summary>
        /// <remarks>
        /// Va al final y con su número escrito porque estos valores acaban en las
        /// partidas guardadas: reordenar el enum convertiría las quejas de alguien en
        /// encargos de piedra.
        /// </remarks>
        Material = 11,
    }

    public enum RequestPriority
    {
        Low = 0,       // capricho, no hay prisa
        Normal = 1,    // necesidad moderada
        High = 2,      // necesidad urgente
        Critical = 3,  // una necesidad en banda crítica
    }

    public enum RequestResolution
    {
        Pending = 0,
        Fulfilled = 1,   // el jugador la resolvió
        Ignored = 2,     // caducó sin que nadie hiciera nada
        Refused = 3,     // el jugador dijo que no, a la cara
    }

    /// <summary>Una petición concreta en la cola del jugador.</summary>
    [Serializable]
    public struct IslanderRequest
    {
        public string RequestId;
        public string IslanderId;
        public RequestKind Kind;
        public RequestPriority Priority;
        public RequestResolution Resolution;

        /// <summary>Qué pide exactamente: id de objeto, de habitante o de zona, según <see cref="Kind"/>.</summary>
        public string TargetId;

        /// <summary>
        /// Cuántas unidades pide. Solo lo usa <see cref="RequestKind.Material"/>; en
        /// todo lo demás vale 0 y se lee como «una».
        /// </summary>
        public int Amount;

        /// <summary>La frase que dice al pedirlo, ya resuelta con su personalidad.</summary>
        public string Line;

        public long CreatedMinute;
        public long ExpiresMinute;

        /// <summary>Lo que da resolverla. La experiencia va al habitante, las monedas al jugador.</summary>
        public float ExperienceReward;
        public int CoinReward;

        public bool IsOpen => Resolution == RequestResolution.Pending;

        /// <summary>
        /// Ignorar una petición duele más cuanto más urgente era. Ese coste es lo que
        /// convierte la cola en decisiones y no en una lista de tareas.
        /// </summary>
        public float NeglectPenalty => Priority switch
        {
            RequestPriority.Critical => 15f,
            RequestPriority.High => 8f,
            RequestPriority.Normal => 4f,
            _ => 1.5f,
        };
    }
}
