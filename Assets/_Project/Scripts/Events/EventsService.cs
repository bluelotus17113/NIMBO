using System;
using Nimbo.Events.Dreams;
using Nimbo.Events.News;
using Nimbo.Events.Scheduling;
using Nimbo.Events.Shows;
using UnityEngine;

namespace Nimbo.Events
{
    /// <summary>
    /// Fachada del módulo de eventos. Construye y cablea el planificador, el concierto,
    /// los sueños y el tablón de noticias. Un <c>MonoBehaviour</c> de arranque la crea
    /// y la registra donde corresponda.
    ///
    /// Implementa <see cref="IDisposable"/> para que al recargar la partida se desuscriba
    /// todo junto y no queden suscriptores zombi.
    /// </summary>
    public class EventsService : IDisposable
    {
        readonly EventsConfig _config;
        readonly EventScheduler _scheduler;
        readonly ConcertEvent _concert;
        readonly DreamEvent _dreams;
        readonly NewsBoard _news;

        public EventScheduler Scheduler => _scheduler;
        public NewsBoard News => _news;

        public EventsService() : this(LoadConfig()) { }

        public EventsService(EventsConfig config)
        {
            _config = config ? config : ScriptableObject.CreateInstance<EventsConfig>();

            _scheduler = new EventScheduler(_config);
            _concert = new ConcertEvent(_config, _scheduler);
            _dreams = new DreamEvent(_config);
            _news = new NewsBoard(_config);

            // El scheduler avisa al concierto cuando un evento de espectáculo
            // arranca o termina. El concierto ya se suscribió en su constructor.
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
            _concert?.Dispose();
            _dreams?.Dispose();
            _news?.Dispose();
        }

        static EventsConfig LoadConfig()
        {
            var asset = Resources.Load<EventsConfig>("Config/EventsConfig");
            if (asset == null)
            {
                Debug.LogWarning("EventsService: no se encontró Resources/Config/EventsConfig.asset, usando defaults.");
                asset = ScriptableObject.CreateInstance<EventsConfig>();
            }
            return asset;
        }
    }
}
