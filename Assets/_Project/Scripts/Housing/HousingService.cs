using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;
using Nimbo.Housing.Catalog;
using Nimbo.Housing.Grid;

namespace Nimbo.Housing
{
    /// <summary>
    /// Implementación de <see cref="IHousingService"/>: editor de interiores tipo
    /// Los Sims con reglas de rejilla, capas y rotación.
    /// </summary>
    public class HousingService : IHousingService
    {
        readonly FurnitureCatalog _catalog;
        readonly Dictionary<RoomLayout, RoomGrid> _grids = new Dictionary<RoomLayout, RoomGrid>();
        int _nextInstanceId;

        public HousingService() : this(new FurnitureCatalog()) { }

        /// <summary>Constructor para tests con catálogo ya cargado.</summary>
        public HousingService(FurnitureCatalog catalog)
        {
            _catalog = catalog;
        }

        // ------------------------------------------------------------------- IHousingService

        public RoomLayout GetLayout(string buildingId, int unitIndex)
        {
            // La asignación edificio → apartamento la resuelve el módulo Island.
            // Por ahora devolvemos null; la habitación se crea bajo demanda en el editor.
            return null;
        }

        public RoomLayout GetHomeOf(string islanderId)
        {
            // Ídem: la resuelve Island/CharacterCreator.
            return null;
        }

        public PlacementError CanPlace(RoomLayout room, string catalogId, GridCoord origin, Facing facing)
        {
            if (room == null) return PlacementError.OutOfBounds;
            if (!_catalog.TryGetFurniture(catalogId, out var entry))
                return PlacementError.UnknownCatalogId;

            var grid = GetOrBuildGrid(room);
            return PlacementRules.Check(room, entry, origin, facing, grid);
        }

        public PlacementError Place(RoomLayout room, string catalogId, GridCoord origin, Facing facing,
                                    int colorVariant = 0)
        {
            if (room == null) return PlacementError.OutOfBounds;
            if (!_catalog.TryGetFurniture(catalogId, out var entry))
                return PlacementError.UnknownCatalogId;

            var grid = GetOrBuildGrid(room);
            var error = PlacementRules.Check(room, entry, origin, facing, grid);
            if (error != PlacementError.None) return error;

            // Todo ok: colocar
            var obj = new PlacedObject
            {
                InstanceId = NextId(catalogId),
                CatalogId = catalogId,
                Origin = origin,
                Facing = facing,
                Layer = entry.Layer,
                ColorVariant = colorVariant,
            };

            room.Objects.Add(obj);
            grid.Occupy(obj, entry);
            return PlacementError.None;
        }

        public bool Remove(RoomLayout room, string instanceId)
        {
            if (room == null) return false;

            for (int i = 0; i < room.Objects.Count; i++)
            {
                if (room.Objects[i].InstanceId == instanceId)
                {
                    room.Objects.RemoveAt(i);
                    if (_grids.TryGetValue(room, out var grid))
                        grid.Free(instanceId);
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<GridCoord> FootprintOf(string catalogId, GridCoord origin, Facing facing)
        {
            if (!_catalog.TryGetFurniture(catalogId, out var entry))
                yield break;

            foreach (var c in Footprint.GetCoords(entry.FootprintX, entry.FootprintY, origin, facing))
                yield return c;
        }

        public void SetFloor(RoomLayout room, GridCoord coord, string tileId)
        {
            if (room == null || !room.InBounds(coord)) return;
            if (!_catalog.TryGetFinish(tileId, out var finish)) return;
            if (finish.Surface != "floor") return;

            int i = room.CellIndex(coord);
            while (room.FloorTiles.Count <= i) room.FloorTiles.Add(null);
            room.FloorTiles[i] = tileId;
        }

        public void SetWallpaper(RoomLayout room, bool north, string wallpaperId)
        {
            if (room == null) return;
            if (!_catalog.TryGetFinish(wallpaperId, out var finish)) return;
            if (finish.Surface != "wall") return;

            if (north) room.WallpaperNorth = wallpaperId;
            else       room.WallpaperWest = wallpaperId;
        }

        /// <summary>
        /// Índice de comodidad del apartamento, de 0 (vacío) a 100 (lujo absoluto).
        ///
        /// Fórmula:
        ///   base = suma de todos los <c>needBonus</c> (hunger+energy+social+hygiene)
        ///          de los muebles colocados, con tope en 80.
        ///   decor = +2 por cada objeto con <c>function == "decor"</c>, con tope en 20.
        ///   score = base + decor, clamp a [0, 100].
        ///
        /// Un buen sofá con social +3 y energy +5 aporta 8 a base. Una habitación con
        /// cama, ducha, nevera y varios adornos ronda los 60-80. Para llegar a 100 hace
        /// falta una habitación grande, bien equipada y decorada con gusto.
        /// </summary>
        public float ComfortScore(RoomLayout room)
        {
            if (room == null) return 0f;

            float baseScore = 0f;
            int decorCount = 0;

            foreach (var obj in room.Objects)
            {
                if (!_catalog.TryGetFurniture(obj.CatalogId, out var entry)) continue;

                baseScore += entry.TotalNeedBonus();
                if (entry.Function == "decor") decorCount++;
            }

            baseScore = UnityEngine.Mathf.Min(baseScore, 80f);
            float decorScore = UnityEngine.Mathf.Min(decorCount * 2f, 20f);

            return UnityEngine.Mathf.Clamp(baseScore + decorScore, 0f, 100f);
        }

        // ------------------------------------------------------------------- internals

        RoomGrid GetOrBuildGrid(RoomLayout room)
        {
            if (!_grids.TryGetValue(room, out var grid))
            {
                grid = RoomGrid.FromRoom(room, _catalog);
                _grids[room] = grid;
            }
            return grid;
        }

        string NextId(string catalogId)
        {
            _nextInstanceId++;
            return $"{catalogId}#{_nextInstanceId}";
        }
    }
}
