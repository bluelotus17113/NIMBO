using System;

namespace Nimbo.Data.Social
{
    /// <summary>
    /// Dos vecinos con un flechazo por la misma persona, y sabiéndolo.
    /// </summary>
    /// <remarks>
    /// Antes de esto podía pasar y no pasaba nada: los dos suspiraban por su lado sin
    /// enterarse el uno del otro, y la historia más jugosa que el sistema podía dar se
    /// tiraba a la basura.
    ///
    /// Se guarda con la partida porque dura días. Un triángulo que solo viviera en
    /// memoria se resolvería solo al cargar, que es la peor manera de resolverse: sin
    /// que nadie lo vea.
    ///
    /// Los dos pretendientes se guardan **ordenados alfabéticamente**, igual que las
    /// bodas: así el mismo triángulo tiene una sola forma de escribirse y no se puede
    /// abrir dos veces con los papeles cambiados.
    /// </remarks>
    [Serializable]
    public class LoveTriangle
    {
        /// <summary>Por quién compiten.</summary>
        public string BelovedId = "";

        /// <summary>Los dos pretendientes, en orden.</summary>
        public string AId = "";
        public string BId = "";

        public int StartedDay;

        public static LoveTriangle Between(string beloved, string one, string other, int day)
        {
            bool inOrder = string.CompareOrdinal(one, other) <= 0;

            return new LoveTriangle
            {
                BelovedId = beloved,
                AId = inOrder ? one : other,
                BId = inOrder ? other : one,
                StartedDay = day,
            };
        }

        /// <summary>¿Es este mismo triángulo?</summary>
        public bool Is(string beloved, string one, string other) =>
            BelovedId == beloved &&
            ((AId == one && BId == other) || (AId == other && BId == one));

        /// <summary>¿Anda este vecino metido, sea como sea?</summary>
        public bool Involves(string islanderId) =>
            islanderId == BelovedId || islanderId == AId || islanderId == BId;

        /// <summary>El otro pretendiente. Vacío si el que preguntas no es ninguno.</summary>
        public string RivalOf(string islanderId) =>
            islanderId == AId ? BId : islanderId == BId ? AId : "";
    }
}
