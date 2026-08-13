using UnityEngine;

namespace Nimbo.UI
{
    /// <summary>
    /// Reparte la tecla Escape entre las dos capas que la escuchan: la del juego
    /// —<c>UiRoot</c>— y la que va por encima —<c>MainMenuView</c>—.
    /// </summary>
    /// <remarks>
    /// Cada una atiende Escape solo cuando manda ella, y eso basta casi siempre: con
    /// el menú abierto manda la del juego, con la pausa delante manda la de arriba.
    /// Lo que no basta es el fotograma en el que el estado cambia. Los dos
    /// <c>Update</c> corren en un orden que Unity no promete, así que si la de abajo
    /// pausa y la de arriba corre después en ese mismo fotograma, la de arriba ve la
    /// tecla todavía pulsada, ve que ahora manda ella y quita la pausa que acababa de
    /// ponerse. La pausa se abría y se cerraba sin que se llegara a ver.
    ///
    /// Con esto, la primera que pregunta se la queda y la segunda se encuentra un no.
    /// </remarks>
    internal static class EscapeGuard
    {
        private static int _frame = -1;

        /// <summary>
        /// Pide la tecla para este fotograma. Cierto si es tuya; falso si otra capa se
        /// te ha adelantado.
        /// </summary>
        public static bool Take()
        {
            if (_frame == Time.frameCount) return false;

            _frame = Time.frameCount;
            return true;
        }
    }
}
