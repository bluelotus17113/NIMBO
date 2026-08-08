using System.Collections.Generic;
using Nimbo.Personality.Types;

namespace Nimbo.Personality.Runtime
{
    /// <summary>
    /// La lista de los dieciséis, escrita a mano y a la vista.
    /// </summary>
    /// <remarks>
    /// Es el único fichero que hay que tocar al añadir un tipo, y por eso no lo toca
    /// ningún agente: es de la orquestación. La alternativa era buscarlos por reflexión,
    /// que ahorra estas dieciséis líneas y a cambio falla en silencio si alguien
    /// renombra una clase.
    /// </remarks>
    public static class PersonalityRoster
    {
        public static IReadOnlyList<PersonalityBehaviourBase> CreateAll() =>
            new PersonalityBehaviourBase[]
            {
                new Ermitano(),    // 0  ----
                new Atleta(),      // 1  +---
                new Artesano(),    // 2  -+--
                new Audaz(),       // 3  ++--
                new Afable(),      // 4  --+-
                new Lider(),       // 5  +-+-
                new Anfitrion(),   // 6  -++-
                new Fiestero(),    // 7  +++-
                new Poeta(),       // 8  ---+
                new Visionario(),  // 9  +--+
                new Artista(),     // 10 -+-+
                new Genio(),       // 11 ++-+
                new Romantico(),   // 12 --++
                new Explorador(),  // 13 +-++
                new Cuentista(),   // 14 -+++
                new Entusiasta(),  // 15 ++++
            };

        /// <summary>Un servicio ya montado con los dieciséis. Lo usan el arranque y los tests.</summary>
        public static PersonalityService CreateService() => new PersonalityService(CreateAll());
    }
}
