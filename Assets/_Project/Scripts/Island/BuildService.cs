using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Island.Zones;
using UnityEngine;

namespace Nimbo.Island
{
    /// <summary>
    /// Decide dónde va cada edificio de la aldea, y lo recuerda.
    /// </summary>
    /// <remarks>
    /// La posición de una zona estaba escrita en <c>IslandLayout</c> y era intocable.
    /// Ahora la manda el jugador, pero la tabla **no desaparece**: sigue siendo el
    /// sitio de salida de cada edificio. Así una partida vieja abre con la aldea tal y
    /// como estaba, y una nueva empieza con un pueblo montado en vez de un prado
    /// vacío donde los vecinos no tendrían adónde ir.
    ///
    /// La plaza y el Árbol Nimbo no se mueven. El árbol mide veintiséis metros y está
    /// en el centro; la plaza es por donde pasa todo el mundo y es lo que da sentido a
    /// las coordenadas de todos los demás.
    /// </remarks>
    public sealed class BuildService : IBuildService
    {
        /// <summary>Lo único que no se toca.</summary>
        public const string PlazaId = "zona_plaza";

        private readonly SaveGame _save;
        private readonly ZoneDefinition[] _zones;
        private readonly Dictionary<string, int> _index = new();
        private readonly List<string> _movable = new();

        public BuildService(SaveGame save)
        {
            _save = save;
            _zones = IslandLayout.FirstIsland();

            for (int i = 0; i < _zones.Length; i++)
            {
                _index[_zones[i].ZoneId] = i;
                if (!IsFixed(_zones[i].ZoneId)) _movable.Add(_zones[i].ZoneId);
            }
        }

        public IReadOnlyList<string> Movable => _movable;
        public IReadOnlyList<BuildingPlacement> Placed => _save.Island.Placements;

        public bool IsFixed(string zoneId) => zoneId == PlazaId;

        public bool TryGetPlacement(string zoneId, out BuildingPlacement placement)
        {
            var list = _save.Island.Placements;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ZoneId != zoneId) continue;
                placement = list[i];
                return true;
            }

            placement = null;
            return false;
        }

        /// <summary>
        /// Dónde está esa zona: donde la puso el jugador, o donde la dejó el diseño.
        /// </summary>
        /// <remarks>
        /// Este método es la pieza entera del cambio. Todo lo que necesitaba saber
        /// dónde estaba un edificio —las agendas, el mapa, el rodeo, quién vive dónde—
        /// pregunta por identificador, así que basta con que la respuesta salga de
        /// aquí para que la colocación libre no obligue a tocar ningún otro módulo.
        /// </remarks>
        public bool TryGetWorldCentre(string zoneId, out Vector3 centre)
        {
            if (!_index.TryGetValue(zoneId, out int i))
            {
                centre = Vector3.zero;
                return false;
            }

            centre = TryGetPlacement(zoneId, out var placement)
                ? BuildGrid.CentreOf(placement.CellX, placement.CellY)
                : _zones[i].Center;

            return true;
        }

        public BuildRejection CanPlace(string zoneId, int cellX, int cellY)
        {
            if (!_index.ContainsKey(zoneId)) return BuildRejection.UnknownZone;
            if (IsFixed(zoneId)) return BuildRejection.Fixed;

            if (!BuildGrid.InsideIsland(cellX, cellY)) return BuildRejection.OffIsland;
            if (BuildGrid.OnReservedCentre(cellX, cellY)) return BuildRejection.ReservedCentre;

            // Contra lo que ya está puesto y contra lo que sigue en su sitio de
            // fábrica: si solo se mirara lo colocado, el primer edificio que muevas
            // podría aterrizar encima de otro que nadie ha tocado todavía.
            foreach (var other in _index.Keys)
            {
                if (other == zoneId) continue;
                if (!TryGetCell(other, out int ox, out int oy)) continue;
                if (BuildGrid.Overlaps(cellX, cellY, ox, oy)) return BuildRejection.Occupied;
            }

            return BuildRejection.Ok;
        }

        public bool Place(string zoneId, int cellX, int cellY, int turns = 0)
        {
            if (CanPlace(zoneId, cellX, cellY) != BuildRejection.Ok) return false;

            if (TryGetPlacement(zoneId, out var placement))
            {
                placement.CellX = cellX;
                placement.CellY = cellY;
                placement.Turns = ((turns % 4) + 4) % 4;
            }
            else
            {
                _save.Island.Placements.Add(new BuildingPlacement
                {
                    ZoneId = zoneId,
                    CellX = cellX,
                    CellY = cellY,
                    Turns = ((turns % 4) + 4) % 4,
                });
            }

            ClearTheGround(cellX, cellY);

            EventBus.Publish(new BuildingMoved(zoneId));
            return true;
        }

        /// <summary>
        /// Aparta los árboles y las piedras del sitio donde acaba de caer el edificio.
        /// </summary>
        /// <remarks>
        /// La recolección siembra esquivando los edificios, pero los edificios se
        /// mueven después de sembrar: sin esto, colocar la panadería sobre una arboleda
        /// dejaba tres robles atravesando el tejado y no había forma de quitarlos —los
        /// nodos no se pueden talar desde dentro de una pared—.
        ///
        /// Se aparta en vez de rechazar la casilla. Con ciento veinte nodos repartidos
        /// por la corona donde se construye, exigir el suelo limpio habría dejado casi
        /// ninguna casilla libre y el modo construcción sería un no constante.
        /// </remarks>
        private static void ClearTheGround(int cellX, int cellY)
        {
            if (!ServiceRegistry.TryGet<IGatheringService>(out var gathering)) return;

            // El mismo radio con el que se siembra esquivando: la parcela de dos
            // casillas más el vuelo del alero.
            gathering.ClearAround(BuildGrid.CentreOf(cellX, cellY), 6f);
        }

        /// <summary>La casilla que ocupa una zona, esté colocada o en su sitio de fábrica.</summary>
        private bool TryGetCell(string zoneId, out int cellX, out int cellY)
        {
            if (TryGetPlacement(zoneId, out var placement))
            {
                cellX = placement.CellX;
                cellY = placement.CellY;
                return true;
            }

            if (!_index.TryGetValue(zoneId, out int i))
            {
                cellX = cellY = 0;
                return false;
            }

            BuildGrid.CellAt(_zones[i].Center, out cellX, out cellY);
            return true;
        }
    }
}
