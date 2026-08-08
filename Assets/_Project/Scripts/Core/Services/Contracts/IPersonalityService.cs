using Nimbo.Data.Islanders;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// La puerta a los dieciséis comportamientos. Lo implementa <c>Nimbo.Personality</c>
    /// y lo consumen la simulación, lo social y la interfaz.
    /// </summary>
    public interface IPersonalityService
    {
        /// <summary>El comportamiento de ese índice de tipo (0-15).</summary>
        IPersonalityBehaviour ByIndex(int typeIndex);

        /// <summary>El comportamiento que le toca a ese perfil, según sus cuatro ejes.</summary>
        IPersonalityBehaviour For(in PersonalityProfile profile);

        IPersonalityBehaviour ById(string personalityId);

        /// <summary>
        /// Cuánto se llevan bien dos tipos, de −8 a +8, antes de contar su historia
        /// juntos. Simétrico: da igual el orden de los argumentos.
        /// </summary>
        int CompatibilityBetween(int typeIndexA, int typeIndexB);
    }
}
