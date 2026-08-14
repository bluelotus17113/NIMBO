namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Qué pasó al ofrecer algo.</summary>
    public enum GiftResult
    {
        /// <summary>Se lo ha quedado. <c>Opinion</c> dice si le ha gustado.</summary>
        Ok = 0,

        /// <summary>No llevabas nada en la mano.</summary>
        EmptyHanded,

        /// <summary>Eso no se regala: una herramienta, un material a medio usar.</summary>
        NotGiftable,

        /// <summary>Ya le has dado algo hoy.</summary>
        AlreadyToday,

        UnknownIslander,
    }

    /// <summary>
    /// Darle algo de la mochila a un vecino.
    /// </summary>
    /// <remarks>
    /// Existe aparte del armario porque el armario solo entiende de ropa y saca de la
    /// despensa; esto saca de **la mochila que llevas encima**, que es la que se llena
    /// recorriendo la isla. Sin esto, lo que recoges no se le puede dar a nadie: se
    /// vende y ya está, y la mitad social del juego no tocaba la mitad de andar.
    ///
    /// Lo que decide la reacción es el gusto de cada uno, y el gusto es estable: la
    /// misma flor le gusta lo mismo hoy que dentro de un mes. Es lo que permite
    /// aprender qué le va a cada vecino, y aprender eso es conocerlos.
    /// </remarks>
    public interface IGiftService
    {
        /// <summary>¿Se puede regalar eso? Falso para herramientas.</summary>
        bool IsGiftable(string catalogId);

        /// <summary>
        /// Cuánto le gustaría a ese habitante, de 0 a 1. Para poder avisar antes de
        /// dárselo.
        /// </summary>
        float LikingOf(string islanderId, string catalogId);

        /// <summary>
        /// Le da lo que lleve en la mano. Si sale <see cref="GiftResult.Ok"/>, ya se
        /// ha descontado de la mochila.
        /// </summary>
        /// <param name="opinion">−1 no le gusta, 0 le da igual, +1 le encanta.</param>
        GiftResult OfferHeld(string islanderId, out int opinion);
    }
}
