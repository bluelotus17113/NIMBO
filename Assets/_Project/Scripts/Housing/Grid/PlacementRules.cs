using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;

namespace Nimbo.Housing.Grid
{
    /// <summary>
    /// Las reglas de colocación. Sin estado: recibe la rejilla, la habitación y la
    /// entrada del catálogo, y devuelve el error o <c>None</c>. Es barata: no reserva
    /// listas ni itera de más.
    /// </summary>
    public static class PlacementRules
    {
        /// <summary>
        /// Comprueba si <c>entry</c> cabe en <c>origin</c> con <c>facing</c>.
        /// El orden de chequeo es: catálogo → fuera → ocupado → pared → superficie.
        /// </summary>
        public static PlacementError Check(
            RoomLayout room,
            Catalog.FurnitureEntry entry,
            GridCoord origin,
            Facing facing,
            RoomGrid grid)
        {
            // 0. Sin entrada no hay nada que comprobar (ya validado fuera)
            if (entry == null) return PlacementError.UnknownCatalogId;

            // 1. Fuera de la rejilla
            int dx = entry.FootprintX;
            int dy = entry.FootprintY;
            if (facing == Facing.East || facing == Facing.West)
            {
                dx = entry.FootprintY;
                dy = entry.FootprintX;
            }

            if (origin.X < 0 || origin.Y < 0 ||
                origin.X + dx > room.Width || origin.Y + dy > room.Height)
                return PlacementError.OutOfBounds;

            // 2. Misma capa ocupada en alguna celda de la huella
            for (int y = 0; y < dy; y++)
            for (int x = 0; x < dx; x++)
            {
                int ci = (origin.Y + y) * room.Width + (origin.X + x);
                if (!grid.IsCellFree(ci, entry.Layer))
                    return PlacementError.Occupied;
            }

            // 3. WallMounted solo en pared norte (y == Height-1) u oeste (x == 0)
            if (entry.Layer == PlacementLayer.WallMounted)
            {
                bool onWall = false;
                for (int y = 0; y < dy; y++)
                for (int x = 0; x < dx; x++)
                {
                    int gx = origin.X + x;
                    int gy = origin.Y + y;
                    if (gx == 0 || gy == room.Height - 1)
                    {
                        onWall = true;
                        break;
                    }
                }
                if (!onWall) return PlacementError.NeedsWall;
            }

            // 4. Surface necesita que TODAS sus casillas tengan Furniture debajo
            //    con function table, storage o kitchen
            if (entry.Layer == PlacementLayer.Surface)
            {
                for (int y = 0; y < dy; y++)
                for (int x = 0; x < dx; x++)
                {
                    int ci = (origin.Y + y) * room.Width + (origin.X + x);
                    if (!grid.HasSurfaceSupport(ci))
                        return PlacementError.NeedsSurface;
                }
            }

            return PlacementError.None;
        }
    }
}
