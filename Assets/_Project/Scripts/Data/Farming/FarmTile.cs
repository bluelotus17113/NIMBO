using System;
using System.Collections.Generic;

namespace Nimbo.Data.Farming
{
    /// <summary>En qué estado está una casilla del huerto.</summary>
    public enum TileState
    {
        Wild = 0,     // hierba sin tocar
        Tilled = 1,   // labrada, lista para sembrar
        Planted = 2,  // con algo creciendo
        Ready = 3,    // se puede recoger
    }

    /// <summary>
    /// Una casilla del huerto. Dato muerto: no sabe crecer.
    /// </summary>
    /// <remarks>
    /// Va dentro del guardado, así que renombrar un campo rompe las partidas.
    /// </remarks>
    [Serializable]
    public class FarmTile
    {
        public int X;
        public int Y;
        public TileState State = TileState.Wild;

        /// <summary>Qué se sembró aquí. Vacío si no hay nada.</summary>
        public string SeedId = "";

        /// <summary>Días de riego acumulados. Es lo que hace crecer, no los días pasados.</summary>
        public int GrowthDays;

        /// <summary>Si está regada hoy. Se seca al pasar el día.</summary>
        public bool Watered;
    }

    /// <summary>El huerto entero, tal y como se guarda.</summary>
    [Serializable]
    public class FarmState
    {
        public int Width = 8;
        public int Height = 6;
        public List<FarmTile> Tiles = new List<FarmTile>();
    }
}
