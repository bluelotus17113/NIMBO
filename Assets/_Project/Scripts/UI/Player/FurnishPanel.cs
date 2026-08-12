using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El menú de amueblar: qué mueble estás colocando dentro de la casa.
    /// </summary>
    /// <remarks>
    /// Solo enseña lo que de verdad se puede colocar. El editor viejo listaba todo lo
    /// que el inventario llamaba «colocable» —papeles pintados y suelos incluidos—, y
    /// elegir uno solo servía para leer «ese mueble no existe» después de haber
    /// apuntado a una casilla: son acabados, no objetos, y no están en el catálogo de
    /// muebles.
    /// </remarks>
    public sealed class FurnishPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        /// <summary>Lo que hay elegido. Vacío para no colocar nada.</summary>
        public string Selected { get; private set; } = "";

        private readonly ScrollView _list;
        private readonly Label _hint;

        private IEconomyService _economy;
        private IHousingService _housing;

        public FurnishPanel()
        {
            Root = UiTheme.Card("amueblar");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 280;
            Root.style.maxHeight = Length.Percent(88);

            Root.Add(UiTheme.Title("Amueblar"));
            Root.Add(UiTheme.Body("Elige un mueble y pincha en la casilla. " +
                                  "Botón derecho lo recoge, R lo gira.", soft: true));

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            _list.style.marginTop = 8;
            Root.Add(_list);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 6;
            _hint.style.minHeight = 18;
            Root.Add(_hint);

            var done = UiTheme.Action("Terminar", () => EventBus.Publish(new FurnishModeChanged(false)));
            done.style.marginTop = 8;
            Root.Add(done);
        }

        /// <summary>
        /// Se entera de cuándo el modo suelta lo que llevabas.
        /// </summary>
        /// <remarks>
        /// Al colocar el último de una pila, quien coloca lo suelta. Sin escuchar eso,
        /// el menú seguiría enseñando esa fila marcada como elegida cuando ya no queda
        /// ninguno, que es la clase de mentira pequeña que hace dudar de todo lo demás.
        /// </remarks>
        public void Subscribe() => EventBus.Subscribe<FurnishSelectionChanged>(OnSelectionChanged);

        public void Unsubscribe() => EventBus.Unsubscribe<FurnishSelectionChanged>(OnSelectionChanged);

        private void OnSelectionChanged(FurnishSelectionChanged evt)
        {
            string incoming = evt.CatalogId ?? "";
            if (incoming == Selected) return;

            Selected = incoming;
            if (IsShowing) Rebuild();
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _economy)) return;
            ServiceRegistry.TryGet(out _housing);

            Select("");
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            Select("");
        }

        public void Rebuild()
        {
            _list.Clear();
            if (_economy == null) return;

            int shown = 0;
            foreach (var stack in _economy.Inventory.Stacks)
            {
                if (stack.Quantity <= 0) continue;
                if (_housing != null && !_housing.IsPlaceable(stack.CatalogId)) continue;

                _list.Add(Row(stack.CatalogId, stack.Quantity, shown++ % 2 == 1));
            }

            if (shown == 0)
                _list.Add(UiTheme.Body("No tienes muebles. Los venden en la tienda de muebles.",
                                       soft: true));
        }

        private VisualElement Row(string catalogId, int quantity, bool alternate)
        {
            var row = UiTheme.Row(alternate);
            row.style.justifyContent = Justify.SpaceBetween;
            if (catalogId == Selected) row.style.backgroundColor = UiTheme.Peach;

            var item = _economy.GetItem(catalogId);
            var name = UiTheme.Body(item != null ? item.DisplayName : catalogId);
            name.style.flexGrow = 1;
            row.Add(name);
            row.Add(UiTheme.Pill($"×{quantity}", UiTheme.Mint));

            row.RegisterCallback<ClickEvent>(_ =>
            {
                // Volver a pulsar el mismo lo suelta: si no, no hay forma de dejar de
                // colocar salvo saliendo del modo entero.
                Select(Selected == catalogId ? "" : catalogId);
                Rebuild();
            });

            return row;
        }

        private void Select(string catalogId)
        {
            if (Selected == catalogId) return;

            Selected = catalogId;
            _hint.text = string.IsNullOrEmpty(catalogId)
                ? ""
                : $"Colocando {_economy?.GetItem(catalogId)?.DisplayName ?? catalogId}.";

            EventBus.Publish(new FurnishSelectionChanged(Selected));
        }
    }
}
