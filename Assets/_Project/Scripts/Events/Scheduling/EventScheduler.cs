using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Services;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// Se suscribe a <see cref="HourPassed"/> y <see cref="DayPassed"/>, sortea eventos
    /// cuando toca y los dispara. Nunca dos a la vez.
    /// </summary>
    public class EventScheduler : IDisposable
    {
        readonly EventsConfig _config;
        readonly Action<HourPassed> _onHour;
        readonly Action<DayPassed> _onDay;

        EventDefinition _activeEvent;
        int _activeStartedHour;
        int _cooldownUntilHour = -1;

        /// <summary>Se invoca cuando un evento arranca. Parámetros: definición, hora de juego.</summary>
        public event Action<EventDefinition, int> EventStarted;

        /// <summary>Se invoca cuando un evento termina (por duración máxima o resolución).</summary>
        public event Action<EventDefinition> EventEnded;

        public EventDefinition ActiveEvent => _activeEvent;

        public EventScheduler(EventsConfig config)
        {
            _config = config;
            _onHour = OnHourPassed;
            _onDay = OnDayPassed;
            EventBus.Subscribe(_onHour);
            EventBus.Subscribe(_onDay);
        }

        // ------------------------------------------------------------------- IDisposable

        public void Dispose()
        {
            EventBus.Unsubscribe(_onHour);
            EventBus.Unsubscribe(_onDay);
        }

        /// <summary>
        /// Pone un evento en marcha a mano, sin esperar al sorteo.
        /// </summary>
        /// <remarks>
        /// Es el gemelo de <c>RequestService.Raise</c> y existe por lo mismo: hay cosas
        /// que tienen que pasar cuando toca y no cuando el azar quiera —el tutorial, un
        /// suceso guionizado, una prueba que necesita un concierto puesto—. El sorteo
        /// normal no se toca; esto solo se salta la espera.
        ///
        /// No hace nada si ya hay otro en marcha: la regla de uno cada vez manda igual.
        /// </remarks>
        public bool TryStartEvent(string eventId, int hour)
        {
            if (_activeEvent != null) return false;

            var def = EventCalendar.ById(eventId);
            if (def == null) return false;

            StartEvent(def, hour);
            return true;
        }

        /// <summary>La lógica interna puede forzar el final del evento activo.</summary>
        public void FinishActiveEvent()
        {
            if (_activeEvent == null) return;
            var finished = _activeEvent;
            _activeEvent = null;
            _cooldownUntilHour = _activeStartedHour + _config.MaxEventDurationHours + _config.CooldownHours;
            EventEnded?.Invoke(finished);
        }

        // ------------------------------------------------------------------- suscripciones

        void OnHourPassed(HourPassed evt)
        {
            // Si hay evento activo y ya pasó su duración máxima, se cierra
            if (_activeEvent != null)
            {
                if (evt.Hour >= _activeStartedHour + _config.MaxEventDurationHours)
                    FinishActiveEvent();
                return; // nunca empieza otro mientras haya uno en marcha
            }

            // Respetar el cooldown
            if (evt.Hour < _cooldownUntilHour) return;

            // Intentar sortear uno nuevo
            TrySchedule(evt.Hour, evt.Day);
        }

        void OnDayPassed(DayPassed evt)
        {
            // Los cumpleaños se comprueban al pasar el día, no cada hora
            CheckBirthdays(evt.Day);
            // Los eventos disparados también
            CheckTriggered(evt.Day);
        }

        // ------------------------------------------------------------------- lógica

        void TrySchedule(int hour, int day)
        {
            var candidates = EligibleEvents(hour, day);
            if (candidates.Count == 0) return;

            var rng = Rng.FromTime();
            if (!rng.Chance(_config.BaseChancePerHour)) return;

            var weights = new List<float>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                weights.Add(candidates[i].Weight);

            int picked = rng.PickWeighted(weights);
            if (picked < 0) return;

            StartEvent(candidates[picked], hour);
        }

        List<EventDefinition> EligibleEvents(int hour, int day)
        {
            var dayName = DayName(day);
            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            var island = ServiceRegistry.Get<IIslandService>();

            int islanderCount = registry?.Count ?? 0;
            int islandLevel = island?.State.Level ?? 0;

            var result = new List<EventDefinition>();
            foreach (var def in EventCalendar.Schedulable())
            {
                // ventana horaria
                if (!def.IsWithinWindow(hour)) continue;
                // día de la semana (los eventos con endHour < startHour cruzan medianoche)
                if (def.DayOfWeek != "any" && def.DayOfWeek != dayName) continue;
                // requisitos
                if (def.MinIslanders > islanderCount) continue;
                if (def.MinIslandLevel > islandLevel) continue;
                if (!string.IsNullOrEmpty(def.RequiredZone) && (island == null || !island.IsUnlocked(def.RequiredZone)))
                    continue;

                result.Add(def);
            }
            return result;
        }

        void StartEvent(EventDefinition def, int hour)
        {
            _activeEvent = def;
            _activeStartedHour = hour;
            EventStarted?.Invoke(def, hour);
        }

        void CheckBirthdays(int day)
        {
            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            if (registry == null) return;

            string today = BirthdayString(day);
            foreach (var islander in registry.All)
            {
                if (islander?.Identity.Birthday == today)
                {
                    var def = EventCalendar.ById("birthday");
                    if (def != null && _activeEvent == null)
                    {
                        // Usamos la hora actual como 10am (media mañana) ya que DayPassed
                        // no trae hora. Lo metemos con un HourPassed simulado.
                        StartEvent(def, 10);
                    }
                    break; // solo un cumpleaños por día
                }
            }
        }

        void CheckTriggered(int day)
        {
            var island = ServiceRegistry.Get<IIslandService>();
            var registry = ServiceRegistry.Get<IIslanderRegistry>();

            foreach (var def in EventCalendar.Triggered())
            {
                if (def.Id == "birthday") continue; // ya chequeado aparte

                switch (def.Id)
                {
                    case "bridge_appears":
                        if (island != null && !island.IsUnlocked("zona_embarcadero")
                            && (registry?.Count ?? 0) >= 12 && (island.State.Level >= 10))
                        {
                            if (_activeEvent == null) StartEvent(def, 8);
                        }
                        break;

                    case "shared_dream":
                        // El sueño compartido lo dispara DreamEvent directamente,
                        // no el planificador. Aquí solo damos la oportunidad.
                        break;
                }
            }
        }

        // ------------------------------------------------------------------- helpers

        /// <summary>Convierte día de juego (1-index) a nombre de día.
        /// Día 1 = lunes.</summary>
        public static string DayName(int day)
        {
            return ((day - 1) % 7) switch
            {
                0 => "monday",
                1 => "tuesday",
                2 => "wednesday",
                3 => "thursday",
                4 => "friday",
                5 => "saturday",
                6 => "sunday",
                _ => "monday",
            };
        }

        /// <summary>Convierte día de juego a "MM-DD" para comparar cumpleaños.
        /// Día 1 = 1 de enero. Simplificación: todos los meses tienen 30 días.</summary>
        public static string BirthdayString(int day)
        {
            int d = ((day - 1) % 30) + 1;
            int m = ((day - 1) / 30) % 12 + 1;
            return $"{m:D2}-{d:D2}";
        }

        /// <summary>Calcula el día de juego para una fecha "MM-DD" dada.
        /// 1 de enero = día 1. Usa meses de 30 días para simplificar.</summary>
        public static int DayOfBirthday(string birthday, int currentDay)
        {
            if (string.IsNullOrEmpty(birthday) || birthday.Length != 5) return -1;
            var parts = birthday.Split('-');
            if (parts.Length != 2) return -1;
            if (!int.TryParse(parts[0], out int m) || !int.TryParse(parts[1], out int d)) return -1;
            return (m - 1) * 30 + d;
        }

        /// <summary>Comprueba si hoy es el cumpleaños de un habitante.</summary>
        public static bool IsBirthdayToday(IslanderData islander, int day)
        {
            if (islander == null) return false;
            int bd = DayOfBirthday(islander.Identity.Birthday, day);
            return bd == day;
        }
    }
}
