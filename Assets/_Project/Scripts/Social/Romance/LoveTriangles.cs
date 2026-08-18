using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Social.Relationships;
using UnityEngine;

namespace Nimbo.Social.Romance
{
    /// <summary>
    /// Dos vecinos que quieren a la misma persona, y que ahora lo saben.
    /// </summary>
    /// <remarks>
    /// Podía pasar desde siempre y no pasaba nada: los dos suspiraban por su lado sin
    /// enterarse el uno del otro. Es la historia más jugosa que el sistema puede dar y
    /// se estaba tirando.
    ///
    /// Lo que hace un triángulo, en orden:
    ///
    /// 1. Al nacer un flechazo se mira si la persona ya recibía otro. Si lo hay, los
    ///    dos pretendientes se ponen en <see cref="ConflictStage.Rivalry"/>.
    /// 2. Cada día pierden afinidad entre ellos. No la pierden con la persona: no es
    ///    culpa suya.
    /// 3. A los seis días gana quien tenga más **afinidad más compatibilidad**. El que
    ///    gana empieza a salir con ella; el que pierde vuelve a nada, con el golpe de
    ///    desamor y unos días sin poder encapricharse de nadie.
    /// 4. La rivalidad **no se cura sola**: sigue ahí hasta que uno se disculpa.
    ///
    /// Lo importante del punto 3 es que el jugador no decide nada y aun así lo entiende:
    /// gana quien mejor se lleva con ella, que es lo que uno esperaría. Y el punto 4 es
    /// lo que hace que un triángulo deje poso en la aldea en vez de ser un suceso que
    /// se olvida al día siguiente.
    /// </remarks>
    public sealed class LoveTriangles
    {
        private readonly IIslanderRegistry _registry;
        private readonly IPersonalityService _personalities;
        private readonly ISimulationService _simulation;
        private readonly SocialConfig _config;
        private readonly List<LoveTriangle> _open;

        public LoveTriangles(IIslanderRegistry registry, IPersonalityService personalities,
                             ISimulationService simulation, SocialConfig config,
                             List<LoveTriangle> open)
        {
            _registry = registry;
            _personalities = personalities;
            _simulation = simulation;
            _config = config;
            _open = open ?? new List<LoveTriangle>();
        }

        public IReadOnlyList<LoveTriangle> Open => _open;

