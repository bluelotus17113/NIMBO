using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;

namespace Nimbo.Simulation.Requests
{
    /// <summary>
    /// La cola de peticiones del jugador: quién pide qué, cuánto aguanta sin respuesta
    /// y qué pasa cuando el jugador contesta o cuando no contesta.
    /// </summary>
    public sealed class RequestService : IRequestService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly ISimulationService _simulation;
        private readonly RequestGenerator _generator;
        private readonly RequestConfig _config;
        private readonly GameClock _clock;

        private readonly List<IslanderRequest> _open = new List<IslanderRequest>();
        private Rng _rng = Rng.FromTime();

        public RequestService(IIslanderRegistry registry, ISimulationService simulation,
                              RequestGenerator generator, RequestConfig config, GameClock clock)
        {
            _registry = registry;
            _simulation = simulation;
            _generator = generator;
            _config = config;
            _clock = clock;

            EventBus.Subscribe<HourPassed>(OnHourPassed);
        }

        public void Dispose() => EventBus.Unsubscribe<HourPassed>(OnHourPassed);

        public IReadOnlyList<IslanderRequest> Open => _open;
        public int OpenCount => _open.Count;

        private void OnHourPassed(HourPassed _)
        {
            ExpireOverdue();

            // Dos tiradas por hora, porque el diseño evalúa cada media hora y el reloj
            // solo avisa en punto. Dos tiradas independientes son exactamente eso.
            for (int pass = 0; pass < 2; pass++) GenerationPass();
        }

        private void GenerationPass()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                int openForHim = CountOpenFor(islander.Id);

                if (!_generator.ShouldGenerate(islander, openForHim, ref _rng)) continue;
                if (!_generator.PickKind(islander, out var kind, ref _rng)) continue;

                var request = _generator.Build(islander, kind, _clock.ElapsedMinutes, ref _rng);

                // Una petición sobre alguien que no existe no se puede resolver, así que
                // no llega a nacer.
                if (NeedsTarget(kind) && string.IsNullOrEmpty(request.TargetId)) continue;

                _open.Add(request);
                EventBus.Publish(new RequestRaised(request));
            }
        }

        private static bool NeedsTarget(RequestKind kind) => kind is
            RequestKind.SocialIntro or RequestKind.Complaint or RequestKind.Reconcile or
            RequestKind.Confession or RequestKind.Activity;

        /// <summary>
        /// Caduca lo que lleva demasiado tiempo esperando. Ignorar tiene coste de ánimo:
        /// es lo que convierte la cola en decisiones y no en una lista de tareas.
        /// </summary>
        private void ExpireOverdue()
        {
            long now = _clock.ElapsedMinutes;
            for (int i = _open.Count - 1; i >= 0; i--)
            {
                if (_open[i].ExpiresMinute > now) continue;

                var request = _open[i];
                _open.RemoveAt(i);

                _simulation.ApplyHappiness(request.IslanderId, -request.NeglectPenalty);
                _simulation.ShowEmotion(request.IslanderId, Emotion.Sad, 5f);
                EventBus.Publish(new RequestExpired(request.RequestId, request.IslanderId));
            }
        }

        private int CountOpenFor(string islanderId)
        {
            int count = 0;
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].IslanderId == islanderId) count++;
            return count;
        }

        // --- IRequestService -------------------------------------------------

        public bool TryGet(string requestId, out IslanderRequest request)
        {
            for (int i = 0; i < _open.Count; i++)
            {
                if (_open[i].RequestId != requestId) continue;
                request = _open[i];
                return true;
            }
            request = default;
            return false;
        }

        public IEnumerable<IslanderRequest> OpenFor(string islanderId)
        {
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].IslanderId == islanderId) yield return _open[i];
        }

        public bool Resolve(string requestId, string payloadId = null)
        {
            int index = IndexOf(requestId);
            if (index < 0) return false;

            var request = _open[index];
            _open.RemoveAt(index);

            _simulation.GrantExperience(request.IslanderId, request.ExperienceReward);
            _simulation.ApplyHappiness(request.IslanderId, HappinessGain(request.Priority));
            _simulation.ShowEmotion(request.IslanderId, Emotion.Ecstatic, 5f);

            EventBus.Publish(new RequestResolved(request.RequestId, request.IslanderId, true));
            return true;
        }

        /// <summary>Atender algo urgente alegra más que atender un capricho.</summary>
        private static float HappinessGain(RequestPriority priority) => priority switch
        {
            RequestPriority.Critical => 18f,
            RequestPriority.High => 12f,
            RequestPriority.Normal => 8f,
            _ => 5f,
        };

        public void Refuse(string requestId)
        {
            int index = IndexOf(requestId);
            if (index < 0) return;

            var request = _open[index];
            _open.RemoveAt(index);

            // Decir que no a la cara duele más que dejar que caduque: el habitante se
            // ha enterado. Es medio castigo más que ignorarla.
            _simulation.ApplyHappiness(request.IslanderId, -request.NeglectPenalty * 1.5f);
            _simulation.ShowEmotion(request.IslanderId, Emotion.Sad, 6f);

            EventBus.Publish(new RequestResolved(request.RequestId, request.IslanderId, false));
        }

        public IslanderRequest Raise(string islanderId, RequestKind kind, string targetId = null)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return default;

            var request = _generator.Build(islander, kind, _clock.ElapsedMinutes, ref _rng);
            if (!string.IsNullOrEmpty(targetId)) request.TargetId = targetId;

            _open.Add(request);
            EventBus.Publish(new RequestRaised(request));
            return request;
        }

        private int IndexOf(string requestId)
        {
            for (int i = 0; i < _open.Count; i++)
                if (_open[i].RequestId == requestId) return i;
            return -1;
        }

        /// <summary>Restaura la cola al cargar partida.</summary>
        public void LoadFrom(IEnumerable<IslanderRequest> saved)
        {
            _open.Clear();
            foreach (var request in saved)
                if (request.IsOpen) _open.Add(request);
        }
    }
}
