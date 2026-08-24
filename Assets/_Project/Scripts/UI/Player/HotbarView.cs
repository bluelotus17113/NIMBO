using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// La barra de abajo: lo que llevas a mano y qué tienes delante.
    /// </summary>
    /// <remarks>
    /// Se pinta entera solo cuando cambia algo, no cada fotograma: son diez huecos y
    /// reconstruirlos sesenta veces por segundo no cambia nada que se pueda ver.
    ///
    /// El aviso de lo que tienes delante va encima de la barra y no en el centro de la
    /// pantalla a propósito: en el centro tapa justo lo que estás mirando.
    /// </remarks>
    public sealed class HotbarView
    {
        public VisualElement Root { get; }

        private readonly VisualElement _slots;
        private readonly Label _prompt;
        private readonly VisualElement _vigorFill;

        private IInventoryService _inventory;
        private int _lastSelected = -1;

        public HotbarView()
        {
            Root = new VisualElement();
            Root.style.alignItems = Align.Center;
            Root.style.marginBottom = UiTheme.Gap;

            _prompt = UiTheme.Body("");
            _prompt.style.backgroundColor = UiTheme.Cream;
            _prompt.style.paddingLeft = _prompt.style.paddingRight = 14;
            _prompt.style.paddingTop = _prompt.style.paddingBottom = 6;
            _prompt.style.marginBottom = 6;
            _prompt.style.unityFontStyleAndWeight = FontStyle.Bold;
            UiTheme.SetRadius(_prompt, UiTheme.RadiusPill);
            _prompt.style.display = DisplayStyle.None;
            Root.Add(_prompt);

            _slots = new VisualElement();
            _slots.style.flexDirection = FlexDirection.Row;
            _slots.style.backgroundColor = UiTheme.Cream;
            _slots.style.paddingLeft = _slots.style.paddingRight = 8;
            _slots.style.paddingTop = _slots.style.paddingBottom = 8;
            UiTheme.SetRadius(_slots, UiTheme.RadiusCard);
            Root.Add(_slots);

            Root.Add(BuildVigor(out _vigorFill));
        }

        private static VisualElement BuildVigor(out VisualElement fill)
        {
            var track = new VisualElement();
            track.style.width = 220;
            track.style.height = 8;
            track.style.marginTop = 6;
            track.style.backgroundColor = UiTheme.InkFaint;
            track.style.overflow = Overflow.Hidden;
            UiTheme.SetRadius(track, 4);

            fill = new VisualElement();
            fill.style.height = 8;
            fill.style.width = Length.Percent(100);
            fill.style.backgroundColor = UiTheme.Mint;
            UiTheme.SetRadius(fill, 4);
            track.Add(fill);
            return track;
        }

        public void Subscribe()
        {
            EventBus.Subscribe<InventoryChanged>(OnInventoryChanged);
            EventBus.Subscribe<SlotSelected>(OnSlotSelected);
            EventBus.Subscribe<VigorChanged>(OnVigorChanged);
        }

        public void Unsubscribe()
        {
            EventBus.Unsubscribe<InventoryChanged>(OnInventoryChanged);
            EventBus.Unsubscribe<SlotSelected>(OnSlotSelected);
            EventBus.Unsubscribe<VigorChanged>(OnVigorChanged);
        }

        private void OnInventoryChanged(InventoryChanged _) => Rebuild();
        private void OnSlotSelected(SlotSelected _) => Rebuild();

        private void OnVigorChanged(VigorChanged evt)
        {
            float fraction = Mathf.Clamp01(evt.Vigor / 100f);
            _vigorFill.style.width = Length.Percent(fraction * 100f);

            // Cambia de color al agotarse, que es más legible de un vistazo que la
            // longitud: con la barra a un cuarto y el mismo verde, nadie la mira.
            _vigorFill.style.backgroundColor =
                fraction > 0.5f ? UiTheme.Mint :
                fraction > 0.2f ? UiTheme.Butter : UiTheme.Rose;
        }

        /// <summary>Lo llama la interfaz. Enseña lo que el jugador tiene delante.</summary>
        public void SetPrompt(string text)
        {
            if (_prompt.text == text) return;

            _prompt.text = text ?? "";
            _prompt.style.display = string.IsNullOrEmpty(text)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Rebuild()
        {
            if (_inventory == null && !ServiceRegistry.TryGet(out _inventory)) return;

            _slots.Clear();
            _lastSelected = _inventory.SelectedSlot;

            for (int i = 0; i < _inventory.HotbarSize; i++)
                _slots.Add(BuildSlot(i, _inventory.At(i)));
        }

        private VisualElement BuildSlot(int index, Data.Economy.ItemStack stack)
        {
            bool selected = index == _inventory.SelectedSlot;

            var slot = new VisualElement();
            slot.style.width = slot.style.height = 54;
            slot.style.marginLeft = slot.style.marginRight = 3;
            slot.style.backgroundColor = selected ? UiTheme.Peach : UiTheme.CreamDeep;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            UiTheme.SetRadius(slot, UiTheme.Radius);

            if (stack.Quantity > 0)
            {
                // Sin iconos: el nombre corto del objeto. Es feo comparado con un
                // dibujo, pero un icono equivocado miente y un texto no. Sale de
                // ItemNames y no de recortar el id: la misma voz que la mochila.
                var name = UiTheme.Body(ItemNames.Short(stack.CatalogId));
                name.style.fontSize = 10;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                name.style.whiteSpace = WhiteSpace.Normal;
                slot.Add(name);

                if (stack.Quantity > 1)
                {
                    var count = UiTheme.Body(stack.Quantity.ToString());
                    count.style.fontSize = 11;
                    count.style.unityFontStyleAndWeight = FontStyle.Bold;
                    slot.Add(count);
                }
            }

            var number = UiTheme.Body(((index + 1) % 10).ToString(), soft: true);
            number.style.fontSize = 9;
            slot.Add(number);

            int captured = index;
            slot.RegisterCallback<ClickEvent>(_ => _inventory.Select(captured));
            return slot;
        }

        /// <summary>Las teclas 1-0 eligen hueco. Sistema de entrada antiguo.</summary>
        public void Tick()
        {
            if (_inventory == null) return;

            for (int i = 0; i < 10; i++)
            {
                var key = i == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key)) _inventory.Select(i);
            }

            // Red de seguridad: si algo cambió la selección sin publicar el aviso, la
            // barra se entera igual en vez de quedarse marcando el hueco que no es.
            if (_inventory.SelectedSlot != _lastSelected) Rebuild();
        }
    }
}
