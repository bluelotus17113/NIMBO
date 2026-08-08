using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Events.Scheduling;

namespace Nimbo.Events.Dreams
{
    /// <summary>
    /// El sueño nocturno de un habitante. Durante la noche, un isleño dormido sueña
    /// con alguien de su agenda: su flechazo, su mejor amigo o con quien está reñido.
    /// Al despertar, la afinidad con esa persona se mueve.
    /// Los soñadores (Outlook alto) sueñan más a menudo.
    /// </summary>
    public class DreamEvent : IDisposable
    {
        readonly EventsConfig _config;
        readonly Action<HourPassed> _onHour;
        readonly Action<DayPassed> _onDay;

        int _dreamsTonight;
        readonly HashSet<string> _dreamersTonight = new HashSet<string>();

        public DreamEvent(EventsConfig config)
        {
            _config = config;
            _onHour = OnHourPassed;
            _onDay = OnDayPassed;
            EventBus.Subscribe(_onHour);
            EventBus.Subscribe(_onDay);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(_onHour);
            EventBus.Unsubscribe(_onDay);
        }

        // ------------------------------------------------------------------- ciclo

        void OnDayPassed(DayPassed evt)
        {
            _dreamsTonight = 0;
            _dreamersTonight.Clear();
        }

        void OnHourPassed(HourPassed evt)
        {
            // Solo durante la noche (23h a 5h)
            if (evt.Hour < 23 && evt.Hour > 5) return;
            if (_dreamsTonight >= 2) return; // máximo 2 sueños por noche

            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            if (registry == null) return;

            var rng = Rng.FromTime();
            var candidates = new List<IslanderData>();
            foreach (var islander in registry.All)
            {
                if (islander == null) continue;
                if (_dreamersTonight.Contains(islander.Id)) continue;

                float chance = _config.DreamChancePerNight / 6f; // repartido entre ~6 horas
                if (islander.Personality.Outlook > 0f)
                    chance *= _config.DreamerMultiplier;

                if (rng.Chance(chance))
                    candidates.Add(islander);
            }

            if (candidates.Count == 0) return;

            var dreamer = rng.Pick(candidates);
            if (dreamer == null) return;

            string targetId = PickDreamTarget(dreamer, rng);
            if (targetId == null) return;

            ProcessDream(dreamer.Id, targetId);
            _dreamersTonight.Add(dreamer.Id);
            _dreamsTonight++;
        }

        // ------------------------------------------------------------------- lógica

        string PickDreamTarget(IslanderData dreamer, Rng rng)
        {
            var social = ServiceRegistry.Get<ISocialService>();
            if (social == null) return null;

            // Categorías de la agenda social del soñador
            var friends = new List<RelationshipRecord>();
            var conflicts = new List<RelationshipRecord>();
            string partnerId = social.PartnerOf(dreamer.Id);

            foreach (var rel in social.FriendsOf(dreamer.Id))
                if (!string.IsNullOrEmpty(rel.OtherId) && rel.OtherId != dreamer.Id) friends.Add(rel);

            foreach (var rel in social.ConflictsOf(dreamer.Id))
                if (!string.IsNullOrEmpty(rel.OtherId) && rel.OtherId != dreamer.Id) conflicts.Add(rel);

            // Elegir categoría: 40% flechazo/pareja, 35% mejor amigo, 25% rival
            float roll = rng.NextFloat();
            if (partnerId != null && roll < 0.40f) return partnerId;
            if (friends.Count > 0 && roll < 0.75f)
                return friends[rng.Range(0, friends.Count)].OtherId;
            if (conflicts.Count > 0) return conflicts[rng.Range(0, conflicts.Count)].OtherId;
            if (partnerId != null) return partnerId;
            if (friends.Count > 0) return friends[0].OtherId;
            return null;
        }

        void ProcessDream(string dreamerId, string targetId)
        {
            var sim = ServiceRegistry.Get<ISimulationService>();
            var social = ServiceRegistry.Get<ISocialService>();

            if (sim == null || social == null) return;

            // El tono del sueño depende de la relación actual
            var rel = social.GetRelationship(dreamerId, targetId);
            float delta = _config.DreamAffinityDelta;

            if (rel.Affinity < -20)
                delta = -delta; // pesadilla con un rival

            social.ApplyAffinity(dreamerId, targetId, delta);
            sim.ShowEmotion(dreamerId, Emotion.Surprised, 5f);
        }
    }
}
