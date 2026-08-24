using System.Collections.Generic;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se puede montar esa fiesta ahora mismo (§15.3).</summary>
    public enum HostRefusal
    {
        Ok = 0,
        UnknownEvent,
        NotUnlocked,        // Aldea 5 para las pequeñas, Aldea 7 para los festivales
        NotEnoughCoins,
        AlreadyRunning,     // ya hay algo puesto
        NotYet,             // la isla todavía no da para eso: vecinos, nivel o zona
    }

    /// <summary>Una fiesta que el jugador puede poner en el calendario.</summary>
    public readonly struct HostableEvent
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly long Cost;
        public readonly bool IsFestival;

        public HostableEvent(string id, string displayName, string description,
                             long cost, bool isFestival)
        {
            Id = id; DisplayName = displayName; Description = description;
            Cost = cost; IsFestival = isFestival;
        }
    }

    /// <summary>
    /// Poner una fiesta en el calendario de la aldea.
    /// </summary>
    /// <remarks>
    /// Los eventos ya se sorteaban solos; lo que faltaba era que el jugador pudiera
    /// **provocar uno**. Y eso importa más de lo que parece: en una fiesta los vecinos
    /// coinciden, y de ahí salen los flechazos.
    ///
    /// Es la mejor herramienta social que se le puede dar al jugador **sin romper la
    /// autonomía de nadie**: no empareja a dos personas, monta el sitio donde puedan
    /// conocerse. La piedra en el estanque de §3.3 — tú tiras la piedra, las ondas las
    /// hace el agua.
    /// </remarks>
    public interface IVillageEvents
    {
        /// <summary>Las que existen y se pueden pedir. Incluye las que aún no puedes pagar.</summary>
        IReadOnlyList<HostableEvent> Hostable { get; }

        HostRefusal CanHost(string eventId);

        /// <summary>La monta hoy mismo. Cobra las monedas.</summary>
        bool Host(string eventId);

        /// <summary>Lo que hay puesto ahora mismo, o vacío.</summary>
        string ActiveEventId { get; }

        /// <summary>La zona donde pasa lo que hay puesto ahora mismo, o vacío.</summary>
        /// <remarks>
        /// La necesita quien quiera acercar gente o mirar hacia allí, y hoy esa respuesta
        /// solo existe dentro de `Nimbo.Events` —es la `RequiredZone` de la definición—.
        /// Por contrato, como el resto de preguntas entre módulos.
        ///
        /// Puede venir vacía con una fiesta puesta: hay eventos que no piden zona. Quien
        /// la lea tiene que tratarlo, no dar por hecho que si hay fiesta hay sitio.
        /// </remarks>
        string ActiveEventZoneId { get; }
    }
}
