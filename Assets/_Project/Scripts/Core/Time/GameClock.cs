using System;
using Nimbo.Core.Events;
using UnityEngine;

namespace Nimbo.Core.Time
{
    public enum DayPhase { Dawn = 0, Morning = 1, Afternoon = 2, Evening = 3, Night = 4 }

    /// <summary>
    /// El reloj del juego. Lleva la cuenta en minutos de juego desde el día 1 y avisa
    /// al pasar cada hora y cada día.
    /// </summary>
    /// <remarks>
    /// Es una clase normal, no un MonoBehaviour: quien la hace avanzar es el bucle de
    /// <c>Nimbo.Game</c>. Así los tests pueden adelantar el reloj sin abrir una escena.
    /// </remarks>
    public sealed class GameClock
    {
        public const int MinutesPerHour = 60;
        public const int HoursPerDay = 24;
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;

        /// <summary>Minutos de juego por segundo real. 1 = un día de juego cada 24 minutos reales.</summary>
        public float MinutesPerRealSecond { get; set; } = 1f;

        public long ElapsedMinutes { get; private set; }

        private float _fraction;
        private bool _paused;

        public GameClock(long startMinutes = 8 * MinutesPerHour)
        {
            ElapsedMinutes = startMinutes;
        }

        public int Minute => (int)(ElapsedMinutes % MinutesPerHour);
        public int Hour => (int)(ElapsedMinutes / MinutesPerHour % HoursPerDay);
        public int Day => (int)(ElapsedMinutes / MinutesPerDay) + 1;

        public bool IsPaused => _paused;

        public DayPhase Phase => Hour switch
        {
            >= 5 and < 8 => DayPhase.Dawn,
            >= 8 and < 13 => DayPhase.Morning,
            >= 13 and < 19 => DayPhase.Afternoon,
            >= 19 and < 23 => DayPhase.Evening,
            _ => DayPhase.Night,
        };

        /// <summary>Franja horaria de dormir. Fuera de ella los habitantes no se van a la cama solos.</summary>
        public bool IsSleepingHours => Hour >= 23 || Hour < 7;

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;

        /// <summary>Avanza el reloj con el tiempo real de un fotograma.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_paused || deltaSeconds <= 0f) return;

            _fraction += deltaSeconds * MinutesPerRealSecond;
            if (_fraction < 1f) return;

            int whole = (int)_fraction;
            _fraction -= whole;
            Advance(whole);
        }

        /// <summary>
        /// Salta hacia adelante. Lo usa el arranque para poner al día una partida que
        /// estuvo cerrada, y los tests para no esperar.
        /// </summary>
        public void Advance(int minutes)
        {
            if (minutes <= 0) return;

            long before = ElapsedMinutes;
            ElapsedMinutes += minutes;

            // Los avisos se emiten hora a hora aunque el salto sea de días: los módulos
            // que acumulan (necesidades, relaciones) necesitan cada paso, no el total.
            long firstHour = before / MinutesPerHour + 1;
            long lastHour = ElapsedMinutes / MinutesPerHour;
            for (long h = firstHour; h <= lastHour; h++)
            {
                int hourOfDay = (int)(h % HoursPerDay);
                int day = (int)(h / HoursPerDay) + 1;
                EventBus.Publish(new HourPassed(hourOfDay, day));
                if (hourOfDay == 0) EventBus.Publish(new DayPassed(day));
            }
        }

        public void SetElapsed(long minutes)
        {
            ElapsedMinutes = Math.Max(0, minutes);
            _fraction = 0f;
        }

        public string FormatClock() => $"{Hour:00}:{Minute:00}";

        public override string ToString() => $"Día {Day}, {FormatClock()} ({Phase})";
    }
}
