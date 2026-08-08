using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Fabrica habitantes nuevos. Lo implementa el creador de personajes.
    /// </summary>
    /// <remarks>
    /// Existe para que el módulo social pueda traer un bebé al mundo sin saber nada de
    /// cómo se mezclan dos caras. Social pide un hijo de estos dos; el creador decide
    /// qué nariz le toca.
    /// </remarks>
    public interface IIslanderFactory
    {
        /// <summary>Un habitante al azar, con aspecto y personalidad coherentes entre sí.</summary>
        IslanderData CreateRandom(string seed = null);

        /// <summary>
        /// Un hijo de esos dos: la cara mezclada y la personalidad cerca del punto medio
        /// de sus padres, pero no exactamente — si saliera la media exacta, todos los
        /// hijos de la isla acabarían siendo el mismo tipo.
        /// </summary>
        IslanderData CreateChild(IslanderData parentA, IslanderData parentB);

        /// <summary>Un habitante en blanco para que el jugador lo modele desde cero.</summary>
        IslanderData CreateBlank();
    }
}
