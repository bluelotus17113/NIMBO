using Nimbo.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El fundido a negro al cruzar una puerta, y el cartel de dónde has entrado.
    /// </summary>
    /// <remarks>
    /// El teletransporte es instantáneo; el fundido está para que no lo parezca. Sin
    /// él, entrar en una casa es un corte seco que se lee como un fallo — el mismo
    /// truco que usa cualquier juego con interiores, y cuesta veinte líneas.
    ///
    /// Va con tiempo sin escalar para que termine aunque el juego esté en pausa.
    /// </remarks>
    public sealed class DoorFade
    {
        private const float Seconds = 0.45f;

        public VisualElement Root { get; }

        private readonly Label _place;
        private float _elapsed = Seconds;

        public DoorFade()
        {
            Root = new VisualElement();
            Root.style.position = Position.Absolute;
            Root.style.left = Root.style.right = Root.style.top = Root.style.bottom = 0;
            Root.style.backgroundColor = new Color(0.09f, 0.07f, 0.10f, 1f);
            Root.style.alignItems = Align.Center;
            Root.style.justifyContent = Justify.Center;

            // Decorativo mientras se desvanece: si no, se traga los clics de todo lo
            // que hay debajo durante medio segundo cada vez que cruzas una puerta.
            Root.pickingMode = PickingMode.Ignore;
            Root.style.display = DisplayStyle.None;

            _place = new Label("");
            _place.style.color = UiTheme.Cream;
            _place.style.fontSize = 26;
            _place.style.unityFontStyleAndWeight = FontStyle.Bold;
            Root.Add(_place);
        }

        public void Subscribe()
        {
            EventBus.Subscribe<InteriorEntered>(OnEntered);
            EventBus.Subscribe<InteriorExited>(OnExited);
        }

        public void Unsubscribe()
        {
            EventBus.Unsubscribe<InteriorEntered>(OnEntered);
            EventBus.Unsubscribe<InteriorExited>(OnExited);
        }

        private void OnEntered(InteriorEntered evt)
        {
            _place.text = string.IsNullOrEmpty(evt.HomeKey) ? "Tu casa" : "Dentro";
            Begin();
        }

        private void OnExited(InteriorExited _)
        {
            _place.text = "";
            Begin();
        }

        private void Begin()
        {
            _elapsed = 0f;
            Root.style.display = DisplayStyle.Flex;
            Root.style.opacity = 1f;
        }

        /// <summary>Lo llama la interfaz cada fotograma, con el tiempo sin escalar.</summary>
        public void Tick(float unscaledDelta)
        {
            if (_elapsed >= Seconds) return;

            _elapsed += unscaledDelta;
            float t = Mathf.Clamp01(_elapsed / Seconds);
            Root.style.opacity = 1f - t;

            if (t < 1f) return;
            Root.style.display = DisplayStyle.None;
        }
    }
}
