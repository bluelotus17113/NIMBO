using System.Collections.Generic;
using Nimbo.Data.Social;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Lo que dos habitantes pueden hacer juntos, y que mueve su afinidad.</summary>
    /// <summary>Por qué no se puede uno declarar todavía (§14.2).</summary>
    /// <remarks>
    /// Cada motivo se le enseña al jugador con sus palabras. «No puedes» a secas es una
    /// puerta cerrada sin cartel; «te falta un ramo» es algo que hacer esta tarde.
    /// </remarks>
    public enum CourtshipRefusal
    {
        Ok = 0,
        UnknownIslander,
        NotUnlocked,        // Convivencia 5
        NotFriendEnough,    // hace falta ser amigo antes
        NoBouquet,
        TooSoon,            // te dijo que no hace poco
        AlreadyCourting,    // ya te has declarado y falta la respuesta
        Taken,              // ya está con alguien
    }

    public enum SocialInteraction
    {
        Chat = 0,
        Joke = 1,
        Compliment = 2,
        Argue = 3,
        Apologize = 4,
        Gift = 5,
        Confess = 6,
        Hug = 7,
        Ignore = 8,
        PlayTogether = 9,
    }

    /// <summary>
    /// Identificadores que no son de ningún habitante del censo.
    /// </summary>
    public static class SocialIds
    {
        /// <summary>
        /// El protagonista dentro de la agenda de un vecino.
        /// </summary>
        /// <remarks>
        /// No está en el censo —no tiene personalidad, ni necesidades, ni trabajo— pero
        /// sí ocupa una ficha en la agenda de cada vecino, porque lo que el juego
        /// necesita guardar es lo que ellos sienten por ti. Como la ficha se busca por
        /// texto, con esto basta y no hay que tocar el formato de guardado.
        /// </remarks>
        public const string Player = "jugador";
    }

    /// <summary>
    /// El grafo social de la isla: quién quiere a quién, quién está reñido con quién
    /// y quién acabará casándose.
    /// </summary>
    public interface ISocialService
    {
        RelationshipRecord GetRelationship(string fromId, string toId);

        /// <summary>Mueve la afinidad en ambos sentidos y reevalúa las etapas.</summary>
        void ApplyAffinity(string aId, string bId, float delta);

        /// <summary>
        /// Ocurre una interacción entre dos habitantes. El servicio decide el efecto
        /// según sus personalidades y su historia; quien llama no calcula nada.
        /// </summary>
        void Interact(string aId, string bId, SocialInteraction interaction);

        /// <summary>Los presenta. Deja de ser un desconocido y empieza a poder pasar de todo.</summary>
        void Introduce(string aId, string bId);

        /// <summary>Lo que ese habitante siente por el protagonista.</summary>
        RelationshipRecord PlayerRelationship(string islanderId);

        /// <summary>
        /// El protagonista hace algo con un habitante. Devuelve false si hoy ya no
        /// cuenta, que es la señal de que hay que decírselo al jugador.
        /// </summary>
        /// <remarks>
        /// Va aparte de <see cref="Interact"/> y no reutilizándolo porque el
        /// protagonista solo mueve un lado: importa lo que el vecino sienta por ti, y
        /// lo que tú sientas por él lo decides tú y no hace falta guardarlo. Además
        /// aquí no hay compatibilidad de personalidades que aplicar — el protagonista
        /// no tiene tipo, y ese es justo el punto: con quién te llevas bien es cosa de
        /// cómo juegues, no de un sorteo del creador de personajes.
        /// </remarks>
        bool PlayerInteract(string islanderId, SocialInteraction interaction);

        /// <summary>Qué impide declararse, o <c>Ok</c> si nada.</summary>
        CourtshipRefusal CanConfess(string islanderId);

        /// <summary>
        /// El protagonista se declara. Gasta el ramo y deja la respuesta para mañana.
        /// </summary>
        /// <remarks>
        /// La respuesta no es inmediata **a propósito**. Un sí o un no en el mismo clic
        /// convierte la declaración en una tirada de dados que se mira una vez; con un
        /// día de por medio, el jugador se va a dormir con la duda, que es exactamente
        /// lo que se quiere que sienta.
        /// </remarks>
        bool PlayerConfess(string islanderId);

        IEnumerable<RelationshipRecord> FriendsOf(string islanderId);
        IEnumerable<RelationshipRecord> ConflictsOf(string islanderId);

        /// <summary>La pareja actual, o null. Solo hay una a la vez.</summary>
        string PartnerOf(string islanderId);

        /// <summary>Intenta casarlos. Falla si no llegan a la etapa o si ya hay pareja.</summary>
        bool TryMarry(string aId, string bId);

        /// <summary>Un hijo de esa pareja. Devuelve el id del recién nacido, o null.</summary>
        string TryHaveBaby(string aId, string bId);
    }
}
