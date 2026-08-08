using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using UnityEngine;

namespace Nimbo.Simulation.Behaviour
{
    /// <summary>
    /// Decide qué hace cada habitante y adónde va. Se ejecuta una vez por hora de juego.
    /// </summary>
    /// <remarks>
    /// El jugador no manda aquí, y eso es deliberado: en este género el jugador observa
    /// e interviene, no maneja. Si pudiera ordenar «vete a comer», las peticiones — que
    /// son el bucle central — dejarían de tener sentido.
    /// </remarks>
    public sealed class IslanderBrain
    {
        private readonly IIslanderRegistry _registry;
        private readonly IIslandService _island;
        private readonly IPersonalityService _personalities;
        private readonly ISocialService _social;
        private readonly GameClock _clock;

        private readonly List<string> _candidates = new List<string>(16);
        private Rng _rng = Rng.FromTime();

        public IslanderBrain(IIslanderRegistry registry, IIslandService island,
                             IPersonalityService personalities, ISocialService social,
                             GameClock clock)
        {
            _registry = registry;
            _island = island;
            _personalities = personalities;
            _social = social;
            _clock = clock;

            EventBus.Subscribe<HourPassed>(OnHourPassed);
        }

        public void Dispose() => EventBus.Unsubscribe<HourPassed>(OnHourPassed);

        private void OnHourPassed(HourPassed _)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++) Decide(all[i]);

            RunChanceEncounters();
        }

        /// <summary>
        /// Elige actividad y destino. El orden importa: lo urgente manda sobre lo que
        /// le apetezca, y solo cuando no hay nada urgente sale la personalidad.
        /// </summary>
        public void Decide(IslanderData islander)
        {
            var needs = islander.Needs;

            // Dormido sigue dormido hasta que amanezca o esté descansado.
            if (islander.Activity == IslanderActivity.Sleeping)
            {
                if (_clock.IsSleepingHours && needs.Energy < 90f) return;
                Wake(islander);
                return;
            }

            if (_clock.IsSleepingHours || needs.Band(NeedKind.Energy) == NeedBand.Critical)
            {
                GoHomeAndSleep(islander);
                return;
            }

            if (needs.Band(NeedKind.Hunger) <= NeedBand.Low)
            {
                Send(islander, ZonePurpose.Food, IslanderActivity.Eating);
                return;
            }

            if (needs.Band(NeedKind.Hygiene) == NeedBand.Critical)
            {
                GoHome(islander, IslanderActivity.Bathing);
                return;
            }

            if (needs.Band(NeedKind.Social) <= NeedBand.Low)
            {
                Send(islander, ZonePurpose.Social, IslanderActivity.Socializing);
                return;
            }

            WanderByPersonality(islander);
        }

        private void Wake(IslanderData islander)
        {
            islander.Activity = IslanderActivity.Idle;
            var behaviour = _personalities.For(islander.Personality);
            EventBus.Publish(new EmotionShown(islander.Id,
                behaviour.ReactTo(PersonalityReaction.WokenUp), 3f));
        }

        private void GoHomeAndSleep(IslanderData islander)
        {
            GoHome(islander, IslanderActivity.Sleeping);
        }

        private void GoHome(IslanderData islander, IslanderActivity activity)
        {
            if (islander.Home.HasHome) _island.SendTo(islander.Id, islander.Home.BuildingId);
            islander.Activity = activity;
        }

        /// <summary>
        /// Adónde va cuando no le urge nada: los sociables buscan gente, los soñadores
        /// el parque y los prácticos las tiendas. Es donde se ve la personalidad sin
        /// que el habitante diga una palabra.
        /// </summary>
        private void WanderByPersonality(IslanderData islander)
        {
            var p = islander.Personality;

            ZonePurpose wanted;
            if (p.Attitude > 0.3f) wanted = ZonePurpose.Social;
            else if (p.Outlook > 0.3f) wanted = ZonePurpose.Nature;
            else if (p.Energy > 0.3f) wanted = ZonePurpose.Leisure;
            else wanted = _rng.Chance(0.5f) ? ZonePurpose.Shopping : ZonePurpose.Home;

            // Un independiente que ya está acompañado prefiere irse a su casa.
            if (p.Attitude < -0.3f && CountCompany(islander) >= 2)
            {
                GoHome(islander, IslanderActivity.Idle);
                return;
            }

            Send(islander, wanted, IslanderActivity.Idle);
        }

        private int CountCompany(IslanderData islander)
        {
            int company = 0;
            foreach (var other in _registry.InZone(islander.CurrentZoneId))
                if (other.Id != islander.Id) company++;
            return company;
        }

        /// <summary>Lo manda a una zona abierta de ese tipo, o lo deja donde está si no hay.</summary>
        private void Send(IslanderData islander, ZonePurpose purpose, IslanderActivity activity)
        {
            _candidates.Clear();
            foreach (var zoneId in _island.ZoneIds)
            {
                if (!_island.IsUnlocked(zoneId)) continue;
                if (_island.PurposeOf(zoneId) != purpose) continue;
                _candidates.Add(zoneId);
            }

            if (_candidates.Count == 0)
            {
                islander.Activity = activity;
                return;
            }

            _island.SendTo(islander.Id, _candidates[_rng.Range(0, _candidates.Count)]);
            islander.Activity = activity;
        }

        /// <summary>
        /// Los que coinciden en una zona se hablan. Es de donde salen solas las
        /// amistades y las riñas sin que el jugador tenga que hacer nada.
        /// </summary>
        private void RunChanceEncounters()
        {
            foreach (var zoneId in _island.ZoneIds)
            {
                _candidates.Clear();
                foreach (var islander in _registry.InZone(zoneId))
                    if (islander.Activity != IslanderActivity.Sleeping)
                        _candidates.Add(islander.Id);

                if (_candidates.Count < 2) continue;

                // Un encuentro por zona y hora: más y la isla entera sería mejores
                // amigos en dos días.
                int a = _rng.Range(0, _candidates.Count);
                int b = _rng.Range(0, _candidates.Count - 1);
                if (b >= a) b++;

                _social.Interact(_candidates[a], _candidates[b], PickInteraction(_candidates[a],
                                                                                _candidates[b]));
            }
        }

        /// <summary>
        /// Qué se dicen al cruzarse. Depende de cómo se lleven ya: los que se llevan
        /// mal discuten y los que se llevan bien bromean.
        /// </summary>
        private SocialInteraction PickInteraction(string aId, string bId)
        {
            var record = _social.GetRelationship(aId, bId);

            if (record.Conflict >= ConflictStage.Quarrel)
                return _rng.Chance(0.7f) ? SocialInteraction.Argue : SocialInteraction.Apologize;

            if (record.Friendship >= FriendshipStage.Friend)
                return _rng.Chance(0.5f) ? SocialInteraction.Joke : SocialInteraction.Chat;

            return SocialInteraction.Chat;
        }
    }
}
