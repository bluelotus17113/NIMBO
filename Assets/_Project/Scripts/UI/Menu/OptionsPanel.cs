using System;
using Nimbo.Core.Events;
using Nimbo.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// Los ajustes. Hoy son los tres volúmenes, y no hay más porque no hay más
    /// que ajustar: un juego amable no necesita veinte casillas.
    /// </summary>
    /// <remarks>
    /// Se usa desde el menú de inicio y desde la pausa, así que es una vista
    /// normal y no un <c>MonoBehaviour</c>: quien la enseñe la mete donde quiera.
    /// </remarks>
    public sealed class OptionsPanel
    {
        public VisualElement Root { get; }

        private readonly Action _onClose;

        public OptionsPanel(Action onClose)
        {
            _onClose = onClose;

            Root = UiTheme.Card("opciones");
            Root.style.width = 460;

            Root.Add(UiTheme.Title("Ajustes"));
            Root.Add(UiTheme.Body(
                "La isla suena sola. Si te molesta, bájala; se queda como la dejes.",
                soft: true));

            var spacer = new VisualElement { style = { height = 14 } };
            Root.Add(spacer);

            Root.Add(Volume("Música", AudioChannel.Music, UiTheme.Lavender));
            Root.Add(Volume("Efectos", AudioChannel.Sfx, UiTheme.Mint));
            Root.Add(Volume("Voces", AudioChannel.Voice, UiTheme.Peach));

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 18;

            var restore = UiTheme.Secondary("Restaurar", () =>
            {
                AudioPrefs.ResetToDefaults();
                RefreshSliders();
            });
            restore.style.marginRight = 8;
            buttons.Add(restore);

            var back = UiTheme.Action("Volver", Close);
            back.style.flexGrow = 1;
            buttons.Add(back);

            Root.Add(buttons);
        }

        private void Close()
        {
            // El disco se toca al salir y no en cada arrastre: escribir en cada
            // fotograma de un deslizador es lo que hace que se noten tirones al
            // moverlo, y el valor ya está aplicado en memoria mucho antes.
            AudioPrefs.Flush();
            _onClose?.Invoke();
        }

        private readonly Slider[] _sliders = new Slider[3];

        private VisualElement Volume(string label, AudioChannel channel, Color colour)
        {
            var row = new VisualElement();
            row.style.marginBottom = 14;

            var caption = new VisualElement();
            caption.style.flexDirection = FlexDirection.Row;
            caption.style.justifyContent = Justify.SpaceBetween;
            caption.style.alignItems = Align.Center;
            caption.style.marginBottom = 4;

            caption.Add(UiTheme.Body(label));

            var readout = UiTheme.Pill(Percent(AudioPrefs.Get(channel)), colour);
            readout.style.marginRight = 0;
            caption.Add(readout);
            row.Add(caption);

            var slider = new Slider(0f, 1f) { value = AudioPrefs.Get(channel) };
            slider.style.marginLeft = slider.style.marginRight = 0;
            slider.style.height = 24;

            // El deslizador de UI Toolkit viene con su aspecto de editor. Se le pinta
            // el carril y el tirador para que no desentone con el resto: es el único
            // control del juego que no está hecho a mano.
            var tracker = slider.Q(className: "unity-base-slider__tracker");
            if (tracker != null)
            {
                tracker.style.backgroundColor = UiTheme.InkFaint;
                tracker.style.height = 10;
                tracker.style.borderTopWidth = tracker.style.borderBottomWidth =
                    tracker.style.borderLeftWidth = tracker.style.borderRightWidth = 0;
                UiTheme.SetRadius(tracker, 5);
            }

            var dragger = slider.Q(className: "unity-base-slider__dragger");
            if (dragger != null)
            {
                dragger.style.backgroundColor = colour;
                dragger.style.width = dragger.style.height = 20;
                dragger.style.marginTop = -6;
                dragger.style.borderTopWidth = dragger.style.borderBottomWidth =
                    dragger.style.borderLeftWidth = dragger.style.borderRightWidth = 0;
                UiTheme.SetRadius(dragger, UiTheme.RadiusPill);
            }

            slider.RegisterValueChangedCallback(evt =>
            {
                AudioPrefs.Set(channel, evt.newValue);
                readout.text = Percent(evt.newValue);
            });

            _sliders[(int)channel] = slider;
            row.Add(slider);
            return row;
        }

        /// <summary>Vuelve a leer los ajustes. Hace falta tras «Restaurar».</summary>
        private void RefreshSliders()
        {
            for (int i = 0; i < _sliders.Length; i++)
                if (_sliders[i] != null) _sliders[i].value = AudioPrefs.Get((AudioChannel)i);
        }

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)} %";
    }
}
