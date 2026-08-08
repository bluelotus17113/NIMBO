using Nimbo.Core.Events;
using Nimbo.Data.Social;

namespace Nimbo.Social.Romance
{
    /// <summary>
    /// La rama romántica: del flechazo a la boda, y también de vuelta.
    /// </summary>
    /// <remarks>
    /// Se evalúa aparte de la amistad porque avanza distinto: la amistad la mueve un
    /// número, el romance necesita además que los dos quieran a la vez. Un flechazo
    /// que no se corresponde se queda ahí, y esa es justo la historia que el jugador
    /// quiere ver.
    /// </remarks>
    public sealed class RomanceEvaluator
    {
        private readonly SocialConfig _config;

        public RomanceEvaluator(SocialConfig config) => _config = config;

        /// <summary>
        /// ¿Le nace un flechazo? Hace falta amistad hecha, buena compatibilidad y que
        /// no esté ya en otra historia.
        /// </summary>
        public bool ShouldDevelopCrush(in RelationshipRecord record, float compatibility) =>
            record.Romance == RomanceStage.None &&
            record.Family == FamilyTie.None &&
            record.Friendship >= FriendshipStage.Friend &&
            record.Affinity >= _config.CrushMinAffinity &&
            compatibility >= _config.CrushMinCompatibility;

        /// <summary>
        /// Avanza o rompe la relación de A hacia B, sabiendo qué siente B por A.
        /// Devuelve la ficha de A ya actualizada.
        /// </summary>
        public RelationshipRecord Evaluate(string fromId, RelationshipRecord record,
                                           in RelationshipRecord reciprocal)
        {
            var before = record.Romance;

            switch (record.Romance)
            {
                case RomanceStage.Crush:
                case RomanceStage.Confessed:
                    // Correspondido: los dos con flechazo, empiezan a salir.
                    if (reciprocal.Romance is RomanceStage.Crush or RomanceStage.Confessed or
                                              RomanceStage.Dating)
                        record.Romance = RomanceStage.Dating;

                    // O el otro ni siquiera le tiene aprecio: se le pasa, y duele.
                    else if (reciprocal.Affinity < _config.FriendThreshold)
                    {
                        record.Romance = RomanceStage.None;
                        record.Affinity += _config.HeartbreakAffinity;
                    }
                    break;

                case RomanceStage.Dating:
                    if (record.Affinity >= _config.DatingThreshold)
                        record.Romance = RomanceStage.Engaged;
                    else if (record.Affinity <= _config.BreakupThreshold)
                    {
                        record.Romance = RomanceStage.None;
                        record.Affinity += _config.BreakupAffinity;
                    }
                    break;

                case RomanceStage.Engaged:
                    // A casarse no se llega solo: hace falta que el jugador vea la
                    // pedida. De eso se encarga TryMarry, no esto.
                    if (record.Affinity <= _config.BreakupThreshold)
                    {
                        record.Romance = RomanceStage.Separated;
                        record.Affinity += _config.BreakupAffinity;
                    }
                    break;

                case RomanceStage.Married:
                    if (record.Affinity <= _config.DivorceThreshold)
                    {
                        record.Romance = RomanceStage.Separated;
                        record.Affinity += _config.DivorceAffinity;
                    }
                    break;
            }

            if (record.Romance != before)
                EventBus.Publish(new RomanceStageChanged(fromId, record.OtherId, record.Romance));

            return record;
        }

        public bool CanMarry(in RelationshipRecord a, in RelationshipRecord b) =>
            a.Romance == RomanceStage.Engaged && b.Romance == RomanceStage.Engaged &&
            a.Affinity >= _config.EngagedThreshold && b.Affinity >= _config.EngagedThreshold;

        public bool CanHaveBaby(in RelationshipRecord a, in RelationshipRecord b) =>
            a.Romance == RomanceStage.Married && b.Romance == RomanceStage.Married &&
            a.Affinity >= _config.EngagedThreshold && b.Affinity >= _config.EngagedThreshold;
    }
}
