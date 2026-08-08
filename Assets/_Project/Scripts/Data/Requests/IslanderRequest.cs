using System;
using UnityEngine;

namespace Nimbo.Data.Requests
{
    /// <summary>
    /// Lo que un habitante puede pedirle al jugador. Es el bucle central del juego:
    /// el habitante pide, el jugador decide, y de ahí sale casi todo lo demás.
    /// </summary>
    public enum RequestKind
    {
        Food = 0,          // "tengo hambre"
        Item = 1,          // quiere un objeto del catálogo
        Clothing = 2,      // quiere ropa
        Advice = 3,        // tiene un problema y pide consejo
        Favor = 4,         // quiere que le presentes a alguien, que le muevas de casa…
        Complaint = 5,     // se queja de otro habitante
        Confession = 6,    // quiere declararse y pide ayuda
        Reconcile = 7,     // quiere hacer las paces
        Furniture = 8,     // quiere algo para su casa
        Outing = 9,        // quiere salir a algún sitio
    }

    public enum RequestState
    {
        Pending = 0,
        Resolved = 1,
        Refused = 2,
        Expired = 3,
    }

    public enum RequestUrgency
    {
        Whim = 0,      // le apetece
        Wish = 1,      // lo quiere de verdad
        Urgent = 2,    // una necesidad en rojo
    }

    /// <summary>
    /// Una petición concreta en la cola del jugador.
    /// </summary>
    [Serializable]
    public struct IslanderRequest
    {
        public string RequestId;
        public string IslanderId;
        public RequestKind Kind;
        public RequestUrgency Urgency;
        public RequestState State;

        /// <summary>Qué pide exactamente: id de objeto, de habitante, de zona… según <see cref="Kind"/>.</summary>
        public string TargetId;

        /// <summary>La frase que dice al pedirlo, ya resuelta con su personalidad.</summary>
        public string Line;

        public long CreatedMinute;
        public long ExpiresMinute;

        /// <summary>Lo que da resolverla. La experiencia va al habitante, las monedas al jugador.</summary>
        public float ExperienceReward;
        public int CoinReward;

        public bool IsOpen => State == RequestState.Pending;
    }
}
