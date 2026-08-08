using System;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Progression;
using UnityEngine;

namespace Nimbo.Simulation
{
    /// <summary>
    /// El latido del juego: cada hora que pasa, todos los habitantes tienen hambre,
    /// sueño y ganas de compañía un poco más que antes.
    /// </summary>
    /// <remarks>
    /// Es la única puerta para mover necesidades, ánimo y experiencia. Todo lo demás
    /// (economía, social, vivienda) le pide a este servicio que aplique el cambio en
    /// vez de escribir el número, y así los avisos de cruce de umbral salen siempre.
    /// </remarks>
    public sealed class SimulationService : ISimulationService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly NeedsSimulator _needs;
        private readonly ExperienceLedger _experience;

        private bool _paused;

        public SimulationService(IIslanderRegistry registry, IPersonalityService personalities,
                                 NeedsConfig config)
        {
            _registry = registry;
            _needs = new NeedsSimulator(config, personalities);
            _experience = new ExperienceLedger();

            EventBus.Subscribe<HourPassed>(OnHourPassed);
        }

        public void Dispose() => EventBus.Unsubscribe<HourPassed>(OnHourPassed);

        private void OnHourPassed(HourPassed _)
        {
            if (_paused) return;

            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                _needs.TickHour(all[i], RelationshipScoreOf(all[i]));
        }

        /// <summary>
        /// Cómo de acompañado se siente, de 0 a 1: la media de lo que le quieren los
        /// que ya conoce. Alguien recién llegado sin amigos empieza en 0.5 y no en 0,
        /// porque estar solo el primer día no debería contar como estar triste.
        /// </summary>
        private static float RelationshipScoreOf(IslanderData islander)
        {
            var records = islander.Relationships?.Records;
            if (records == null || records.Count == 0) return 0.5f;

            float sum = 0f;
            int counted = 0;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Friendship == FriendshipStage.Stranger) continue;
                sum += records[i].Affinity;
                counted++;
            }
            if (counted == 0) return 0.5f;

            // La afinidad va de -100 a 100 y aquí hace falta de 0 a 1.
            return Mathf.Clamp01((sum / counted + 100f) / 200f);
        }

        // --- ISimulationService --------------------------------------------

        public void ApplyNeed(string islanderId, NeedKind need, float delta)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            _needs.ApplyNeed(islander, need, delta);
        }

        public void SetNeed(string islanderId, NeedKind need, float value)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            _needs.ApplyNeed(islander, need, value - islander.Needs[need]);
        }

        public void ApplyHappiness(string islanderId, float delta)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            _needs.ShiftHappiness(islander, delta);
        }

        public void ShowEmotion(string islanderId, Emotion emotion, float seconds = 4f)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;

            islander.Mood.Emotion = emotion;
            islander.Mood.EmotionTimer = seconds;
            EventBus.Publish(new EmotionShown(islanderId, emotion, seconds));
        }

        public void GrantExperience(string islanderId, float amount)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            _experience.Grant(islander, amount);
        }

        public void SetSimulationPaused(bool paused) => _paused = paused;

        /// <summary>Lo llama el arranque cada día: el domingo se duerme mejor.</summary>
        public void SetRestMultiplier(float multiplier) => _needs.RestMultiplierToday = multiplier;

        // --- lo que corre por fotograma -------------------------------------

        /// <summary>
        /// Caduca las emociones. Es lo único que no puede ir por horas: una cara de
        /// sorpresa dura cuatro segundos, no una hora de juego.
        /// </summary>
        public void TickFrame(float deltaSeconds)
        {
            if (_paused) return;

            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                if (islander.Mood.EmotionTimer <= 0f) continue;

                islander.Mood.EmotionTimer -= deltaSeconds;
                if (islander.Mood.EmotionTimer > 0f) continue;

                islander.Mood.EmotionTimer = 0f;
                islander.Mood.Emotion = islander.Mood.BaselineEmotion;
            }
        }
    }
}
