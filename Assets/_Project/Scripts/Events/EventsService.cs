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

        /// <summary>
        /// Enciende el módulo. Con <paramref name="save"/> y <paramref name="clock"/> la
        /// crónica se guarda y sobrevive a cerrar el juego; sin ellos el tablón solo
        /// vive en memoria, que es lo que necesitan los tests.
        /// </summary>
        public EventsService(EventsConfig config,
                             Nimbo.Data.Save.SaveGame save = null,
                             Nimbo.Core.Time.GameClock clock = null)
        {
            // Sin configuración se busca el asset, y si tampoco está, valores por defecto.
            // Antes se saltaba el asset y se iba directo a los defaults, así que ajustar
            // el EventsConfig del proyecto no servía de nada por este camino.
            _config = config ? config : LoadConfig();

            _scheduler = new EventScheduler(_config);
            _concert = new ConcertEvent(_config, _scheduler);
            _dreams = new DreamEvent(_config);
            _news = new NewsBoard(_config, save, clock);

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
