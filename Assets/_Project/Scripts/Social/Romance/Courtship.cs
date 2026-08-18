using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Social.Relationships;
using UnityEngine;

namespace Nimbo.Social.Romance
{
    /// <summary>
    /// El protagonista se declara, y le pueden decir que no.
    /// </summary>
    /// <remarks>
    /// El romance del protagonista **no es una barra que se llena**. En un juego donde
    /// puedes hablar con la misma persona todos los días, el romance por acumulación es
    /// inevitable y aburrido, y por eso los vecinos siguen sin enamorarse de ti solos.
    /// Lo que hay es una puerta distinta: una declaración que se hace una vez, cuesta un
    /// ramo, y tiene respuesta.
    ///
    /// Y la respuesta llega **al día siguiente**. Un sí o un no en el mismo clic
    /// convierte la declaración en una tirada de dados que se mira una vez; con un día
    /// de por medio uno se va a dormir con la duda.
    ///
    /// **La compatibilidad pone el precio, no el veredicto** (§14.3.4). Un umbral de
    /// «compatible o no» es un muro, y un muro en un juego cuyo contrato es no
    /// castigar; encima el jugador no ve el número, así que un «no» seco se lee como
    /// arbitrario. Lo que hace la compatibilidad es mover **cuánta afinidad hace
    /// falta**: nadie es imposible, a los opuestos les cuesta el doble de convivencia.
    /// </remarks>
    public sealed class Courtship
    {
        private readonly IIslanderRegistry _registry;
        private readonly SocialConfig _config;

        /// <summary>Lo que se gasta al declararse.</summary>
        public const string BouquetId = "gift_ramo";

        /// <summary>Afinidad que hace falta con quien menos y con quien más pega.</summary>
        private const float RequiredAtWorst = 90f;
        private const float RequiredAtBest = 62f;
        private const float WorstCompatibility = -0.2f;
        private const float BestCompatibility = 0.8f;

        /// <summary>Lo que cuesta un «no», y lo que hay que esperar para volver.</summary>
        private const float RefusalAffinity = -5f;
        private const int RefusalCooldownDays = 10;

        public Courtship(IIslanderRegistry registry, SocialConfig config)
        {
            _registry = registry;
            _config = config;
        }

        // ── declararse ───────────────────────────────────────────────────────

        public CourtshipRefusal CanConfess(string islanderId, int day)
        {
            if (!_registry.TryGet(islanderId, out var islander))
                return CourtshipRefusal.UnknownIslander;

            if (ServiceRegistry.TryGet<IPlayerProgression>(out var progression) &&
                !progression.IsUnlocked(Unlock.Courtship))
                return CourtshipRefusal.NotUnlocked;

            var record = islander.Relationships.GetOrCreate(SocialIds.Player);

            if (record.Romance == RomanceStage.Confessed) return CourtshipRefusal.AlreadyCourting;
            if (record.BlockedUntilDay > day) return CourtshipRefusal.TooSoon;

            // Con otro vecino ya no estás a tiempo. Es lo que hace que los triángulos
            // de la aldea (§13.2) te afecten aunque no seas parte de ellos: mientras
            // dudas, ellos avanzan.
            if (PartnerOf(islander) != null) return CourtshipRefusal.Taken;

            if (record.Friendship < FriendshipStage.Friend)
                return CourtshipRefusal.NotFriendEnough;

            if (!HasBouquet()) return CourtshipRefusal.NoBouquet;

            return CourtshipRefusal.Ok;
        }

        /// <summary>Deja la declaración hecha y el ramo gastado. Contesta mañana.</summary>
        public bool Confess(string islanderId, int day)
        {
            if (CanConfess(islanderId, day) != CourtshipRefusal.Ok) return false;
            if (!TakeBouquet()) return false;

            var islander = _registry.Get(islanderId);
            var book = islander.Relationships;
            var record = book.GetOrCreate(SocialIds.Player);

            record.Romance = RomanceStage.Confessed;
            book.Set(record);

            EventBus.Publish(new RomanceStageChanged(islanderId, SocialIds.Player,
                                                     RomanceStage.Confessed));
            return true;
        }

        // ── la respuesta, al día siguiente ───────────────────────────────────

        /// <summary>Contesta a todas las declaraciones que estén esperando.</summary>
        public void AnswerPending(int day, IConductService conduct)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                var book = islander.Relationships;
                if (!book.TryGet(SocialIds.Player, out var record)) continue;
                if (record.Romance != RomanceStage.Confessed) continue;

