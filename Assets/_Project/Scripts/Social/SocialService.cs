using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Social.Relationships;
using Nimbo.Social.Romance;
using UnityEngine;

namespace Nimbo.Social
{
    /// <summary>
    /// El grafo social de la isla: quién conoce a quién, quién se lleva bien con quién,
    /// quién está reñido y quién acaba casándose.
    /// </summary>
    /// <remarks>
    /// Toda relación es bilateral pero **asimétrica**: A puede querer a B más de lo que
    /// B quiere a A, y ahí están los amores no correspondidos. Por eso cada cambio
    /// escribe en las dos agendas por separado en vez de en una tabla compartida.
    /// </remarks>
    public sealed class SocialService : ISocialService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly IPersonalityService _personalities;
        private readonly ISimulationService _simulation;
        private readonly IIslanderFactory _factory;
        private readonly GameClock _clock;
        private readonly SocialConfig _config;

        private readonly StageEvaluator _stages;
        private readonly RomanceEvaluator _romance;
        private readonly LoveTriangles _triangles;
        private readonly Courtship _courtship;

        private readonly SpouseChores _chores;

        /// <summary>Las bodas apuntadas de la partida. La tuya se guarda con las suyas.</summary>
        private readonly List<WeddingBooking> _weddings;

        /// <summary>Cuántas veces han hecho hoy cada cosa, para los límites diarios.</summary>
        private readonly Dictionary<(string, string, SocialInteraction), int> _todayCounts = new();
        private int _countedDay = -1;

        /// <param name="triangles">
        /// La lista de la partida, para que los triángulos duren sus seis días de un
        /// arranque a otro. Sin ella se llevan en memoria: las pruebas montan el social
        /// sin partida y no tienen por qué enterarse.
        /// </param>
        public SocialService(IIslanderRegistry registry, IPersonalityService personalities,
                             ISimulationService simulation, IIslanderFactory factory,
                             GameClock clock, SocialConfig config,
                             List<LoveTriangle> triangles = null,
                             List<WeddingBooking> weddings = null)
        {
            _registry = registry;
            _personalities = personalities;
            _simulation = simulation;
            _factory = factory;
            _clock = clock;
            _config = config;

            _stages = new StageEvaluator(config);
            _romance = new RomanceEvaluator(config);
            _triangles = new LoveTriangles(registry, personalities, simulation, config, triangles);
            _courtship = new Courtship(registry, config);
            _chores = new SpouseChores(registry);
            _weddings = weddings;

            EventBus.Subscribe<DayPassed>(OnDayPassed);
        }

        /// <summary>Los triángulos abiertos. Los lee la ficha del vecino y la crónica.</summary>
        public LoveTriangles Triangles => _triangles;

        public void Dispose() => EventBus.Unsubscribe<DayPassed>(OnDayPassed);

        private void OnDayPassed(DayPassed evt)
        {
            _todayCounts.Clear();
            _countedDay = evt.Day;
            CoolDownAndEvaluate(evt.Day);
        }

        /// <summary>
        /// Cada día las relaciones se enfrían un poco hacia cero y se reevalúan los
        /// romances. Sin esto, dos habitantes que se cayeron bien una vez seguirían
        /// siendo mejores amigos aunque no volvieran a cruzarse nunca.
        /// </summary>
        /// <remarks>
        /// El día llega **en el aviso** y no se lee del reloj. Son dos fuentes para el
        /// mismo dato y solo una es la buena: el aviso dice qué día acaba de empezar, y
        /// el reloj puede ir por delante —o por detrás, si alguien publica el aviso a
        /// mano— sin que nadie se entere. Se vio en las bodas del protagonista: la fecha
        /// se comparaba contra un reloj parado y no llegaba nunca.
        /// </remarks>
        private void CoolDownAndEvaluate(int day)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                var records = islander.Relationships.Records;

