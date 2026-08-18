using System;
using System.Collections.Generic;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// Un evento posible en la isla: qué, cuándo y con qué requisitos.
    /// Los datos los rellena <see cref="EventCalendar"/>; el planificador
    /// solo lee.
    /// </summary>
    public class EventDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;

        /// <summary>Hora más temprana en que puede arrancar (0-23).</summary>
        public readonly int StartHour;

        /// <summary>Hora más tardía (exclusive). Un evento de 18-22 puede arrancar
        /// a las 18, 19, 20 o 21.</summary>
        public readonly int EndHour;

        /// <summary>"monday".."sunday", o "any".</summary>
        public readonly string DayOfWeek;

        /// <summary>0 = sin requisito.</summary>
        public readonly int MinIslanders;

        public readonly int MinIslandLevel;

        /// <summary>Zona que tiene que estar desbloqueada, o null.</summary>
        public readonly string RequiredZone;

        /// <summary>Peso relativo en el sorteo. Más alto = más frecuente.</summary>
        public readonly float Weight;

        /// <summary>Si es true, no se sortea: se dispara cuando su condición se cumple
        /// (cumpleaños, puente, etc.).</summary>
        public readonly bool IsTriggered;

        /// <summary>
        /// Si es una fiesta grande. Solo cambia lo que cuesta organizarla y qué nivel
        /// de Aldea pide (§15.3).
        /// </summary>
        /// <remarks>
        /// Escrito en la ficha y no deducido de los requisitos: «pide cinco vecinos» y
        /// «es un festival» se parecen hoy por casualidad, y el día que alguien añada un
        /// evento pequeño para seis vecinos dejarían de parecerse sin que nadie lo note.
        /// </remarks>
        public readonly bool IsFestival;

        public EventDefinition(
            string id, string displayName, string description,
            int startHour, int endHour, string dayOfWeek,
            int minIslanders, int minIslandLevel, string requiredZone,
            float weight, bool isTriggered = false, bool isFestival = false)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            StartHour = startHour;
            EndHour = endHour;
            DayOfWeek = dayOfWeek ?? "any";
            MinIslanders = minIslanders;
            MinIslandLevel = minIslandLevel;
            RequiredZone = requiredZone;
            Weight = weight;
            IsTriggered = isTriggered;
            IsFestival = isFestival;
        }

        /// <summary>
        /// ¿Cae esa hora dentro de la ventana del evento?
        /// </summary>
        /// <remarks>
        /// La ventana puede cruzar la medianoche, y tiene que poder: un sueño
        /// compartido va de 22:00 a 06:00, y con la comparación ingenua
        /// (<c>hora >= inicio y hora &lt; fin</c>) esa ventana no la cumple ninguna hora.
        /// </remarks>
        public bool IsWithinWindow(int hour) =>
            EndHour > StartHour
                ? hour >= StartHour && hour < EndHour
                : hour >= StartHour || hour < EndHour;

    }
}
