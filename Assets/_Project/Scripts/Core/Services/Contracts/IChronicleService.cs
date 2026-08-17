using System.Collections.Generic;
using Nimbo.Data.World;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// La crónica de la aldea: lo que ha ido pasando, para poder leerlo.
    /// </summary>
    /// <remarks>
    /// Existe porque los avisos de la vida social —quién empezó a salir con quién, quién
    /// dejó de hablarse, que hay boda el jueves— se publicaban en el <c>EventBus</c> y
    /// **no los escuchaba nadie**. La aldea vivía entera y el jugador no tenía por dónde
    /// enterarse.
    ///
    /// Es de solo lectura a propósito: quien lo pinta no escribe en él. Las líneas las
    /// pone el tablón al oír los avisos, y así no hay dos sitios desde los que se cuenta
    /// la historia de la isla.
    /// </remarks>
    public interface IChronicleService
    {
        /// <summary>Todas las líneas, de la más vieja a la más reciente.</summary>
        IReadOnlyList<ChronicleEntry> Entries { get; }
    }
}
