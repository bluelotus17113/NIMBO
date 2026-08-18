using System.Collections.Generic;
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
        private readonly LoveTriangles _triangles;

        /// <summary>Lo que se gasta al declararse. Vive en los contratos: lo pide también la pantalla.</summary>
        public const string BouquetId = RomanceItems.Bouquet;

        /// <summary>Afinidad que hace falta con quien menos y con quien más pega.</summary>
        private const float RequiredAtWorst = 90f;
        private const float RequiredAtBest = 62f;
        private const float WorstCompatibility = -0.2f;
        private const float BestCompatibility = 0.8f;

        /// <summary>
        /// Lo que pesa parecerse al comparar contigo un pretendiente (§14.4).
        /// </summary>
        /// <remarks>
        /// El mismo veinte con el que compiten dos vecinos entre ellos (§13.2). Tenía
        /// que ser el mismo: si el jugador jugara con otra tabla, ganar o perder
        /// dependería de contra quién compites y no de lo que hayas hecho.
        /// </remarks>
        private const float RivalWeight = 20f;

        /// <summary>Lo que cuesta un «no», y lo que hay que esperar para volver.</summary>
        private const float RefusalAffinity = -5f;
        private const int RefusalCooldownDays = 10;

        /// <param name="triangles">
        /// Para que tu declaración **compita** con quien ya suspiraba por esa persona
        /// (§14.4). Puede faltar: entonces el cortejo solo mira si está libre, que es
        /// como funcionaba antes.
        /// </param>
        public Courtship(IIslanderRegistry registry, SocialConfig config,
                         LoveTriangles triangles = null)
        {
            _registry = registry;
            _config = config;
            _triangles = triangles;
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

            // Si alguien ya suspiraba por ella, acabas de meterte de por medio y se
            // entera (§14.4). Antes de esto el rival era decorado: seguía a lo suyo
            // mientras tú te declarabas y no pasaba nada entre vosotros.
            _triangles?.OnPlayerCourts(islanderId);

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

            // Y aunque llegues al listón, puede haber alguien que le llegue más
            // (§14.4). Se compara por lo mismo que compiten dos vecinos entre ellos:
            // cariño más lo que se parecen. No hay premio por haber llegado primero.
            string rivalId = null;
            if (accepted && _triangles != null &&
                _triangles.TryBestSuitor(islander.Id, out rivalId, out float rivalScore))
            {
                float mine = record.Affinity + compatibility * RivalWeight;
                if (rivalScore > mine) accepted = false;
                else rivalId = null;
            }

            if (accepted)
            {
                record.Romance = RomanceStage.Dating;
                record.DatingSinceDay = day;   // desde hoy cuentan los días (§14.5)
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

            // Cuando el «no» es porque hay otro, la frase lo dice. Soltarle el discurso
            // de los ejes cuando el motivo es que quiere a otra persona sería mentirle
            // al jugador sobre lo que ha pasado, y encima le haría cambiar de conducta
            // para arreglar algo que no era el problema.
            string line = rivalId != null && _registry.TryGet(rivalId, out var rival)
                ? $"{islander.Identity.ShortName}: «Lo siento. Hay otra persona, " +
                  $"y es {rival.Identity.ShortName}.»"
                : $"{islander.Identity.ShortName}: «{ReasonFor(player, islander.Personality)}»";

            EventBus.Publish(new CourtshipAnswered(islander.Id, false, line));
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

        // ── pedir la mano ────────────────────────────────────────────────────

        /// <summary>Días saliendo y cariño que hacen falta para la pedida (§14.5).</summary>
        private const int DaysDatingBeforeProposal = 10;
        private const float ProposalAffinity = 85f;

        /// <summary>Días entre la pedida y la boda, para que la aldea se entere.</summary>
        private const int NoticeDays = 3;

        public ProposalRefusal CanPropose(string islanderId, int day)
        {
            if (!_registry.TryGet(islanderId, out var islander))
                return ProposalRefusal.UnknownIslander;

            var record = islander.Relationships.GetOrCreate(SocialIds.Player);

            if (record.Romance == RomanceStage.Engaged || record.Romance == RomanceStage.Married)
                return ProposalRefusal.AlreadyEngaged;

            if (record.Romance != RomanceStage.Dating) return ProposalRefusal.NotDating;

            // Los tres requisitos, uno de cada mitad del juego. Se comprueban en este
            // orden porque es el de lo que cuesta arreglarlos: esperar unos días, ganarse
            // el cariño, fabricar el anillo, pagar la obra de tu casa.
            if (day - record.DatingSinceDay < DaysDatingBeforeProposal)
                return ProposalRefusal.TooEarly;

            if (record.Affinity < ProposalAffinity) return ProposalRefusal.NotFondEnough;

            if (!Has(RomanceItems.Ring)) return ProposalRefusal.NoRing;

            if (ServiceRegistry.TryGet<IHomeUpgradeService>(out var homes) &&
                homes.PlayerLevel < 1)
                return ProposalRefusal.HomeTooSmall;

            return ProposalRefusal.Ok;
        }

        /// <summary>
        /// Pide la mano: gasta el anillo, os deja prometidos y la aldea pone fecha.
        /// </summary>
        /// <remarks>
        /// La fecha se guarda en la misma lista que las bodas de los vecinos, con
        /// <c>SocialIds.Player</c> como uno de los dos. Así la crónica la anuncia con las
        /// mismas plantillas y no hay una segunda forma de tener una boda pendiente.
        /// </remarks>
        public bool Propose(string islanderId, int day, List<WeddingBooking> bookings)
        {
            if (CanPropose(islanderId, day) != ProposalRefusal.Ok) return false;
            if (!Take(RomanceItems.Ring)) return false;

            var islander = _registry.Get(islanderId);
            var book = islander.Relationships;
            var record = book.GetOrCreate(SocialIds.Player);

            record.Romance = RomanceStage.Engaged;
            book.Set(record);

            EventBus.Publish(new RomanceStageChanged(islanderId, SocialIds.Player,
                                                     RomanceStage.Engaged));

            if (bookings != null)
            {
                var booking = WeddingBooking.Between(SocialIds.Player, islanderId, day);
                booking.WeddingDay = day + NoticeDays;
                bookings.Add(booking);

                EventBus.Publish(new WeddingAnnounced(SocialIds.Player, islanderId,
                                                      booking.WeddingDay));
            }

            return true;
        }

        /// <summary>
        /// ¿Toca hoy tu boda?
        /// </summary>
        /// <remarks>
        /// Lo mira el mismo paso diario que contesta a las declaraciones. El
        /// planificador de bodas de la aldea no puede: busca a los dos en el censo y el
        /// protagonista no está.
        /// </remarks>
        public void AdvanceWedding(int day, List<WeddingBooking> bookings)
        {
            if (bookings == null) return;

            for (int i = bookings.Count - 1; i >= 0; i--)
            {
                var booking = bookings[i];
                if (!booking.Involves(SocialIds.Player)) continue;
                if (booking.IsMarried || !booking.HasDate || booking.WeddingDay > day) continue;

                string islanderId = booking.AId == SocialIds.Player ? booking.BId : booking.AId;

                // Si os habéis roto entre la pedida y la fecha, no hay boda. La reserva
                // se cae con ella: una fecha que se queda puesta te casaría con alguien
                // que ya no está contigo el día que volvieras a salir.
                if (!_registry.TryGet(islanderId, out var islander) ||
                    !islander.Relationships.TryGet(SocialIds.Player, out var record) ||
                    record.Romance != RomanceStage.Engaged)
                {
                    bookings.RemoveAt(i);
                    continue;
                }

                record.Romance = RomanceStage.Married;
                islander.Relationships.Set(record);
                booking.MarriedOnDay = day;

                EventBus.Publish(new RomanceStageChanged(islanderId, SocialIds.Player,
                                                         RomanceStage.Married));
                EventBus.Publish(new WeddingHeld(SocialIds.Player, islanderId));
            }
        }

        // ── el ramo y el anillo ──────────────────────────────────────────────

        /// <summary>
        /// El ramo se busca en la mochila y en la despensa, como todo lo demás.
        /// </summary>
        /// <remarks>
        /// El jugador guarda en dos sitios sin haberlo decidido —lo que recoge cae en
        /// la mochila y lo que compra en la despensa—, y un cortejo que solo mirase uno
        /// diría que no tienes el ramo que acabas de fabricar.
        /// </remarks>
        private static bool Has(string catalogId)
        {
            if (ServiceRegistry.TryGet<IInventoryService>(out var bag) &&
                bag.CountOf(catalogId) > 0) return true;

            return ServiceRegistry.TryGet<IEconomyService>(out var economy)
                   && economy.Inventory != null
                   && economy.Inventory.CountOf(catalogId) > 0;
        }

        private static bool Take(string catalogId)
        {
            if (ServiceRegistry.TryGet<IInventoryService>(out var bag) &&
                bag.TryTake(catalogId)) return true;

            return ServiceRegistry.TryGet<IEconomyService>(out var economy)
                   && economy.Inventory != null
                   && economy.Inventory.Remove(catalogId);
        }

        private static bool HasBouquet() => Has(BouquetId);
        private static bool TakeBouquet() => Take(BouquetId);

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
