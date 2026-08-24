using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
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

            if (VaALaFiesta(islander)) return;

            WanderByPersonality(islander);
        }

        /// <summary>
        /// Si hay fiesta puesta y a este no le urge nada, se acerca a donde pasa.
        /// </summary>
        /// <remarks>
        /// Es lo que llena la plaza, y sin ello un festival de 1.200 monedas cambiaba la
        /// música y el decorado y **no cambiaba a nadie**: el sorteo de encuentros es por
        /// zona (<see cref="RunChanceEncounters"/>), así que la gente que sigue de paseo
        /// por la tienda ni se cruza. La fiesta se pagaba y no daba público.
        ///
        /// Va **al final** de la escalera y no antes: el que tiene sueño se va a dormir
        /// aunque haya verbena, y el que está muerto de hambre come. Una fiesta que
        /// atropella las necesidades convierte a los vecinos en figurantes.
        ///
        /// Y comprueba que la zona esté abierta aunque el planificador no deje empezar un
        /// evento con su zona cerrada. Este repo ya se comió una vez tres eventos que
        /// pedían una zona inexistente y por eso no ocurrían nunca, sin excepción ni
        /// aviso: cuando el coste de preguntar es una llamada, se pregunta.
        ///
        /// **Lo que esto deja cojo, dicho aquí para que no se pierda.**
        /// <c>AgendaProjection.Haunt</c> (Nimbo.UI) proyecta el día del vecino copiando la
        /// precedencia de <see cref="WanderByPersonality"/>, y ahora hay un escalón por
        /// encima que no conoce: con fiesta puesta la ficha sigue diciendo que le
        /// encontrarás en su sitio de siempre y estará en la plaza. No es una costura
        /// rota —nadie referencia un nombre que no existe— sino una ficha que se queda
        /// desfasada las horas que dura un festival, que es de las tres o cuatro veces al
        /// mes. Se arregla leyendo <c>ActiveEventZoneId</c> también desde la agenda, y eso
        /// es carril de quien lleve la ficha, no de aquí.
        /// </remarks>
        private bool VaALaFiesta(IslanderData islander)
        {
            if (!ServiceRegistry.TryGet<IVillageEvents>(out var fiestas)) return false;

            string zona = fiestas.ActiveEventZoneId;
            if (string.IsNullOrEmpty(zona)) return false;
            if (!_island.IsUnlocked(zona)) return false;

            _island.SendTo(islander.Id, zona);
            islander.Activity = IslanderActivity.Socializing;
            return true;
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

            // Por gravedad y no por el número del enum: la rivalidad se añadió al final
            // para no renumerar las partidas guardadas.
            if (record.Conflict.IsSerious())
                return _rng.Chance(0.7f) ? SocialInteraction.Argue : SocialInteraction.Apologize;

            // Dos rivales no se pelean a gritos: se aguantan. Casi siempre se ignoran, y
            // de vez en cuando uno da el paso — que es la única forma que tienen de
            // salir de ahí, porque la rivalidad no se cura sola (§13.2).
            if (record.Conflict == ConflictStage.Rivalry)
                return _rng.Chance(0.25f) ? SocialInteraction.Apologize : SocialInteraction.Ignore;

            if (record.Friendship >= FriendshipStage.Friend)
                return _rng.Chance(0.5f) ? SocialInteraction.Joke : SocialInteraction.Chat;

            return SocialInteraction.Chat;
        }
    }
}
