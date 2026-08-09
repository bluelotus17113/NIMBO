using System.Collections.Generic;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Qué clase de adorno es. Decide dónde se puede poner y cómo se dibuja.</summary>
    public enum DecorKind
    {
        Seat = 0,      // bancos, sillas, hamacas
        Light = 1,     // farolas, farolillos
        Statue = 2,    // estatuas y monumentos
        Plant = 3,     // arbustos, macetas, arbolitos
        Sign = 4,      // carteles y buzones
        Fence = 5,     // vallas y setos, se ponen en fila
        Water = 6,     // fuentes y estanques
    }

    /// <summary>Un adorno del catálogo. Es una ficha muerta: no sabe colocarse.</summary>
    public readonly struct DecorDefinition
    {
        public readonly string CatalogId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly DecorKind Kind;
        public readonly long Price;
        public readonly int UnlockLevel;

        /// <summary>Radio que ocupa en el suelo, en metros. Dos adornos no se solapan.</summary>
        public readonly float Footprint;

        /// <summary>Cuánto anima la zona donde está, de 0 a 1. Suma al ánimo de quien pasa.</summary>
        public readonly float Charm;

        public DecorDefinition(string catalogId, string displayName, string description,
                               DecorKind kind, long price, int unlockLevel,
                               float footprint, float charm)
        {
            CatalogId = catalogId;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
            Price = price;
            UnlockLevel = unlockLevel;
            Footprint = footprint;
            Charm = charm;
        }
    }

    /// <summary>Por qué no se ha podido poner un adorno ahí. El aviso se le enseña al jugador.</summary>
    public enum DecorRejection
    {
        Ok = 0,
        UnknownItem,
        ZoneLocked,
        OutsideZone,
        Overlaps,
        TooMany,
        NotOwned,
    }

    /// <summary>
    /// Los adornos de la isla: bancos, farolas, estatuas y demás.
    /// </summary>
    /// <remarks>
    /// Es deliberadamente parecido a <c>IHousingService</c> pero no es lo mismo: allí
    /// se decora una habitación sobre una rejilla cerrada, y aquí se decora el aire
    /// libre, donde no hay rejilla y sí zonas que pueden estar cerradas. Compartir el
    /// servicio habría obligado a llenar el de vivienda de casos de «si es exterior…».
    ///
    /// El servicio decide y guarda; no dibuja nada. Publica <c>DecorPlaced</c> y
    /// <c>DecorRemoved</c>, y quien pinta la isla se entera por ahí.
    /// </remarks>
    public interface IDecorService
    {
        /// <summary>Todo el catálogo, incluido lo que aún no se puede comprar.</summary>
        IReadOnlyList<DecorDefinition> Catalog { get; }

        bool TryGetDefinition(string catalogId, out DecorDefinition definition);

        /// <summary>Lo que hay puesto ahora mismo en toda la isla.</summary>
        IReadOnlyList<DecorPlacement> Placed { get; }

        /// <summary>Lo que hay puesto en una zona concreta.</summary>
        IEnumerable<DecorPlacement> InZone(string zoneId);

        /// <summary>Cuántos adornos caben en una zona. Pasado ese número se rechaza.</summary>
        int CapacityPerZone { get; }

        /// <summary>
        /// ¿Cabe ahí? No cambia nada; sirve para pintar la vista previa en verde o
        /// en rojo mientras el jugador arrastra, sin tener que intentar colocarlo.
        /// </summary>
        DecorRejection CanPlace(string catalogId, string zoneId, Vector3 localPosition);

        /// <summary>
        /// Pone el adorno y devuelve su identificador. Cadena vacía si no se pudo:
        /// el motivo se pregunta con <see cref="CanPlace"/>.
        /// </summary>
        string Place(string catalogId, string zoneId, Vector3 localPosition, float yaw);

        bool Move(string placementId, Vector3 localPosition, float yaw);

        /// <summary>Lo quita y lo devuelve al inventario. No reembolsa: eso es de economía.</summary>
        bool Remove(string placementId);

        /// <summary>
        /// Encanto total de una zona: la suma de sus adornos, con tope. Lo lee la
        /// simulación para dar un pellizco de ánimo a quien pasa el rato allí.
        /// </summary>
        float CharmOf(string zoneId);
    }
}
