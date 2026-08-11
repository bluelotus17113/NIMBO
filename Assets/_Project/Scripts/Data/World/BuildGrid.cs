using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Data.World
{
    /// <summary>
    /// Dónde ha puesto el jugador un edificio de la aldea.
    /// </summary>
    /// <remarks>
    /// Dato muerto y dentro del guardado. Se guarda la **casilla**, no los metros: si
    /// mañana se cambia el tamaño de casilla, todo se recoloca solo en la rejilla
    /// nueva en vez de quedarse a medio camino entre dos.
    /// </remarks>
    [Serializable]
    public class BuildingPlacement
    {
        public string ZoneId = "";
        public int CellX;
        public int CellY;

        /// <summary>Giro en pasos de 90°, de 0 a 3. Los edificios no van torcidos.</summary>
        public int Turns;
    }

    /// <summary>
    /// La rejilla sobre la que se construye la aldea.
    /// </summary>
    /// <remarks>
    /// Cuatro metros de casilla: un edificio ocupa dos por dos, que a la altura a la
    /// que se juega es una manzana reconocible. Con casillas de uno, colocar sería
    /// puntería fina sobre una isla de doscientos metros; con casillas de diez, no
    /// habría decisión que tomar.
    ///
    /// La rejilla cubre la isla de la aldea entera y va centrada en su centro, así que
    /// la casilla (0,0) es la del medio y los índices son negativos hacia el oeste y
    /// el sur. Poner el origen en una esquina habría obligado a sumar el mismo
    /// desplazamiento en cada sitio que la use.
    /// </remarks>
    public static class BuildGrid
    {
        public const float CellSize = 4f;

        /// <summary>Casillas de lado que ocupa un edificio.</summary>
        public const int BuildingCells = 2;

        /// <summary>Casilla más lejana en cada dirección desde el centro.</summary>
        public static int MaxCell => Mathf.FloorToInt(Archipelago.VillageRadius / CellSize);

        /// <summary>El centro de esa casilla, en coordenadas del mundo.</summary>
        public static Vector3 CentreOf(int cellX, int cellY)
        {
            // El +0.5 pone el punto en el medio de la casilla y no en su esquina; sin
            // él, un edificio de dos por dos quedaría descuadrado media casilla.
            return new Vector3(
                Archipelago.VillageCentre.x + (cellX + 0.5f) * CellSize,
                0f,
                Archipelago.VillageCentre.z + (cellY + 0.5f) * CellSize);
        }

        /// <summary>Sobre qué casilla cae ese punto del mundo.</summary>
        public static void CellAt(Vector3 world, out int cellX, out int cellY)
        {
            cellX = Mathf.FloorToInt((world.x - Archipelago.VillageCentre.x) / CellSize);
            cellY = Mathf.FloorToInt((world.z - Archipelago.VillageCentre.z) / CellSize);
        }

        /// <summary>
        /// ¿Cabe ahí un edificio? Solo mira la rejilla y la isla, no lo que hay puesto.
        /// </summary>
        /// <remarks>
        /// Se comprueban las cuatro esquinas del edificio contra el radio útil de la
        /// isla. Con el centro solo, un edificio en el borde asomaba media manzana
        /// sobre el vacío.
        /// </remarks>
        public static bool InsideIsland(int cellX, int cellY)
        {
            // 0,84 del radio: el borde del prado es irregular y baja hasta el 0,86, así
            // que se deja un margen para no colocar sobre un mordisco del contorno.
            float usable = Archipelago.VillageRadius * 0.84f;

            for (int dx = 0; dx <= BuildingCells; dx++)
            for (int dy = 0; dy <= BuildingCells; dy++)
            {
                var corner = CentreOf(cellX + dx, cellY + dy) - new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f);
                float x = corner.x - Archipelago.VillageCentre.x;
                float z = corner.z - Archipelago.VillageCentre.z;
                if (x * x + z * z > usable * usable) return false;
            }

            return true;
        }

        /// <summary>
        /// La plaza y el Árbol Nimbo son intocables: están en el centro y ahí no se
        /// construye. El árbol mide veintiséis metros y la plaza es por donde pasa
        /// todo el mundo.
        /// </summary>
        public const float ReservedRadius = 20f;

        public static bool OnReservedCentre(int cellX, int cellY)
        {
            var centre = CentreOf(cellX, cellY);
            float x = centre.x - Archipelago.VillageCentre.x;
            float z = centre.z - Archipelago.VillageCentre.z;
            return x * x + z * z < ReservedRadius * ReservedRadius;
        }

        /// <summary>Dos edificios se pisan si sus cuadrados de casillas se solapan.</summary>
        public static bool Overlaps(int aX, int aY, int bX, int bY) =>
            Mathf.Abs(aX - bX) < BuildingCells && Mathf.Abs(aY - bY) < BuildingCells;

        /// <summary>Las casillas que ocupa un edificio puesto ahí.</summary>
        public static IEnumerable<Vector2Int> CellsOf(int cellX, int cellY)
        {
            for (int dx = 0; dx < BuildingCells; dx++)
            for (int dy = 0; dy < BuildingCells; dy++)
                yield return new Vector2Int(cellX + dx, cellY + dy);
        }
    }
}