        /// <summary>¿Anda este vecino metido en alguno?</summary>
        public bool Involves(string islanderId)
        {
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].Involves(islanderId)) return true;
            return false;
        }

        // ── nacer ────────────────────────────────────────────────────────────

        /// <summary>
        /// Acaba de nacerle un flechazo a <paramref name="suitorId"/> por
        /// <paramref name="belovedId"/>: ¿había ya alguien?
        /// </summary>
        public void OnCrushBorn(string suitorId, string belovedId, int day)
        {
            string other = OtherSuitor(suitorId, belovedId);
            if (other == null) return;

            // El mismo triángulo no se abre dos veces. Puede pasar: el flechazo del
            // segundo nace un día y el del tercero otro, y ambos miran hacia atrás.
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].Is(belovedId, suitorId, other)) return;

            _open.Add(LoveTriangle.Between(belovedId, suitorId, other, day));

            SetRivalry(suitorId, other);
            SetRivalry(other, suitorId);

            _simulation.ShowEmotion(suitorId, Emotion.Surprised, 5f);
            _simulation.ShowEmotion(other, Emotion.Surprised, 5f);
        }

        /// <summary>
        /// Otro que también esté colado por esa persona, si lo hay.
        /// </summary>
        /// <remarks>
        /// Solo cuenta el flechazo que todavía no ha llegado a nada —<c>Crush</c> o
        /// <c>Confessed</c>—: quien ya sale con ella no es un rival, es la pareja, y eso
        /// es otra historia distinta que este proyecto no cuenta todavía.
        /// </remarks>
        private string OtherSuitor(string suitorId, string belovedId)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var candidate = all[i];
                if (candidate.Id == suitorId || candidate.Id == belovedId) continue;
                if (!candidate.Relationships.TryGet(belovedId, out var record)) continue;

                if (record.Romance is RomanceStage.Crush or RomanceStage.Confessed)
                    return candidate.Id;
            }
            return null;
        }

        // ── durar y resolverse ───────────────────────────────────────────────

        /// <summary>Pasa un día en todos los triángulos abiertos.</summary>
        /// <remarks>
        /// De atrás hacia delante porque los que se resuelven se van de la lista. Y se
        /// limpia antes de rozar: un triángulo al que le falta alguien —se ha ido de la
        /// isla, o ya está saliendo con otra persona— no tiene que restar afinidad un
        /// día más.
        /// </remarks>
        public void AdvanceDay(int day)
        {
            for (int i = _open.Count - 1; i >= 0; i--)
            {
                var triangle = _open[i];

                if (IsStale(triangle))
                {
                    _open.RemoveAt(i);
                    continue;
                }

                if (day - triangle.StartedDay >= _config.TriangleDays)
                {
                    Resolve(triangle, day);
                    _open.RemoveAt(i);
                    continue;
                }

                WearDown(triangle);
            }
        }

        /// <summary>Ya no hay triángulo: falta alguien o alguno dejó de estar colado.</summary>
        private bool IsStale(LoveTriangle triangle)
        {
            if (!_registry.TryGet(triangle.BelovedId, out _)) return true;
            return !StillPining(triangle.AId, triangle.BelovedId)
                || !StillPining(triangle.BId, triangle.BelovedId);
        }

        private bool StillPining(string suitorId, string belovedId) =>
            _registry.TryGet(suitorId, out var suitor)
            && suitor.Relationships.TryGet(belovedId, out var record)
            && record.Romance is RomanceStage.Crush or RomanceStage.Confessed;

        /// <summary>Un día más aguantándose. Se llevan un poco peor.</summary>
        private void WearDown(LoveTriangle triangle)
        {
            Nudge(triangle.AId, triangle.BId, _config.RivalryDailyAffinity);
            Nudge(triangle.BId, triangle.AId, _config.RivalryDailyAffinity);
        }

        /// <summary>
        /// Se acabó el plazo: gana quien mejor se lleve con ella.
        /// </summary>
        /// <remarks>
        /// Afinidad más compatibilidad por su peso. La compatibilidad entra porque si
        /// solo contara la afinidad ganaría siempre quien más veces se haya cruzado con
        /// ella, y eso premia el azar de por dónde pasean. Con las dos, gana el que uno
        /// habría dicho que le pega más.
        /// </remarks>
        private void Resolve(LoveTriangle triangle, int day)
        {
            float scoreA = ScoreOf(triangle.AId, triangle.BelovedId);
            float scoreB = ScoreOf(triangle.BId, triangle.BelovedId);

            // Empate a la letra: gana el primero por orden, que es tan arbitrario como
            // cualquier otra cosa y al menos es estable entre cargas.
            string winner = scoreA >= scoreB ? triangle.AId : triangle.BId;
            string loser = winner == triangle.AId ? triangle.BId : triangle.AId;

            Win(winner, triangle.BelovedId);
            Lose(loser, triangle.BelovedId, day);
        }

        private float ScoreOf(string suitorId, string belovedId)
        {
            if (!_registry.TryGet(suitorId, out var suitor)) return float.MinValue;
            if (!_registry.TryGet(belovedId, out var beloved)) return float.MinValue;
            if (!suitor.Relationships.TryGet(belovedId, out var record)) return float.MinValue;

            int bias = _personalities.CompatibilityBetween(
                suitor.Personality.TypeIndex, beloved.Personality.TypeIndex);
            float compatibility = Compatibility.Full(suitor.Personality, beloved.Personality, bias);

            return record.Affinity + compatibility * _config.RivalryCompatibilityWeight;
        }

        /// <summary>
        /// El que gana empieza a salir con ella, y ella con él.
        /// </summary>
        /// <remarks>
        /// Se escribe en las dos agendas porque salir es cosa de dos: dejarlo en una
        /// sola daría una pareja que solo existe para uno de los dos, y el evaluador de
        /// romance la desharía a la mañana siguiente.
        /// </remarks>
        private void Win(string winnerId, string belovedId)
        {
            SetRomance(winnerId, belovedId, RomanceStage.Dating);
            SetRomance(belovedId, winnerId, RomanceStage.Dating);

            _simulation.ShowEmotion(winnerId, Emotion.Love, 8f);
        }

        private void Lose(string loserId, string belovedId, int day)
        {
            SetRomance(loserId, belovedId, RomanceStage.None);
            Nudge(loserId, belovedId, _config.HeartbreakAffinity);

            if (_registry.TryGet(loserId, out var loser))
                loser.CrushBlockedUntilDay = day + _config.HeartbreakCooldownDays;

            _simulation.ShowEmotion(loserId, Emotion.Sad, 10f);
        }

        // ── escribir en las agendas ──────────────────────────────────────────

        /// <remarks>
        /// Con <c>GetOrCreate</c> y no con <c>TryGet</c>, y esto costó un test: dos
        /// vecinos pueden no tener ficha el uno del otro —no se han cruzado nunca— y con
        /// <c>TryGet</c> la escritura se perdía en silencio. Lo que se vio fue que el
        /// que ganaba el triángulo empezaba a salir con ella y **ella no salía con él**,
        /// porque el flechazo era de una sola dirección y ella nunca había apuntado
        /// nada sobre él. Una pareja escrita en una sola agenda la deshace el evaluador
        /// de romance a la mañana siguiente.
        /// </remarks>
        private void SetRivalry(string fromId, string toId)
        {
            if (!_registry.TryGet(fromId, out var islander)) return;

            var book = islander.Relationships;
            var record = book.GetOrCreate(toId);

            // Una riña de verdad no se rebaja a rivalidad: si ya se llevaban peor que
            // esto, lo que hay entre ellos sigue siendo lo peor de los dos.
            if (record.Conflict.Severity() > ConflictStage.Rivalry.Severity()) return;

            record.Conflict = ConflictStage.Rivalry;
            book.Set(record);

            EventBus.Publish(new ConflictStageChanged(fromId, toId, ConflictStage.Rivalry));
        }

        private void SetRomance(string fromId, string toId, RomanceStage stage)
        {
            if (!_registry.TryGet(fromId, out var islander)) return;

            var book = islander.Relationships;
            var record = book.GetOrCreate(toId);
            if (record.Romance == stage) return;

            record.Romance = stage;
            book.Set(record);

            EventBus.Publish(new RomanceStageChanged(fromId, toId, stage));
        }

        private void Nudge(string fromId, string toId, float delta)
        {
            if (!_registry.TryGet(fromId, out var islander)) return;

            var book = islander.Relationships;
            var record = book.GetOrCreate(toId);

            record.Affinity = Mathf.Clamp(record.Affinity + delta,
                                          RelationshipRecord.MinAffinity,
                                          RelationshipRecord.MaxAffinity);
            book.Set(record);
        }
    }
}
