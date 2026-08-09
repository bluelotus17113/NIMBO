using System;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// La pausa. Se abre con Escape y para el reloj de la isla.
    /// </summary>
    /// <remarks>
    /// Enseña el día y la hora en grande porque es lo que se pierde de vista: en un
    /// juego que avanza solo, lo primero que uno quiere saber al volver del baño es
    /// cuánto ha corrido sin él.
    ///
    /// Las dos salidas guardan antes. No hay «salir sin guardar» y no lo va a haber:
    /// aquí no se pierden partidas por pulsar mal.
    /// </remarks>
    public sealed class PausePanel
    {
        public VisualElement Root { get; }

        private readonly Action _onResume;
        private readonly Action _onOptions;
        private readonly Label _when;

        public PausePanel(Action onResume, Action onOptions)
        {
            _onResume = onResume;
            _onOptions = onOptions;

            Root = UiTheme.Card("pausa");
            Root.style.width = 380;

            Root.Add(UiTheme.Title("En pausa"));

            _when = UiTheme.Body("", soft: true);
            _when.style.fontSize = 15;
            _when.style.marginBottom = 16;
            Root.Add(_when);

            Root.Add(Wide(UiTheme.Action("Seguir jugando", () => _onResume?.Invoke())));
            Root.Add(Wide(UiTheme.Secondary("Ajustes", () => _onOptions?.Invoke())));
            Root.Add(Wide(UiTheme.Secondary("Guardar y volver al menú",
                () => EventBus.Publish(new ReturnToMenuRequested()))));
            Root.Add(Wide(UiTheme.Secondary("Guardar y salir",
                () => EventBus.Publish(new QuitRequested()))));

            var note = UiTheme.Body("La isla se guarda sola cada hora de juego.", soft: true);
            note.style.marginTop = 10;
            note.style.fontSize = 12;
            Root.Add(note);
        }

        /// <summary>Se llama al abrir: la hora hay que leerla en ese momento, no al montar.</summary>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<GameClock>(out var clock))
            {
                _when.text = "";
                return;
            }

            string phase = clock.Phase switch
            {
                DayPhase.Dawn => "amanece",
                DayPhase.Morning => "por la mañana",
                DayPhase.Afternoon => "por la tarde",
                DayPhase.Evening => "al caer el día",
                _ => "de noche",
            };

            string who = ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)
                ? $" · {registry.Count} habitantes"
                : "";

            _when.text = $"Día {clock.Day}, {clock.FormatClock()} — {phase}{who}";
        }

        private static Button Wide(Button button)
        {
            button.style.width = Length.Percent(100);
            button.style.marginBottom = 8;
            button.style.paddingTop = button.style.paddingBottom = 11;
            return button;
        }
    }
}