                for (int j = 0; j < records.Count; j++)
                {
                    var record = records[j];
                    if (record.IsFamily || record.Romance == RomanceStage.Married) continue;

                    record.Affinity = Mathf.MoveTowards(record.Affinity, 0f, _config.DailyDecay);
                    record = _stages.Evaluate(islander.Id, record);

                    if (_registry.TryGet(record.OtherId, out var other) &&
                        other.Relationships.TryGet(islander.Id, out var back))
                        record = _romance.Evaluate(islander.Id, record, back);

                    records[j] = record;
                }
            }

            // Lo primero de la mañana es contestar a quien se declaró ayer. Antes que
            // los triángulos y que los flechazos nuevos: si te dicen que sí, ya estás
            // con esa persona cuando la aldea se pone a repartir enamoramientos.
            ServiceRegistry.TryGet<IConductService>(out var conduct);
            _courtship.AnswerPending(day, conduct);
            _courtship.AdvanceWedding(day, _weddings);

            // Y tu pareja pasa por el huerto, si la tienes. Una casilla, la que esté
            // seca: lo justo para que se note que ya no vives solo.
            _chores.DoMorningChore();

            // Los triángulos antes que los flechazos nuevos: lo primero es cerrar los
            // que cumplen hoy, y así el que acaba de llevarse el desengaño arranca ya
            // con su espera puesta en vez de encapricharse de otra esta misma mañana.
            _triangles.AdvanceDay(day);

            DevelopCrushes(day);
        }

        /// <summary>Le nacen flechazos a quien le toque, una vez al día.</summary>
        private void DevelopCrushes(int day)
        {
            var all = _registry.All;

            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                if (HasPartner(islander)) continue;

                // Al que acaba de perder un triángulo no le nace nada en unos días
                // (§13.2). Sin la espera, el desamor no significa nada: se encapricha
                // de otra persona a la mañana siguiente y nadie se entera de que ha
                // pasado algo.
                if (islander.CrushBlockedUntilDay > day) continue;

                var records = islander.Relationships.Records;
                for (int j = 0; j < records.Count; j++)
                {
                    var record = records[j];
                    if (!_registry.TryGet(record.OtherId, out var other)) continue;

                    float compatibility = CompatibilityOf(islander, other);
                    if (!_romance.ShouldDevelopCrush(record, compatibility)) continue;

                    record.Romance = RomanceStage.Crush;
                    records[j] = record;
                    EventBus.Publish(new RomanceStageChanged(islander.Id, record.OtherId,
                                                             RomanceStage.Crush));

                    // ¿Ya había alguien suspirando por esa persona? Ahí nace la rivalidad.
                    _triangles.OnCrushBorn(islander.Id, record.OtherId, day);

                    break; // uno por día: si no, se enamoraría de media isla el martes
                }
            }
        }

        private float CompatibilityOf(IslanderData a, IslanderData b)
        {
            int bias = _personalities.CompatibilityBetween(
                a.Personality.TypeIndex, b.Personality.TypeIndex);
            return Compatibility.Full(a.Personality, b.Personality, bias);
        }

        private static bool HasPartner(IslanderData islander)
        {
            var records = islander.Relationships.Records;
            for (int i = 0; i < records.Count; i++)
                if (records[i].Romance >= RomanceStage.Dating &&
                    records[i].Romance != RomanceStage.Separated) return true;
            return false;
        }

        // --- ISocialService --------------------------------------------------

        public RelationshipRecord GetRelationship(string fromId, string toId)
        {
            if (!_registry.TryGet(fromId, out var from)) return default;
            return from.Relationships.GetOrCreate(toId);
        }

        public void ApplyAffinity(string aId, string bId, float delta)
        {
            ApplyOneWay(aId, bId, delta);
            ApplyOneWay(bId, aId, delta);
        }

        /// <summary>
        /// Mueve lo que A siente por B, modulado por lo bien que pegan sus tipos.
        /// Un cambio entre incompatibles vale la mitad; entre compatibles, hasta 1.5×.
        /// </summary>
        private void ApplyOneWay(string fromId, string toId, float delta)
        {
            if (!_registry.TryGet(fromId, out var from) || !_registry.TryGet(toId, out var to))
                return;

            var record = from.Relationships.GetOrCreate(toId);

            float compatibility = CompatibilityOf(from, to);
            float scaled = delta * (0.5f + Mathf.Clamp01((compatibility + 1f) * 0.5f) *
                                            (_config.CompatibilityWeight * 2f));

            float before = record.Affinity;
            record.Affinity = Mathf.Clamp(before + scaled,
                                          RelationshipRecord.MinAffinity,
                                          RelationshipRecord.MaxAffinity);
            record.Interactions++;
            record.LastInteractionMinute = _clock.ElapsedMinutes;

            record = _stages.Evaluate(fromId, record);
            from.Relationships.Set(record);

            EventBus.Publish(new AffinityChanged(fromId, toId,
                                                 record.Affinity - before, record.Affinity));
        }

        public void Interact(string aId, string bId, SocialInteraction interaction)
        {
            if (aId == bId) return;

            var effect = _config.EffectOf(interaction);
            if (!WithinDailyCap(aId, bId, interaction, effect.DailyCap)) return;

            ApplyAffinity(aId, bId, effect.Affinity);

            // La cara que ponen la decide su personalidad, no la interacción.
            var reaction = ReactionFor(interaction);
            ShowReaction(aId, reaction);
            ShowReaction(bId, reaction);

            if (interaction == SocialInteraction.Chat || interaction == SocialInteraction.Joke)
            {
                _simulation.ApplyNeed(aId, NeedKind.Social, 5f);
                _simulation.ApplyNeed(bId, NeedKind.Social, 5f);
            }

            // Una rivalidad no se cura sola (§13.2): hace falta que uno dé el paso.
            // Disculparse es el único que existe hoy, y es el que le pega — no se
            // arregla una rivalidad regalando cosas ni contando chistes.
            if (interaction == SocialInteraction.Apologize) ClearRivalry(aId, bId);
        }

        /// <summary>
        /// Se han hecho las paces: la rivalidad se levanta en los dos lados.
        /// </summary>
        /// <remarks>
        /// En los dos aunque solo uno se disculpe. Dejarla puesta en el otro daría un
        /// vecino que sigue viendo a un rival en alguien que ya vino a pedirle perdón,
        /// y eso no hay forma de deshacerlo desde el juego.
        ///
        /// Solo levanta la rivalidad. Si además estaban reñidos de antes, eso sigue
        /// donde estaba: se ha arreglado una cosa, no todas.
        /// </remarks>
        private void ClearRivalry(string aId, string bId)
        {
            Clear(aId, bId);
            Clear(bId, aId);

            void Clear(string fromId, string toId)
            {
                if (!_registry.TryGet(fromId, out var islander)) return;

                var book = islander.Relationships;
                if (!book.TryGet(toId, out var record)) return;
                if (record.Conflict != ConflictStage.Rivalry) return;

                record.Conflict = ConflictStage.None;
                book.Set(record);

                EventBus.Publish(new ConflictStageChanged(fromId, toId, ConflictStage.None));
            }
        }

        private void ShowReaction(string islanderId, PersonalityReaction reaction)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            var behaviour = _personalities.For(islander.Personality);
            _simulation.ShowEmotion(islanderId, behaviour.ReactTo(reaction), 4f);
        }

        private static PersonalityReaction ReactionFor(SocialInteraction interaction) => interaction switch
        {
            SocialInteraction.Argue => PersonalityReaction.QuarrelStarted,
            SocialInteraction.Apologize => PersonalityReaction.Reconciled,
            SocialInteraction.Compliment => PersonalityReaction.Complimented,
            SocialInteraction.Ignore => PersonalityReaction.Ignored,
            SocialInteraction.Gift => PersonalityReaction.GiftLoved,
            _ => PersonalityReaction.Introduced,
        };

        /// <summary>
        /// Los límites diarios existen para que el jugador no pueda subir una amistad a
        /// tope repitiendo «charlar» cuarenta veces seguidas.
        /// </summary>
        private bool WithinDailyCap(string aId, string bId, SocialInteraction interaction, int cap)
        {
            if (cap <= 0) return true;

            if (_countedDay != _clock.Day)
            {
                _todayCounts.Clear();
                _countedDay = _clock.Day;
            }

            // Ordenado, para que charlar A→B y B→A cuenten como la misma charla.
            var key = string.CompareOrdinal(aId, bId) <= 0
                ? (aId, bId, interaction)
                : (bId, aId, interaction);

            _todayCounts.TryGetValue(key, out int used);
            if (used >= cap) return false;

            _todayCounts[key] = used + 1;
            return true;
        }

        public void Introduce(string aId, string bId)
        {
            if (aId == bId) return;
            if (!_registry.TryGet(aId, out var a) || !_registry.TryGet(bId, out var b)) return;

            SeedAcquaintance(a, bId);
            SeedAcquaintance(b, aId);

            // Los tipos que pegan empiezan ya con algo de ventaja, y los que chocan al revés.
            int bias = _personalities.CompatibilityBetween(
                a.Personality.TypeIndex, b.Personality.TypeIndex);
            if (bias != 0) ApplyAffinity(aId, bId, bias);
        }

        private void SeedAcquaintance(IslanderData islander, string otherId)
        {
            var record = islander.Relationships.GetOrCreate(otherId);
            if (record.Friendship != FriendshipStage.Stranger) return;

            record.Interactions = Mathf.Max(1, record.Interactions);
            record.LastInteractionMinute = _clock.ElapsedMinutes;
            record = _stages.Evaluate(islander.Id, record);
            islander.Relationships.Set(record);
        }

        // Las dos listas de abajo dejan fuera al protagonista a propósito: alimentan lo
        // que hacen los vecinos entre ellos —los sueños, las peticiones de presentar a
        // alguien— y eso resuelve nombres y personalidades contra el censo, donde el
        // protagonista no está. Lo que él tenga con cada uno se pregunta por
        // PlayerRelationship, que es el camino que sí lo sabe.

        public IEnumerable<RelationshipRecord> FriendsOf(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) yield break;

            var records = islander.Relationships.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].OtherId == SocialIds.Player) continue;
                if (records[i].Friendship >= FriendshipStage.Friend) yield return records[i];
            }
        }

        public IEnumerable<RelationshipRecord> ConflictsOf(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) yield break;

            var records = islander.Relationships.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].OtherId == SocialIds.Player) continue;
                if (records[i].Conflict != ConflictStage.None) yield return records[i];
            }
        }

        // --- El protagonista ------------------------------------------------

        public RelationshipRecord PlayerRelationship(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return default;
            return islander.Relationships.GetOrCreate(SocialIds.Player);
        }

        /// <summary>
        /// Mueve lo que un vecino siente por ti. Un solo lado, y sin compatibilidad.
        /// </summary>
        /// <remarks>
        /// El enfriamiento diario de <see cref="CoolDownAndEvaluate"/> alcanza también
        /// a esta ficha, y es lo que se quiere: dejar de aparecer por la isla enfría lo
        /// que sienten por ti igual que enfría lo que sienten entre ellos.
        ///
        /// Lo que no le alcanza es el romance. <see cref="DevelopCrushes"/> pide el
        /// otro al censo antes de nada, y el protagonista no está: nadie se enamora de
        /// ti por acumular charlas, que es exactamente lo que hay que evitar en un
        /// juego donde puedes hablar con la misma persona todos los días.
        /// </remarks>
        public bool PlayerInteract(string islanderId, SocialInteraction interaction)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return false;

            var effect = _config.EffectOf(interaction);
            if (!WithinDailyCap(SocialIds.Player, islanderId, interaction, effect.DailyCap))
                return false;

            var record = islander.Relationships.GetOrCreate(SocialIds.Player);

            float before = record.Affinity;
            record.Affinity = Mathf.Clamp(before + effect.Affinity,
                                          RelationshipRecord.MinAffinity,
                                          RelationshipRecord.MaxAffinity);
            record.Interactions++;
            record.LastInteractionMinute = _clock.ElapsedMinutes;

            record = _stages.Evaluate(islanderId, record);
            islander.Relationships.Set(record);

            EventBus.Publish(new AffinityChanged(islanderId, SocialIds.Player,
                                                 record.Affinity - before, record.Affinity));

            ShowReaction(islanderId, ReactionFor(interaction));

            if (interaction is SocialInteraction.Chat or SocialInteraction.Joke)
                _simulation.ApplyNeed(islanderId, NeedKind.Social, 5f);

            NoteExpression(interaction);
            return true;
        }

        /// <summary>
        /// Cómo hablas con la gente es el eje de Expresión (§14.3.1).
        /// </summary>
        /// <remarks>
        /// Los gestos que se ven —una broma, un abrazo, un halago, jugar— frente a los
        /// que no —charlar sin más—. Es una proporción entre cosas que compiten por el
        /// mismo momento: cuando te pones delante de alguien haces una **o** la otra.
        ///
        /// Declararse no cuenta para ningún lado. Pasa una vez con cada persona y es la
        /// decisión más grande del juego; dejar que además te describa el carácter
        /// sería medir el argumento en vez de la costumbre.
        /// </remarks>
        private void NoteExpression(SocialInteraction interaction)
        {
            if (interaction is SocialInteraction.Confess or SocialInteraction.Gift) return;
            if (!ServiceRegistry.TryGet<IConductService>(out var conduct)) return;

            bool expressive = interaction is SocialInteraction.Joke or SocialInteraction.Hug
                              or SocialInteraction.Compliment or SocialInteraction.PlayTogether;

            conduct.Note(PersonalityAxis.Expression, expressive);
        }

        public CourtshipRefusal CanConfess(string islanderId) =>
            _courtship.CanConfess(islanderId, _clock.Day);

        public bool PlayerConfess(string islanderId) =>
            _courtship.Confess(islanderId, _clock.Day);

        public ProposalRefusal CanPropose(string islanderId) =>
            _courtship.CanPropose(islanderId, _clock.Day);

        public bool PlayerPropose(string islanderId) =>
            _courtship.Propose(islanderId, _clock.Day, _weddings);

        public string PartnerOf(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return null;

            var records = islander.Relationships.Records;
            for (int i = 0; i < records.Count; i++)
                if (records[i].IsRomantic) return records[i].OtherId;
            return null;
        }

        public bool TryMarry(string aId, string bId)
        {
            if (!_registry.TryGet(aId, out var a) || !_registry.TryGet(bId, out var b)) return false;
            if (PartnerOf(aId) != bId || PartnerOf(bId) != aId) return false;

            var fromA = a.Relationships.GetOrCreate(bId);
            var fromB = b.Relationships.GetOrCreate(aId);
            if (!_romance.CanMarry(fromA, fromB)) return false;

            Wed(a, ref fromA);
            Wed(b, ref fromB);

            _simulation.ApplyHappiness(aId, 25f);
            _simulation.ApplyHappiness(bId, 25f);
            _simulation.ShowEmotion(aId, Emotion.Love, 8f);
            _simulation.ShowEmotion(bId, Emotion.Love, 8f);
            return true;
        }

        private void Wed(IslanderData islander, ref RelationshipRecord record)
        {
            record.Romance = RomanceStage.Married;
            record.Family = FamilyTie.Spouse;
            islander.Relationships.Set(record);
            EventBus.Publish(new RomanceStageChanged(islander.Id, record.OtherId,
                                                     RomanceStage.Married));
        }

        public string TryHaveBaby(string aId, string bId)
        {
            if (!_registry.TryGet(aId, out var a) || !_registry.TryGet(bId, out var b)) return null;

            var fromA = a.Relationships.GetOrCreate(bId);
            var fromB = b.Relationships.GetOrCreate(aId);
            if (!_romance.CanHaveBaby(fromA, fromB)) return null;

            var child = _factory.CreateChild(a, b);
            if (child == null) return null;

            _registry.Add(child);
            LinkFamily(a, child, FamilyTie.Child);
            LinkFamily(b, child, FamilyTie.Child);
            LinkFamily(child, a, FamilyTie.Parent);
            LinkFamily(child, b, FamilyTie.Parent);

            EventBus.Publish(new IslanderCreated(child.Id));
            EventBus.Publish(new BabyBorn(aId, bId, child.Id));
            return child.Id;
        }

        /// <summary>La familia arranca ya queriéndose: nadie conoce a su hijo desde cero.</summary>
        private void LinkFamily(IslanderData from, IslanderData to, FamilyTie tie)
        {
            var record = from.Relationships.GetOrCreate(to.Id);
            record.Family = tie;
            record.Affinity = Mathf.Max(record.Affinity, 60f);
            record.Interactions = Mathf.Max(1, record.Interactions);
            record = _stages.Evaluate(from.Id, record);
            from.Relationships.Set(record);
        }
    }
}
