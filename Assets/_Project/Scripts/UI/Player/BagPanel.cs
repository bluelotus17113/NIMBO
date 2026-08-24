using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// La mochila entera: los veinticuatro huecos, no solo los diez de la barra.
    /// </summary>
    /// <remarks>
    /// Se ordena pulsando dos huecos: el primero se marca y el segundo intercambia.
    /// No es arrastrar, que en UI Toolkit cuesta bastante y con el ratón sobre una
    /// rejilla de este tamaño no aporta nada — dos clics son igual de rápidos y no
    /// se te cae nada por soltar mal.
    /// </remarks>
    public sealed class BagPanel
    {
        private const int Columns = 6;

        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly VisualElement _grid;
        private readonly Label _hint;

        private IInventoryService _inventory;
        private int _picked = -1;

        public BagPanel()
        {
            Root = UiTheme.Card("mochila");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 520;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Lo que llevas"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _hint = UiTheme.Body("Los diez primeros huecos son los de la barra de abajo.",
                                 soft: true);
            _hint.style.marginBottom = 10;
            Root.Add(_hint);

            _grid = new VisualElement();
            _grid.style.flexDirection = FlexDirection.Row;
            _grid.style.flexWrap = Wrap.Wrap;
            Root.Add(_grid);
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _inventory)) return;

            _picked = -1;
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            _picked = -1;
        }

        public void Rebuild()
        {
            if (_inventory == null) return;

            _grid.Clear();
            for (int i = 0; i < _inventory.SlotCount; i++)
                _grid.Add(BuildSlot(i));
        }

        private VisualElement BuildSlot(int index)
        {
            var stack = _inventory.At(index);
            bool inHotbar = index < _inventory.HotbarSize;
            bool picked = index == _picked;

            var slot = new VisualElement();
            slot.style.width = slot.style.height = 74;
            slot.style.marginRight = slot.style.marginBottom = 6;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            slot.style.paddingLeft = slot.style.paddingRight = 4;

            // Los de la barra van en crema claro y el resto en el profundo: se ve de un
            // vistazo qué tienes a mano y qué está guardado al fondo.
            slot.style.backgroundColor = picked ? UiTheme.Peach
                                       : inHotbar ? UiTheme.Cream : UiTheme.CreamDeep;
            UiTheme.SetRadius(slot, UiTheme.Radius);

            if (stack.Quantity > 0)
            {
                // El nombre lo decide ItemNames, como el hotbar y el tablón: el mismo
                // objeto no puede llamarse distinto según la pantalla que lo pinta.
                var name = UiTheme.Body(ItemNames.Display(stack.CatalogId));
                name.style.fontSize = 11;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                name.style.whiteSpace = WhiteSpace.Normal;
                slot.Add(name);

                if (stack.Quantity > 1)
                {
                    var count = UiTheme.Pill(stack.Quantity.ToString(), UiTheme.Butter);
                    count.style.marginRight = 0;
                    count.style.marginTop = 3;
                    slot.Add(count);
                }
            }

            int captured = index;
            slot.RegisterCallback<ClickEvent>(_ => OnSlotClicked(captured));
            return slot;
        }

        private void OnSlotClicked(int index)
        {
            if (_picked < 0)
            {
                // Marcar un hueco vacío no hace nada: no hay nada que mover y dejaría
                // al jugador con una selección que no se entiende.
                if (_inventory.At(index).Quantity <= 0) return;

                _picked = index;
                _hint.text = "Ahora pulsa dónde lo quieres.";
                Rebuild();
                return;
            }

            if (_picked != index) _inventory.Swap(_picked, index);

            _picked = -1;
            _hint.text = "Los diez primeros huecos son los de la barra de abajo.";
            Rebuild();
        }
    }
}
