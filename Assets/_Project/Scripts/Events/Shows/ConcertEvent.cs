using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Events.Scheduling;

namespace Nimbo.Events.Shows
{
    /// <summary>
    /// El concierto en el escenario. Elige al isleño con más nivel entre los libres,
    /// reúne público, y al terminar reparte ánimo y afinidad. Quien más disfruta es
    /// quien tiene el eje <c>Expression</c> alto.
    /// </summary>
    public class ConcertEvent : System.IDisposable
    {
        readonly EventsConfig _config;
        readonly EventScheduler _scheduler;
        readonly Action<HourPassed> _onHour;

        string _performerId;
        readonly List<string> _audience = new List<string>();
        int _startHour;

        public ConcertEvent(EventsConfig config, EventScheduler scheduler)
        {
            _config = config;
            _scheduler = scheduler;
            _onHour = OnHourPassed;
            scheduler.EventStarted += OnEventStarted;
            scheduler.EventEnded += OnEventEnded;
            EventBus.Subscribe(_onHour);
        }

        public void Dispose()
        {
            _scheduler.EventStarted -= OnEventStarted;
            _scheduler.EventEnded -= OnEventEnded;
            EventBus.Unsubscribe(_onHour);
        }

        // ------------------------------------------------------------------- ciclo

        void OnEventStarted(EventDefinition def, int hour)
        {
            if (def.Id != "concert" && def.Id != "talent_show") return;
            StartConcert(hour);
        }

        void OnEventEnded(EventDefinition def)
        {
            if (def.Id != "concert" && def.Id != "talent_show") return;
            ResolveConcert();
        }

        void OnHourPassed(HourPassed evt)
        {
            // El concierto termina por duración máxima; el scheduler llama FinishActiveEvent.
            // Aquí solo escuchamos por si necesitamos lógica intra-evento.
        }

        // ------------------------------------------------------------------- lógica

        void StartConcert(int hour)
        {
            _startHour = hour;
            _performerId = null;
            _audience.Clear();

            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            if (registry == null) return;

            // Artista: el de más nivel entre los libres (que no estén durmiendo)
            int bestLevel = -1;
            foreach (var islander in registry.All)
            {
                if (islander == null) continue;
                if (islander.Progression.Level <= bestLevel) continue;
                _performerId = islander.Id;
                bestLevel = islander.Progression.Level;
            }

            if (_performerId == null) return;

            // Público: el resto de la isla, hasta el aforo máximo
            var rng = Rng.FromTime();
            var candidates = new List<IslanderData>();
            foreach (var islander in registry.All)
            {
                if (islander?.Id != _performerId) candidates.Add(islander);
            }
            rng.Shuffle(candidates);

            int taken = 0;
            foreach (var islander in candidates)
            {
                if (taken >= _config.ConcertMaxAudience) break;
                _audience.Add(islander.Id);
                taken++;
            }
        }

        void ResolveConcert()
        {
            var sim = ServiceRegistry.Get<ISimulationService>();
            var social = ServiceRegistry.Get<ISocialService>();
            var personality = ServiceRegistry.Get<IPersonalityService>();
            var registry = ServiceRegistry.Get<IIslanderRegistry>();

            if (sim == null || social == null || registry == null) return;

            // Ánimo al artista
            if (_performerId != null && registry.TryGet(_performerId, out var performer))
            {
                float performerBoost = _config.ConcertMoodBoost;
                // Los expresivos disfrutan más actuando
                if (performer.Personality.Expression > 0f)
                    performerBoost *= 1f + performer.Personality.Expression * 0.5f;
                sim.ApplyHappiness(_performerId, performerBoost);
                sim.ShowEmotion(_performerId, Emotion.Proud, 6f);
            }

            // Ánimo y afinidad al público
            float audienceBoost = _config.ConcertMoodBoost * 0.7f;
            foreach (var id in _audience)
            {
                if (!registry.TryGet(id, out var islander)) continue;
                float boost = audienceBoost;
                if (islander.Personality.Expression > 0f)
                    boost *= 1f + islander.Personality.Expression * 0.5f;
                sim.ApplyHappiness(id, boost);
                sim.ShowEmotion(id, Emotion.Happy, 4f);
            }

            // Afinidad entre asistentes (incluido el artista)
            var allIds = new List<string>(_audience);
            if (_performerId != null) allIds.Add(_performerId);

            for (int i = 0; i < allIds.Count; i++)
            for (int j = i + 1; j < allIds.Count; j++)
            {
                social.ApplyAffinity(allIds[i], allIds[j], _config.ConcertAffinityBoost);
            }

            _performerId = null;
            _audience.Clear();
        }
    }
}
