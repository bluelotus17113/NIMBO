using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Simulation.Progression;
using UnityEngine;

namespace Nimbo.Simulation.Needs
{
    /// <summary>
    /// Hace bajar las necesidades y sube o baja el ánimo, una hora de juego cada vez.
    /// </summary>
    /// <remarks>
    /// Trabaja por horas y no por fotograma a propósito: así una partida que estuvo
    /// cerrada se pone al día con el mismo código que la que está corriendo, y no hay
    /// dos caminos que puedan discrepar.
    /// </remarks>
    public sealed class NeedsSimulator
    {
        private readonly NeedsConfig _config;
        private readonly IPersonalityService _personalities;

        public NeedsSimulator(NeedsConfig config, IPersonalityService personalities)
        {
            _config = config;
            _personalities = personalities;
        }

        /// <summary>Avanza una hora de juego a un habitante. Publica los cambios de banda.</summary>
        public void TickHour(IslanderData islander, float relationshipScore)
        {
            var behaviour = _personalities.For(islander.Personality);
            bool asleep = islander.Activity == IslanderActivity.Sleeping;

            for (int i = 0; i < NeedState.Count; i++)
            {
                var need = (NeedKind)i;
                float before = islander.Needs[need];

                float perHour = need == NeedKind.Energy && asleep
                    ? _config.SleepingEnergyDecay
                    : _config.DecayPerHour(need);

                float multiplier = _config.DecayMultiplier(
                    behaviour.NeedDecayMultiplier(need), islander.Personality.Energy);

                float after = before + perHour * multiplier;

                // Dormir no solo frena la pérdida de energía: la recupera.
                if (asleep && need == NeedKind.Energy) after = before + SleepRecovery(islander);

                ApplyNeed(islander, need, after - before);
            }

            ApplyCriticalPenalties(islander);
            UpdateMood(islander, relationshipScore);
        }

        private float SleepRecovery(IslanderData islander)
        {
            float baseRate = islander.Home.HasHome
                ? _config.SleepRecoveryOwnBed
                : _config.SleepRecoverySofa;

            return baseRate * _restMultiplier;
        }

        /// <summary>
        /// Lo pone el servicio de simulación cada día: el domingo se duerme mejor.
        /// Entra por fuera y no se consulta aquí para que el simulador siga sin
        /// depender del calendario y se pueda probar solo.
        /// </summary>
        public float RestMultiplierToday
        {
            get => _restMultiplier;
            set => _restMultiplier = Mathf.Max(0.1f, value);
        }

        private float _restMultiplier = 1f;

        /// <summary>
        /// Cambia una necesidad y avisa si cruzó de banda. Todo lo que toque necesidades
        /// pasa por aquí: es el único sitio donde puede nacer un <c>NeedBandChanged</c>.
        /// </summary>
        public void ApplyNeed(IslanderData islander, NeedKind need, float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;

            float before = islander.Needs[need];
            var bandBefore = NeedState.BandOf(before);

            islander.Needs[need] = before + delta;

            var bandAfter = NeedState.BandOf(islander.Needs[need]);
            if (bandAfter != bandBefore)
                EventBus.Publish(new NeedBandChanged(islander.Id, need, bandBefore, bandAfter));
        }

        /// <summary>Lo que cuesta tener una necesidad en rojo, más allá de la barra vacía.</summary>
        private void ApplyCriticalPenalties(IslanderData islander)
        {
            if (islander.Needs.Band(NeedKind.Hunger) == NeedBand.Critical)
                ApplyNeed(islander, NeedKind.Energy, _config.StarvingEnergyDrain);

            if (islander.Needs.Band(NeedKind.Social) == NeedBand.Critical)
                ShiftHappiness(islander, _config.LonelyMoodDrain);
        }

        /// <summary>
        /// El ánimo persigue a un objetivo en vez de saltar a él, para que una mala
        /// hora no borre una buena semana.
        /// </summary>
        private void UpdateMood(IslanderData islander, float relationshipScore)
        {
            float needsPart = islander.Needs.Normalized * _config.MoodNeedsWeight;
            float socialPart = Mathf.Clamp01(relationshipScore) * (1f - _config.MoodNeedsWeight);
            float target = (needsPart + socialPart) * MoodState.Max;

            var behaviour = _personalities.For(islander.Personality);
            float step = _config.MoodApproachPerHour * behaviour.MoodDecayMultiplier;

            float before = islander.Mood.Happiness;
            islander.Mood.Happiness = Mathf.MoveTowards(before, target, step);

            if (!Mathf.Approximately(before, islander.Mood.Happiness))
                EventBus.Publish(new HappinessChanged(islander.Id, before, islander.Mood.Happiness));
        }

        /// <summary>Empujón directo al ánimo: un regalo, una riña, una petición ignorada.</summary>
        public void ShiftHappiness(IslanderData islander, float delta)
        {
            float before = islander.Mood.Happiness;
            islander.Mood.Happiness = Mathf.Clamp(before + delta, MoodState.Min, MoodState.Max);
            if (!Mathf.Approximately(before, islander.Mood.Happiness))
                EventBus.Publish(new HappinessChanged(islander.Id, before, islander.Mood.Happiness));
        }
    }
}
