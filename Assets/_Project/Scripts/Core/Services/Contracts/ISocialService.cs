using System.Collections.Generic;
using Nimbo.Data.Social;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se puede pedir la mano todavía (§14.5).</summary>
    /// <remarks>
    /// Tres requisitos, uno de cada mitad del juego: el tiempo y el cariño vienen de lo
    /// social, el anillo de la granja y el oficio, y la casa de la gestión. Es a
    /// propósito — la boda es lo último que pasa en la partida y tiene que haber tocado
    /// las tres cosas para llegar.
    /// </remarks>
    public enum ProposalRefusal
    {
        Ok = 0,
        UnknownIslander,
        NotDating,          // hay que salir antes
        TooEarly,           // lleváis poco
        NotFondEnough,      // falta cariño
        NoRing,
        HomeTooSmall,       // tu cabaña sin ampliar
        AlreadyEngaged,
    }

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

    /// <summary>Lo que dos habitantes pueden hacer juntos, y que mueve su afinidad.</summary>
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

        /// <summary>«¿Me traes algo?» (Convivencia 7). Uno al día por vecino.</summary>
        Favour = 10,
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

        /// <summary>
        /// Lo que ese vecino te cuenta de lo de estos días al hablarle, o <c>null</c> si
        /// hoy no trae nada.
        /// </summary>
        /// <remarks>
        /// **Por qué esto es un miembro del contrato y no una llamada directa.** Quien
        /// pinta la conversación es la ficha del habitante, que vive en `Nimbo.UI`, y
        /// `Nimbo.UI` no referencia `Nimbo.Social` — comprobado en el asmdef, que solo
        /// trae Data, Core, Player, TMP e InputSystem. Sin esta puerta el recuerdo se
        /// queda escrito y probado del lado de dentro sin forma de llegar al jugador,
        /// que es exactamente la enfermedad de §18 y la razón de ser de esta carpeta.
        ///
        /// Devuelve <c>null</c> y no cadena vacía a propósito: «hoy no cuenta nada» es
        /// un caso normal y frecuente —el dado de la personalidad va dentro y hay tope
        /// de una historia por vecino y día—, así que quien llame tiene que decidir qué
        /// enseñar en su lugar. Un <c>string.Empty</c> se colaría en el cartel sin que
        /// nadie lo notara.
        /// </remarks>
        string RecallLine(string islanderId);

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

        /// <summary>
        /// Media en la riña que ese vecino tenga con otro. Devuelve con quién, o null.
        /// </summary>
        /// <remarks>
        /// Baja un escalón de conflicto, no lo borra: hacer las paces del todo es cosa
        /// de ellos dos, y un botón que arregla una enemistad de golpe convierte las
        /// riñas de la aldea en una tarea de mantenimiento.
        ///
        /// Se elige la peor que tenga. Si el jugador tuviera que elegir con quién,
        /// haría falta una lista dentro de otra lista para algo que solo tiene una
        /// respuesta sensata.
        /// </remarks>
        string PlayerMediate(string islanderId);

        /// <summary>
        /// Le pides un favor: te trae algo de lo que recoge por la isla.
        /// </summary>
        /// <remarks>
        /// Devuelve lo que te ha traído, o null si no estaba por la labor. Uno por
        /// vecino y día, como cualquier otra interacción: es un favor, no un empleado.
        /// </remarks>
        string PlayerAskFavour(string islanderId);

        /// <summary>Qué impide pedir la mano, o <c>Ok</c> si nada.</summary>
        ProposalRefusal CanPropose(string islanderId);

        /// <summary>
        /// El protagonista pide la mano. Gasta el anillo y la aldea pone fecha.
        /// </summary>
        /// <remarks>
        /// No os casa en el acto: deja unos días de aviso, como con cualquier pareja de
        /// la isla (§13.1). Una boda que ocurre en el mismo clic no la ve nadie, y el
        /// sentido de que la aldea ponga fecha es justamente que se entere.
        /// </remarks>
        bool PlayerPropose(string islanderId);

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
