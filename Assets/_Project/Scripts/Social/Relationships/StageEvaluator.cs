using Nimbo.Core.Events;
using Nimbo.Data.Social;

namespace Nimbo.Social.Relationships
{
    /// <summary>
    /// Traduce un número de afinidad a en qué punto está la relación, y avisa cuando
    /// ese punto cambia.
    /// </summary>
    /// <remarks>
    /// Amistad y riña se evalúan por separado y a la vez. Suena redundante — nadie es
    /// mejor amigo y enemigo del mismo — pero al ir en dos ejes basta con que las dos
    /// escalas se lean del mismo número y no hay que decidir cuál gana.
    /// </remarks>
    public sealed class StageEvaluator
    {
        private readonly SocialConfig _config;

        public StageEvaluator(SocialConfig config) => _config = config;

        /// <summary>
        /// Recalcula las etapas de una ficha ya modificada. Devuelve la ficha nueva y
        /// publica un evento por cada etapa que haya cambiado.
        /// </summary>
        public RelationshipRecord Evaluate(string fromId, RelationshipRecord record)
        {
            var friendship = FriendshipFor(record);
            var conflict = ConflictFor(record.Affinity);

            if (friendship != record.Friendship)
            {
                record.Friendship = friendship;
                EventBus.Publish(new FriendshipStageChanged(fromId, record.OtherId, friendship));
            }

            // Una rivalidad no se cura sola: no la borra que la afinidad esté bien
            // (§13.2). Solo la tapa algo peor —una riña de verdad, una enemistad— y de
            // ahí ya no se vuelve a «rivales», se vuelve a estar bien. Sin esto, el
            // triángulo se deshacía cada mañana en la reevaluación diaria y no llegaba
            // a verse nunca.
            bool taparRivalidad = record.Conflict == ConflictStage.Rivalry
                                  && conflict.Severity() <= ConflictStage.Rivalry.Severity();

            if (conflict != record.Conflict && !taparRivalidad)
            {
                record.Conflict = conflict;
                EventBus.Publish(new ConflictStageChanged(fromId, record.OtherId, conflict));
            }

            return record;
        }

        private FriendshipStage FriendshipFor(in RelationshipRecord record)
        {
            // Un desconocido sigue siéndolo hasta que se hablen, por mucha afinidad
            // que le den los tipos de personalidad: primero hay que conocerse.
            if (record.Interactions == 0) return FriendshipStage.Stranger;

            float a = record.Affinity;
            if (a >= _config.BestFriendThreshold) return FriendshipStage.BestFriend;
            if (a >= _config.CloseFriendThreshold) return FriendshipStage.CloseFriend;
            if (a >= _config.FriendThreshold) return FriendshipStage.Friend;
            return FriendshipStage.Acquaintance;
        }

        private ConflictStage ConflictFor(float affinity)
        {
            if (affinity <= _config.FeudThreshold) return ConflictStage.Feud;
            if (affinity <= _config.QuarrelThreshold) return ConflictStage.Quarrel;
            if (affinity <= _config.TensionThreshold) return ConflictStage.Tension;
            return ConflictStage.None;
        }
    }
}
