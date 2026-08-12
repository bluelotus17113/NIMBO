using System.Collections.Generic;
using Nimbo.Data.Housing;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se puede poner un mueble donde el jugador acaba de soltarlo.</summary>
    public enum PlacementError
    {
        None = 0,
        OutOfBounds = 1,
        Occupied = 2,
        NeedsWall = 3,
        NeedsFloor = 4,
        NeedsSurface = 5,
        NotOwned = 6,
        UnknownCatalogId = 7,
    }

    /// <summary>
    /// Los apartamentos y su interior. Reglas de colocación tipo Los Sims: rejilla,
    /// capas y rotación.
    /// </summary>
    public interface IHousingService
    {
        RoomLayout GetLayout(string buildingId, int unitIndex);

        /// <summary>La casa de ese habitante, o null si aún no tiene.</summary>
        RoomLayout GetHomeOf(string islanderId);

        /// <summary>
        /// ¿Es un mueble que se puede colocar en una habitación?
        /// </summary>
        /// <remarks>
        /// Hace falta para no ofrecer lo que el clic va a rechazar. El menú viejo
        /// listaba también papeles pintados y suelos —están en el inventario y son de
        /// una categoría «colocable»—, pero eso no son objetos: son acabados, no
        /// aparecen en el catálogo de muebles, y elegirlos solo servía para leer «ese
        /// mueble no existe» después de haber apuntado a una casilla.
        /// </remarks>
        bool IsPlaceable(string catalogId);

        /// <summary>Comprueba sin colocar. La interfaz la llama en cada movimiento del ratón.</summary>
        PlacementError CanPlace(RoomLayout room, string catalogId, GridCoord origin, Facing facing);

        /// <summary>Coloca de verdad. Devuelve el error si no pudo, y no toca nada.</summary>
        PlacementError Place(RoomLayout room, string catalogId, GridCoord origin, Facing facing,
                             int colorVariant = 0);

        bool Remove(RoomLayout room, string instanceId);

        /// <summary>Las casillas que ocupa un objeto del catálogo mirando hacia ahí.</summary>
        IEnumerable<GridCoord> FootprintOf(string catalogId, GridCoord origin, Facing facing);

        void SetFloor(RoomLayout room, GridCoord coord, string tileId);
        void SetWallpaper(RoomLayout room, bool north, string wallpaperId);

        /// <summary>Lo que aporta el interior a una necesidad. Un buen sofá se nota.</summary>
        float ComfortScore(RoomLayout room);
    }
}
