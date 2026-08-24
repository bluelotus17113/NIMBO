using System;
using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// Los ajustes: los tres volúmenes, la pantalla —modo de ventana, resolución
    /// y escala de la interfaz— y la lista de teclas. Un juego amable no necesita
    /// veinte casillas, pero sí una con la que defenderse de una pantalla que no
    /// le encaja.
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

            // Lo guardado manda desde el arranque: el menú construye este panel en
            // su Start, así que la resolución y la escala de la sesión anterior se
            // aplican solas, sin que nadie del arranque tenga que saber de esto.
            DisplayPrefs.ApplySavedOnce();

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

            Root.Add(DisplaySection());

            Root.Add(KeysSection());

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 18;

            var restore = UiTheme.Secondary("Restaurar", () =>
            {
                AudioPrefs.ResetToDefaults();
                DisplayPrefs.ResetToDefaults();
                RefreshSliders();
                RefreshDisplayControls();
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
            DisplayPrefs.Flush();
            _onClose?.Invoke();
        }

        /// <summary>
        /// La lista de teclas. No hay una sola palabra escrita a mano: cada fila
        /// sale de <see cref="GameKeys.Listed"/>, que es también lo que leen los
        /// que atienden la pulsación. Cambiar una tecla cambia aquí solito.
        /// </summary>
        /// <remarks>
        /// Va en ajustes y no en un panel propio porque ajustes ya se abre desde
        /// dos sitios —el título y la pausa—, que son justo los dos momentos en
        /// que uno se pregunta «¿cómo era esto?».
        /// </remarks>
        private static VisualElement KeysSection()
        {
            var section = new VisualElement();
            section.style.marginTop = 16;

            section.Add(UiTheme.Title("Teclas"));

            foreach (var binding in GameKeys.Listed)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;
                row.AddToClassList("fila-tecla");

                row.Add(UiTheme.Body(binding.Action));

                var key = UiTheme.Pill(GameKeys.Name(binding.Key), UiTheme.Lavender);
                key.style.marginRight = 0;
                row.Add(key);

                section.Add(row);
            }

            return section;
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
            PaintSlider(slider, colour);

            slider.RegisterValueChangedCallback(evt =>
            {
                AudioPrefs.Set(channel, evt.newValue);
                readout.text = Percent(evt.newValue);
            });

            _sliders[(int)channel] = slider;
            row.Add(slider);
            return row;
        }

        /// <summary>
        /// El deslizador de UI Toolkit viene con su aspecto de editor. Se le pinta
        /// el carril y el tirador para que no desentone con el resto: es el único
        /// control del juego que no está hecho a mano — ahora son dos sitios,
        /// volúmenes y escala, y los dos salen de aquí.
        /// </summary>
        private static void PaintSlider(Slider slider, Color colour)
        {
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
        }

        // ── Pantalla: modo de ventana, resolución y escala ───────────────────
        //
        // La escala es la respuesta de fondo a las fuentes pequeñas del hotbar:
        // sube todo el panel de interfaz de golpe, layout incluido, porque divide
        // la referencia del PanelSettings compartido (ver DisplayPrefs).

        private DropdownField _modeDropdown;
        private DropdownField _resolutionDropdown;
        private Slider _scaleSlider;
        private Label _scaleReadout;
        private List<Vector2Int> _resolutions;
        private Vector2Int _chosen;

        /// <summary>Los dos modos que se ofrecen, en el orden del desplegable.</summary>
        /// <remarks>
        /// Dos y no tres: el exclusivo a pantalla completa pelea con los
        /// compositores modernos y pierde, y quien lo echa de menos es menos de
        /// quien se confunde con él.
        /// </remarks>
        private static readonly string[] ModeNames = { "Pantalla completa", "Ventana" };

        private VisualElement DisplaySection()
        {
            var section = new VisualElement();
            section.style.marginTop = 16;

            section.Add(UiTheme.Title("Pantalla"));

            _resolutions = DisplayPrefs.AvailableResolutions();

            // La marcada al abrir: la guardada si existe; si no, la de la ventana
            // tal cual está ahora, que es lo que el jugador espera ver elegido.
            _chosen = DisplayPrefs.Width > 0 && DisplayPrefs.Height > 0
                ? new Vector2Int(DisplayPrefs.Width, DisplayPrefs.Height)
                : new Vector2Int(Screen.width > 0 ? Screen.width : 1280,
                                 Screen.height > 0 ? Screen.height : 720);
            if (!_resolutions.Contains(_chosen)) _resolutions.Insert(0, _chosen);

            var modeRow = CaptionRow("Modo de ventana");
            _modeDropdown = Choice(ModeNames, IndexOfMode(DisplayPrefs.Mode));
            _modeDropdown.name = "modo-ventana";
            _modeDropdown.RegisterValueChangedCallback(_ =>
                DisplayPrefs.Set(_chosen.x, _chosen.y, ModeOf(_modeDropdown.index)));
            modeRow.Add(_modeDropdown);
            section.Add(modeRow);

            var resolutionRow = CaptionRow("Resolución");
            _resolutionDropdown = Choice(
                _resolutions.Select(r => $"{r.x} × {r.y}").ToList(),
                _resolutions.IndexOf(_chosen));
            _resolutionDropdown.name = "resolucion";
            _resolutionDropdown.RegisterValueChangedCallback(_ =>
            {
                if (_resolutionDropdown.index >= 0 && _resolutionDropdown.index < _resolutions.Count)
                    _chosen = _resolutions[_resolutionDropdown.index];
                DisplayPrefs.Set(_chosen.x, _chosen.y, ModeOf(_modeDropdown.index));
            });
            resolutionRow.Add(_resolutionDropdown);
            section.Add(resolutionRow);

            var scaleRow = new VisualElement();
            scaleRow.style.marginBottom = 14;

            var caption = new VisualElement();
            caption.style.flexDirection = FlexDirection.Row;
            caption.style.justifyContent = Justify.SpaceBetween;
            caption.style.alignItems = Align.Center;
            caption.style.marginBottom = 4;

            caption.Add(UiTheme.Body("Escala de la interfaz"));

            _scaleReadout = UiTheme.Pill(Percent(DisplayPrefs.Scale), UiTheme.Butter);
            _scaleReadout.style.marginRight = 0;
            caption.Add(_scaleReadout);
            scaleRow.Add(caption);

            _scaleSlider = new Slider(DisplayPrefs.MinScale, DisplayPrefs.MaxScale)
            {
                value = DisplayPrefs.Scale,
                name = "escala-interfaz",
            };
            _scaleSlider.style.marginLeft = _scaleSlider.style.marginRight = 0;
            _scaleSlider.style.height = 24;
            PaintSlider(_scaleSlider, UiTheme.Butter);

            _scaleSlider.RegisterValueChangedCallback(evt =>
            {
                DisplayPrefs.SetScale(evt.newValue);
                _scaleReadout.text = Percent(evt.newValue);
            });

            scaleRow.Add(_scaleSlider);
            section.Add(scaleRow);

            return section;
        }

        private static VisualElement CaptionRow(string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 10;
            row.AddToClassList("fila-pantalla");
            row.Add(UiTheme.Body(label));
            return row;
        }

        private static DropdownField Choice(IReadOnlyList<string> choices, int index)
        {
            var dropdown = new DropdownField(choices.ToList(), Mathf.Clamp(index, 0, choices.Count - 1));
            var s = dropdown.style;
            s.width = 200;
            s.backgroundColor = UiTheme.CreamDeep;
            s.color = UiTheme.Ink;
            s.borderTopWidth = s.borderBottomWidth =
                s.borderLeftWidth = s.borderRightWidth = 0;
            s.paddingLeft = s.paddingRight = 10;
            s.paddingTop = s.paddingBottom = 6;
            UiTheme.SetRadius(dropdown, UiTheme.Radius);
            return dropdown;
        }

        private static int IndexOfMode(FullScreenMode mode) => mode == FullScreenMode.Windowed ? 1 : 0;

        private static FullScreenMode ModeOf(int index) =>
            index == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        /// <summary>Vuelve a leer la pantalla tras «Restaurar», sin disparar callbacks.</summary>
        private void RefreshDisplayControls()
        {
            if (_modeDropdown != null)
                _modeDropdown.SetValueWithoutNotify(ModeNames[IndexOfMode(DisplayPrefs.Mode)]);

            if (_resolutionDropdown != null && _resolutions != null)
            {
                int w = DisplayPrefs.Width > 0 ? DisplayPrefs.Width : _chosen.x;
                int h = DisplayPrefs.Height > 0 ? DisplayPrefs.Height : _chosen.y;
                _chosen = new Vector2Int(w, h);
                _resolutionDropdown.SetValueWithoutNotify($"{w} × {h}");
            }

            if (_scaleSlider != null)
            {
                _scaleSlider.SetValueWithoutNotify(DisplayPrefs.Scale);
                _scaleReadout.text = Percent(DisplayPrefs.Scale);
            }
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
