using System;

namespace Nimbo.Data.Social
{
    /// <summary>
    /// Una pareja en el tramo que va del compromiso a la familia.
    /// </summary>
    /// <remarks>
    /// Es una clase y no un struct porque el planificador la modifica en el sitio
    /// mientras recorre la lista. Con un struct habría que sacarla, tocarla y volver a
    /// meterla por índice, que es el patrón del que salen los fallos de «se me ha
    /// olvidado guardar la copia».
    ///
    /// Los dos identificadores se guardan **ordenados** (<see cref="Between"/>) para
    /// que una pareja no pueda tener dos entradas por haberla mirado desde cada lado.
    /// </remarks>
    [Serializable]
    public class WeddingBooking
    {
        public string AId = "";
        public string BId = "";

        /// <summary>Día de juego en que se les vio prometidos por primera vez.</summary>
        public int EngagedSinceDay;

        /// <summary>Día de la boda. Cero mientras no haya fecha puesta.</summary>
        public int WeddingDay;

        /// <summary>Día en que se casaron. Cero mientras no lo estén.</summary>
        public int MarriedOnDay;

        public bool HasDate => WeddingDay > 0;
        public bool IsMarried => MarriedOnDay > 0;

        /// <summary>
        /// La entrada de esa pareja, con los identificadores en orden alfabético para
        /// que dé igual en qué orden lleguen.
        /// </summary>
        public static WeddingBooking Between(string one, string other, int day)
        {
            bool inOrder = string.CompareOrdinal(one, other) <= 0;
            return new WeddingBooking
            {
                AId = inOrder ? one : other,
                BId = inOrder ? other : one,
                EngagedSinceDay = day,
            };
        }

        /// <summary>¿Es esta la entrada de esa pareja, mirada desde cualquier lado?</summary>
        public bool Is(string one, string other) =>
            (AId == one && BId == other) || (AId == other && BId == one);
    }
}
