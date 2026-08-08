using System.Collections.Generic;
using Nimbo.Data.Housing;

namespace Nimbo.Housing.Grid
{
    /// <summary>
    /// Qué casillas ocupa un mueble según su tamaño y hacia dónde mira.
    /// Sin estado, sin reservas: yield return para que la previsualización
    /// no pague asignaciones por frame.
    /// </summary>
    public static class Footprint
    {
        /// <summary>
        /// Devuelve las coordenadas que ocuparía un mueble de <c>footX × footY</c>
        /// colocado en <c>origin</c> mirando hacia <c>facing</c>.
        /// Al girar 90° o 270° las dimensiones se intercambian.
        /// </summary>
        public static IEnumerable<GridCoord> GetCoords(int footX, int footY, GridCoord origin, Facing facing)
        {
            int dx = footX;
            int dy = footY;
            if (facing == Facing.East || facing == Facing.West)
            {
                dx = footY;
                dy = footX;
            }

            for (int y = 0; y < dy; y++)
            for (int x = 0; x < dx; x++)
            {
                yield return new GridCoord(origin.X + x, origin.Y + y);
            }
        }

        /// <summary>Índices lineales (cellIndex = y * width + x) para búsquedas rápidas en array plano.</summary>
        public static IEnumerable<int> GetCellIndices(int footX, int footY, GridCoord origin, Facing facing, int width)
        {
            int dx = footX;
            int dy = footY;
            if (facing == Facing.East || facing == Facing.West)
            {
                dx = footY;
                dy = footX;
            }

            for (int y = 0; y < dy; y++)
            for (int x = 0; x < dx; x++)
            {
                yield return (origin.Y + y) * width + (origin.X + x);
            }
        }
    }
}
