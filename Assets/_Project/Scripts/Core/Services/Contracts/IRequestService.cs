using System.Collections.Generic;
using Nimbo.Data.Requests;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// La cola de peticiones: lo que los habitantes le piden al jugador y qué pasa
    /// cuando este contesta.
    /// </summary>
    public interface IRequestService
    {
        /// <summary>Las peticiones abiertas, de la más urgente a la menos.</summary>
        IReadOnlyList<IslanderRequest> Open { get; }

        int OpenCount { get; }

        bool TryGet(string requestId, out IslanderRequest request);

        IEnumerable<IslanderRequest> OpenFor(string islanderId);

        /// <summary>
        /// El jugador atiende la petición. <paramref name="payloadId"/> es lo que le
        /// da: id de objeto, de habitante o de zona, según el tipo de petición.
        /// </summary>
        bool Resolve(string requestId, string payloadId = null);

        /// <summary>El jugador dice que no. Tiene coste de ánimo, y por eso existe.</summary>
        void Refuse(string requestId);

        /// <summary>Fuerza una petición. La usan los eventos guionizados y el tutorial.</summary>
        IslanderRequest Raise(string islanderId, RequestKind kind, string targetId = null);
    }
}
