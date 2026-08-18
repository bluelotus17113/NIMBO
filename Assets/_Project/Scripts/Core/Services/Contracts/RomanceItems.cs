namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Las dos cosas que hay que fabricar para que el romance del protagonista avance.
    /// </summary>
    /// <remarks>
    /// Viven aquí y no en el módulo social porque los preguntan los dos extremos: el
    /// cortejo, que los gasta, y la pantalla de crafteo, que decide qué recetas se
    /// enseñan. Escritos dos veces, un cambio de identificador dejaría el anillo
    /// fabricable y sin servir para nada, sin que fallara nada.
    ///
    /// Son objetos y no un contador a propósito: **atan el romance a la isla**. El ramo
    /// pide flores recogidas a mano y el anillo pide el corazón de tres geodas, que es
    /// el nodo que más tarda en reponerse. Sin ellos el cortejo sería una conversación
    /// con uno mismo.
    /// </remarks>
    public static class RomanceItems
    {
        /// <summary>Lo que cuesta declararse (§14.2).</summary>
        public const string Bouquet = "gift_ramo";

        /// <summary>Lo que cuesta pedir la mano (§14.5).</summary>
        public const string Ring = "gift_anillo";
    }
}
