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

    /// <summary>
    /// Dónde está el huerto en el mundo y cuánto mide cada casilla.
    /// </summary>
    /// <remarks>
    /// Vive en Data y no en el servicio porque lo necesitan tres sitios que no se ven
    /// entre sí: quien lo dibuja, quien decide sobre qué casilla estás y el propio
    /// servicio. Con el número copiado en tres ficheros, el día que se mueva la
    /// parcela el jugador labraría una casilla y se pondría verde otra.
    ///
    /// El sitio está elegido a mano: en tu isla, al oeste de la cabaña y con sitio de
    /// sobra alrededor.
    /// </remarks>
    public static class FarmPlot
    {
        public const float TileSize = 1.6f;

        // En tu isla, al oeste de la cabaña. Antes estaba en la de la aldea, pegado a
        // la plaza; con las dos islas separadas, el huerto es de tu casa y punto.
        public const float CentreX = -10f;
        public const float CentreZ = -168f;

        /// <summary>La esquina (0,0) de la parcela, en coordenadas del mundo.</summary>
        public static void Origin(int width, int height, out float x, out float z)
        {
            x = CentreX - width * TileSize * 0.5f;
            z = CentreZ - height * TileSize * 0.5f;
        }

        /// <summary>El centro de esa casilla, en coordenadas del mundo.</summary>
        public static void CentreOf(int tileX, int tileY, int width, int height,
                                    out float x, out float z)
        {
            Origin(width, height, out float ox, out float oz);
            x = ox + (tileX + 0.5f) * TileSize;
            z = oz + (tileY + 0.5f) * TileSize;
        }

        /// <summary>
        /// Sobre qué casilla cae ese punto del mundo. Falso si está fuera.
        /// </summary>
        public static bool TileAt(float worldX, float worldZ, int width, int height,
                                  out int tileX, out int tileY)
        {
            Origin(width, height, out float ox, out float oz);

            tileX = (int)System.Math.Floor((worldX - ox) / TileSize);
            tileY = (int)System.Math.Floor((worldZ - oz) / TileSize);

            return tileX >= 0 && tileY >= 0 && tileX < width && tileY < height;
        }

        /// <summary>
        /// Cuánta parcela se puede trabajar con ese nivel de Cultivo.
        /// </summary>
        /// <remarks>
        /// La tierra está toda ahí desde el primer día —ocho por seis— y lo que crece
        /// es cuánta se sabe trabajar. Se roturan las del medio hacia fuera, así que la
        /// parcela se ensancha alrededor de lo que ya tienes plantado en vez de
        /// aparecer una franja suelta en una esquina.
        ///
        /// Lo que decide es el nivel y no un sí o un no, porque son tres escalones:
        /// una fila al 2, una fila y una columna al 4, y el huerto entero al 8.
        /// </remarks>
        public static void UsableSize(int farmingLevel, out int width, out int height)
        {
            if (farmingLevel >= 8) { width = 8; height = 6; return; }
            if (farmingLevel >= 4) { width = 6; height = 5; return; }
            if (farmingLevel >= 2) { width = 4; height = 4; return; }

            width = 4; height = 3;
        }

        /// <summary>¿Se puede trabajar esa casilla con ese nivel de Cultivo?</summary>
        public static bool IsUsable(int tileX, int tileY, int farmingLevel,
                                    int width = 8, int height = 6)
        {
            UsableSize(farmingLevel, out int usableW, out int usableH);

            // Centrado, y con la mitad sobrante hacia el mismo lado siempre: si el
            // redondeo cambiara de lado al crecer, una casilla ya labrada podría quedar
            // fuera al subir de nivel, que es lo contrario de lo que promete subir.
            int x0 = (width - usableW) / 2;
            int y0 = (height - usableH) / 2;

            return tileX >= x0 && tileX < x0 + usableW
                && tileY >= y0 && tileY < y0 + usableH;
        }
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