                Answer(islander, record, day, conduct);
            }
        }

        private void Answer(IslanderData islander, RelationshipRecord record, int day,
                            IConductService conduct)
        {
            var player = conduct?.Profile ?? default;

            // Con `Between` y nunca con `Full`: `Full` suma el sesgo entre tipos de
            // personalidad, y el protagonista no tiene tipo — tiene ejes. Pedirle uno
            // lo convertiría en uno de los dieciséis arquetipos justo cuando la gracia
            // es que no lo sea (§14.3.2).
            float compatibility = Compatibility.Between(player, islander.Personality);

            float required = Mathf.Lerp(RequiredAtWorst, RequiredAtBest,
                                        Mathf.InverseLerp(WorstCompatibility, BestCompatibility,
                                                          compatibility));

            bool accepted = record.Affinity >= required;
            var book = islander.Relationships;

            if (accepted)
            {
                record.Romance = RomanceStage.Dating;
                book.Set(record);

                EventBus.Publish(new RomanceStageChanged(islander.Id, SocialIds.Player,
                                                         RomanceStage.Dating));
                EventBus.Publish(new CourtshipAnswered(islander.Id, true,
                    $"{islander.Identity.ShortName} te ha dicho que sí."));
                return;
            }

            record.Romance = RomanceStage.None;
            record.Affinity = Mathf.Max(RelationshipRecord.MinAffinity,
                                        record.Affinity + RefusalAffinity);
            record.BlockedUntilDay = day + RefusalCooldownDays;
            book.Set(record);

            EventBus.Publish(new RomanceStageChanged(islander.Id, SocialIds.Player,
                                                     RomanceStage.None));
            EventBus.Publish(new CourtshipAnswered(islander.Id, false,
                $"{islander.Identity.ShortName}: «{ReasonFor(player, islander.Personality)}»"));
        }

        /// <summary>
        /// El «no» nombra el eje que más lejos quedó.
        /// </summary>
        /// <remarks>
        /// Con eso el jugador aprende el sistema sin tutorial y sin ver un número: la
        /// frase le dice en qué no se parecen, y cambiar eso de verdad lleva semanas
        /// —lo cual es justo lo que hace que signifique algo—.
        ///
        /// Se pesa cada eje por lo que pesa en la compatibilidad: si la Actitud manda
        /// más que la Energía al decidir, también tiene que mandar más al explicarlo.
        /// </remarks>
        private static string ReasonFor(in PersonalityProfile player, in PersonalityProfile other)
        {
            float energy = Mathf.Abs(player.Energy - other.Energy) * 0.20f;
            float expression = Mathf.Abs(player.Expression - other.Expression) * 0.20f;
            float attitude = Mathf.Abs(player.Attitude - other.Attitude) * 0.35f;
            float outlook = Mathf.Abs(player.Outlook - other.Outlook) * 0.25f;

            float worst = Mathf.Max(Mathf.Max(energy, expression), Mathf.Max(attitude, outlook));

            if (worst <= 0.0001f)
                return "Te aprecio mucho, pero todavía no. Dame tiempo.";

            if (worst == attitude)
                return player.Attitude > other.Attitude
                    ? "Siempre estás rodeado de gente. Yo no sé estar así."
                    : "Yo necesito gente alrededor, y tú necesitas que no la haya.";

            if (worst == outlook)
                return player.Outlook > other.Outlook
                    ? "Tú estás en las nubes. Yo tengo los pies en el suelo."
                    : "Tú tienes los pies en el suelo. Yo estoy en las nubes.";

            if (worst == energy)
                return player.Energy > other.Energy
                    ? "Eres de los que no paran quietos, y yo necesito calma."
                    : "Vas muy despacio para mí.";

            return player.Expression > other.Expression
                ? "Lo dices todo demasiado pronto."
                : "No sé nunca lo que estás pensando.";
        }

        // ── el ramo ──────────────────────────────────────────────────────────

        /// <summary>
        /// El ramo se busca en la mochila y en la despensa, como todo lo demás.
        /// </summary>
        /// <remarks>
        /// El jugador guarda en dos sitios sin haberlo decidido —lo que recoge cae en
        /// la mochila y lo que compra en la despensa—, y un cortejo que solo mirase uno
        /// diría que no tienes el ramo que acabas de fabricar.
        /// </remarks>
        private static bool HasBouquet()
        {
            if (ServiceRegistry.TryGet<IInventoryService>(out var bag) &&
                bag.CountOf(BouquetId) > 0) return true;

            return ServiceRegistry.TryGet<IEconomyService>(out var economy)
                   && economy.Inventory != null
                   && economy.Inventory.CountOf(BouquetId) > 0;
        }

        private static bool TakeBouquet()
        {
            if (ServiceRegistry.TryGet<IInventoryService>(out var bag) &&
                bag.TryTake(BouquetId)) return true;

            return ServiceRegistry.TryGet<IEconomyService>(out var economy)
                   && economy.Inventory != null
                   && economy.Inventory.Remove(BouquetId);
        }

        private static string PartnerOf(IslanderData islander)
        {
            var records = islander.Relationships.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].OtherId == SocialIds.Player) continue;
                if (records[i].IsRomantic) return records[i].OtherId;
            }
            return null;
        }
    }
}
