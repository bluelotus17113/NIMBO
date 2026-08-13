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
        private IEconomyService _economy;
        private int _picked = -1;

        public BagPanel()
        {
            Root = UiTheme.Card("mochila");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 520;

            Root.Add(UiTheme.Header("Lo que llevas", Hide));

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
            ServiceRegistry.TryGet(out _economy);

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

            // Diez píxeles más que antes. Con 74 y el nombre en tres líneas, «Semillas
            // de nimbocereza» se comía la primera línea y el hueco decía «de
            // nimbocereza», que no es nada.
            slot.style.width = slot.style.height = 84;
            slot.style.marginRight = slot.style.marginBottom = 6;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            slot.style.paddingLeft = slot.style.paddingRight = 4;

            // Todos los huecos en crema profundo. Los de la barra iban en crema claro
            // —el mismo color que el panel de detrás—, así que los diez primeros no se
            // veían: quedaba el nombre del objeto flotando sobre la nada. Cuál está a
            // mano lo dice su número, que va en melocotón en esos diez.
            slot.style.backgroundColor = picked ? UiTheme.Peach : UiTheme.CreamDeep;
            slot.style.overflow = Overflow.Hidden;
            UiTheme.SetRadius(slot, UiTheme.Radius);
            UiTheme.Animate(slot, 120);
            UiTheme.Hoverable(slot, picked ? UiTheme.Peach : UiTheme.CreamDeep,
                              picked ? UiTheme.Peach : UiTheme.CreamPress);

            if (stack.Quantity > 0)
            {
                var name = UiTheme.Body(NameOf(stack.CatalogId));
                name.style.fontSize = 10;
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

            // La tecla con la que se saca, en los diez que están a mano. Es lo que
            // distingue esa primera fila ahora que todos los huecos son del mismo color.
            if (inHotbar)
            {
                var key = UiTheme.Body(((index + 1) % 10).ToString());
                key.style.fontSize = 10;
                key.style.unityFontStyleAndWeight = FontStyle.Bold;
                key.style.color = UiTheme.PeachDeep;
                key.style.marginTop = 2;
                slot.Add(key);
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

        /// <summary>El nombre del catálogo, o el identificador aseado si no está.</summary>
        private string NameOf(string catalogId)
        {
            var item = _economy?.GetItem(catalogId);
            if (item != null) return item.DisplayName;

            int underscore = catalogId.IndexOf('_');
            return underscore >= 0 ? catalogId[(underscore + 1)..].Replace('_', ' ') : catalogId;
        }
    }
}
