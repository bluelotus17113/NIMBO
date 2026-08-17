using System.Collections.Generic;
using Nimbo.Data.Requests;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Lo que cuesta atender una petición.
    /// </summary>
    /// <remarks>
    /// Hay tres formas de pedir y esta estructura las distingue:
    ///
    /// - **No cuesta nada** (<see cref="WantsAnItem"/> falso): un consejo, una queja,
    ///   una presentación. Lo que piden es tu rato, y el rato ya lo pagas yendo hasta
    ///   allí.
    /// - **Piden esto exacto, y tantas** (<see cref="CatalogId"/> con nombre): el
    ///   encargo de material. «Tráeme cinco maderas» solo se paga con cinco maderas.
    /// - **Piden algo de esta familia** (<see cref="CatalogId"/> vacío): comida, ropa,
    ///   un mueble. Vale cualquiera y **elige el jugador**, que es lo que hace que los
    ///   gustos de cada vecino sirvan para algo: darle lo que le encanta alegra más.
    ///
    /// Lo exacto se reserva al material a propósito. La ropa y los muebles se compran
    /// en tiendas cuyo surtido rota cada día, así que pedir una prenda concreta sería
    /// pedir algo que la mayoría de los días no se puede conseguir; el material lo
    /// sueltan los nodos de la isla y siempre hay dónde ir a por él.
    /// </remarks>
    public readonly struct RequestDemand
    {
        /// <summary>El objeto exacto, o vacío si vale cualquiera de <see cref="Category"/>.</summary>
        public readonly string CatalogId;

        /// <summary>La familia que acepta cuando no pide uno exacto.</summary>
        public readonly ItemCategory Category;

        /// <summary>Cuántas unidades. Siempre 1 salvo en los encargos de material.</summary>
        public readonly int Quantity;

        /// <summary>Falso cuando la petición no cuesta objetos.</summary>
        public readonly bool WantsAnItem;

        public RequestDemand(ItemCategory category, string catalogId, int quantity)
        {
            Category = category;
            CatalogId = catalogId ?? "";
            Quantity = quantity < 1 ? 1 : quantity;
            WantsAnItem = true;
        }

        /// <summary>Lo que devuelve una petición que solo pide tu tiempo.</summary>
        public static RequestDemand Nothing => default;
    }

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

        /// <summary>Qué hace falta para atenderla. <see cref="RequestDemand.Nothing"/> si nada.</summary>
        RequestDemand DemandOf(string requestId);

        /// <summary>
        /// Lo que el jugador puede darle ahora mismo, mirando la mochila y la despensa.
        /// </summary>
        /// <remarks>
        /// Vacía si no llega. Para un encargo de material trae el material pedido cuando
        /// lleva bastante, y nada cuando no: ahí no hay nada que elegir.
        ///
        /// Existe para que la pregunta «¿tengo con qué?» se conteste **en un solo
        /// sitio**. El jugador guarda cosas en dos sitios —lo que recoge va a la mochila
        /// y lo que compra a la despensa— y si la pantalla mirase por su cuenta acabaría
        /// enseñando un botón que el servicio rechaza.
        /// </remarks>
        IReadOnlyList<string> OptionsFor(string requestId);

        /// <summary>
        /// El jugador atiende la petición. <paramref name="payloadId"/> es lo que le
        /// da: uno de los que devuelve <see cref="OptionsFor"/>.
        /// </summary>
        /// <remarks>
        /// Falso y sin tocar nada si pide un objeto y no se le da uno que valga. Es la
        /// diferencia entre una petición y un botón: antes esto repartía ánimo y
        /// experiencia por un clic.
        /// </remarks>
        bool Resolve(string requestId, string payloadId = null);

        /// <summary>El jugador dice que no. Tiene coste de ánimo, y por eso existe.</summary>
        void Refuse(string requestId);

        /// <summary>Fuerza una petición. La usan los eventos guionizados y el tutorial.</summary>
        IslanderRequest Raise(string islanderId, RequestKind kind, string targetId = null);
    }
}
