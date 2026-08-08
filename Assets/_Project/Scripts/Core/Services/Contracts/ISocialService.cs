using System.Collections.Generic;
using Nimbo.Data.Social;

namespace Nimbo.Core.Services.Contracts
{
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
