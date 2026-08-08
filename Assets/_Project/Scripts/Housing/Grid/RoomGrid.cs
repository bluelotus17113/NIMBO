using System.Collections.Generic;
using Nimbo.Data.Housing;

namespace Nimbo.Housing.Grid
{
    /// <summary>
    /// Ocupación de la rejilla por capas. Sabe qué casilla usa cada objeto colocado,
    /// qué celdas tienen soporte de superficie (Furniture con function table/storage/kitchen),
    /// y libera en O(1) cuando se quita un mueble.
    ///
    /// Se reconstruye desde <c>RoomLayout.Objects</c> + catálogo cada vez que se necesita
    /// (típicamente al abrir el editor). Después, Place y Remove lo mantienen al día.
    /// </summary>
    public class RoomGrid
    {
        readonly int _width;
        readonly int _height;
        readonly int _cellCount;

        // _cells[layer][cellIndex] = instanceId o null
        readonly Dictionary<PlacementLayer, string[]> _cells = new Dictionary<PlacementLayer, string[]>();
        // _support[cellIndex] = true si hay Furniture con function table/storage/kitchen
        readonly bool[] _support;
        // instanceId → lista de cellIndex que ocupa (para Remove rápido)
        readonly Dictionary<string, List<int>> _instanceCells = new Dictionary<string, List<int>>();

        public RoomGrid(int width, int height)
        {
            _width = width;
            _height = height;
            _cellCount = width * height;
            _support = new bool[_cellCount];
        }

        /// <summary>Construye la rejilla desde los objetos ya colocados y el catálogo.</summary>
        public static RoomGrid FromRoom(RoomLayout room, Catalog.FurnitureCatalog catalog)
        {
            var grid = new RoomGrid(room.Width, room.Height);
            foreach (var obj in room.Objects)
            {
                if (!catalog.TryGetFurniture(obj.CatalogId, out var entry)) continue;
                grid.Occupy(obj, entry);
            }
            return grid;
        }

        // ------------------------------------------------------------------- consultas

        public int CellCount => _cellCount;
        public int Width => _width;
        public int Height => _height;

        public int CellIndex(GridCoord c) => c.Y * _width + c.X;

        public bool InBounds(GridCoord c)
            => c.X >= 0 && c.Y >= 0 && c.X < _width && c.Y < _height;

        public bool IsCellFree(int cellIndex, PlacementLayer layer)
        {
            if (!_cells.TryGetValue(layer, out var layerCells)) return true;
            return layerCells[cellIndex] == null;
        }

        public bool HasSurfaceSupport(int cellIndex) => _support[cellIndex];

        public string OccupantAt(int cellIndex, PlacementLayer layer)
        {
            if (!_cells.TryGetValue(layer, out var layerCells)) return null;
            return layerCells[cellIndex];
        }

        // ------------------------------------------------------------------- mutación

        /// <summary>Marca las celdas que ocupa <c>obj</c> con su huella.</summary>
        public void Occupy(PlacedObject obj, Catalog.FurnitureEntry entry)
        {
            var indices = new List<int>();
            foreach (int ci in Footprint.GetCellIndices(
                entry.FootprintX, entry.FootprintY, obj.Origin, obj.Facing, _width))
            {
                indices.Add(ci);
                EnsureLayer(obj.Layer)[ci] = obj.InstanceId;
            }

            _instanceCells[obj.InstanceId] = indices;

            // Si es Furniture con function de soporte, marca esas celdas
            if (obj.Layer == PlacementLayer.Furniture && SupportsSurface(entry.Function))
            {
                foreach (int ci in indices) _support[ci] = true;
            }
        }

        /// <summary>Libera todas las celdas de un objeto por su instanceId.</summary>
        public void Free(string instanceId)
        {
            if (!_instanceCells.TryGetValue(instanceId, out var indices)) return;

            // Averiguar la capa: buscar en qué capa está
            foreach (var kv in _cells)
            {
                var layerCells = kv.Value;
                foreach (int ci in indices)
                {
                    if (layerCells[ci] == instanceId)
                    {
                        layerCells[ci] = null;
                        _support[ci] = false; // el soporte se va con el mueble
                    }
                }
            }

            _instanceCells.Remove(instanceId);
        }

        // ------------------------------------------------------------------- helpers

        string[] EnsureLayer(PlacementLayer layer)
        {
            if (!_cells.TryGetValue(layer, out var layerCells))
            {
                layerCells = new string[_cellCount];
                _cells[layer] = layerCells;
            }
            return layerCells;
        }

        static bool SupportsSurface(string function)
        {
            return function == "table" || function == "storage" || function == "kitchen";
        }
    }
}
