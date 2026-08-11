using System.Collections.Generic;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se puede poner un edificio en esa casilla.</summary>
    public enum BuildRejection
    {
        Ok = 0,
        UnknownZone,
        OffIsland,      // se sale de la isla de la aldea
        ReservedCentre, // la plaza y el Árbol no se tocan
        Occupied,       // ya hay otro edificio ahí
        NotUnlocked,    // ese edificio todavía no está abierto
        Fixed,          // ese no se mueve
    }

    /// <summary>
    /// Colocar y mover los edificios de la aldea sobre la rejilla.
    /// </summary>
    /// <remarks>
    /// Antes la posición de cada zona estaba escrita en una tabla y era intocable.
    /// Ahora la decide el jugador, y **lo importante es que nadie más se entera**: las
    /// agendas de los vecinos, el rodeo de edificios y el mapa piden «dónde está la
    /// tienda de comida», no unas coordenadas. Por eso el cambio cabe aquí en vez de
    /// obligar a tocar media docena de módulos.
    ///
    /// La plaza y el Árbol Nimbo no se mueven: están en el centro, el árbol mide
    /// veintiséis metros y la plaza es por donde pasa todo el mundo.
    /// </remarks>
    public interface IBuildService
    {
        /// <summary>Las zonas que se pueden colocar, estén puestas o no.</summary>
        IReadOnlyList<string> Movable { get; }

        /// <summary>Dónde está puesta esa zona. Falso si no lo está.</summary>
        bool TryGetPlacement(string zoneId, out BuildingPlacement placement);

        /// <summary>Todo lo puesto ahora mismo.</summary>
        IReadOnlyList<BuildingPlacement> Placed { get; }

        /// <summary>No cambia nada; sirve para pintar el fantasma en verde o en rojo.</summary>
        BuildRejection CanPlace(string zoneId, int cellX, int cellY);

        /// <summary>
        /// Pone o mueve la zona a esa casilla. Publica <c>BuildingMoved</c> al lograrlo.
        /// </summary>
        bool Place(string zoneId, int cellX, int cellY, int turns = 0);

        /// <summary>Cierto si esa zona está fija y no se puede mover.</summary>
        bool IsFixed(string zoneId);

        /// <summary>El punto del mundo donde está el centro de esa zona.</summary>
        bool TryGetWorldCentre(string zoneId, out Vector3 centre);
    }
}
