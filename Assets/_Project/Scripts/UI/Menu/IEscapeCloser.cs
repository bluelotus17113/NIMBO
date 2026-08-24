namespace Nimbo.UI.Menu
{
    /// <summary>
    /// Lo que ofrece la capa de juego cuando se le pregunta por Escape: cerrar
    /// una cosa de las que tenga abiertas.
    /// </summary>
    /// <remarks>
    /// Existe para que <c>MainMenuView</c> —que es quien lee la tecla— no conozca a
    /// <c>UiRoot</c> a pelo: los dos viven en Nimbo.UI, pero el monolito se toca lo
    /// mínimo y por contrato. Si nadie está registrado —el enganche aún sin
    /// aplicar— Escape abre la pausa directamente, que era el comportamiento de
    /// siempre y no se pierde mientras tanto.
    /// </remarks>
    public interface IEscapeCloser
    {
        /// <summary>
        /// Cierra UNA cosa: primero el modo activo si lo hay, después el panel
        /// pintado más arriba.
        /// </summary>
        /// <returns>False si no había nada que cerrar; entonces quien leyó la
        /// tecla decide qué hacer sin paneles, que hoy es abrir la pausa.</returns>
        bool CloseTopPanel();
    }
}
