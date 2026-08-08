using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Data.Housing
{
    /// <summary>Casilla de la rejilla del apartamento. Enteros: nada de posiciones libres.</summary>
    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int X;
        public int Y;

        public GridCoord(int x, int y) { X = x; Y = y; }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord c && Equals(c);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X},{Y})";

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    /// En qué plano vive un objeto. Dos objetos de capas distintas pueden compartir
    /// casilla; dos de la misma, no. Es toda la regla de colisión del editor.
    /// </summary>
    public enum PlacementLayer
    {
        Floor = 0,      // el suelo mismo, no se coloca: se pinta
        Rug = 1,
        Furniture = 2,  // lo que se apoya en el suelo
        Surface = 3,    // lo que va encima de un mueble
        WallMounted = 4,
        Ceiling = 5,
    }

    public enum Facing { North = 0, East = 1, South = 2, West = 3 }

    /// <summary>Un mueble colocado. El catálogo dice su tamaño; aquí solo va dónde y cómo.</summary>
    [Serializable]
    public struct PlacedObject
    {
        public string InstanceId;
        public string CatalogId;
        public GridCoord Origin;
        public Facing Facing;
        public PlacementLayer Layer;
        public int ColorVariant;
    }

    /// <summary>
    /// El interior de un apartamento: rejilla, acabados y todo lo colocado dentro.
    /// </summary>
    [Serializable]
    public class RoomLayout
    {
        public const int MinSize = 6;
        public const int MaxSize = 16;

        public int Width = 8;
        public int Height = 8;

        /// <summary>Un id de acabado por casilla, en orden fila a fila (índice = y * Width + x).</summary>
        public List<string> FloorTiles = new List<string>();

        /// <summary>Acabado de las paredes norte y oeste, las dos que se ven en cámara.</summary>
        public string WallpaperNorth = "wall_liso_crema";
        public string WallpaperWest = "wall_liso_crema";

        public List<PlacedObject> Objects = new List<PlacedObject>();

        public bool InBounds(GridCoord c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

        public int CellIndex(GridCoord c) => c.Y * Width + c.X;

        public string FloorAt(GridCoord c)
        {
            if (!InBounds(c)) return null;
            int i = CellIndex(c);
            return i < FloorTiles.Count ? FloorTiles[i] : null;
        }

        /// <summary>Rellena el suelo entero con un acabado. También sirve para estrenar la lista.</summary>
        public void FillFloor(string tileId)
        {
            int cells = Width * Height;
            FloorTiles.Clear();
            FloorTiles.Capacity = cells;
            for (int i = 0; i < cells; i++) FloorTiles.Add(tileId);
        }

        public static RoomLayout Starter()
        {
            var room = new RoomLayout { Width = 8, Height = 8 };
            room.FillFloor("floor_madera_clara");
            return room;
        }
    }
}
