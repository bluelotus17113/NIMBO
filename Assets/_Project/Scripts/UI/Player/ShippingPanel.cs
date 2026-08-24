using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El cajón de envíos: vender lo que has traído de la isla.
    /// </summary>
    /// <remarks>
    /// Solo se ofrece lo que tiene sentido vender —materiales y cultivos—, nunca las
    /// herramientas ni las semillas. Vender la azada sin querer es un desastre que no
    /// se puede deshacer, y este juego no hace eso; dejarlas fuera de la lista es más
    /// simple que un aviso de confirmación y no se puede fallar.
    ///
    /// «Vender todo» es la excepción que sí pregunta: vacía de un golpe justo lo que
    /// los encargos de material pagan por recibir, así que ahí el desastre cuesta un
    /// clic. Se pregunta enseñando qué se va a perder —el patrón del título al
    /// empezar de nuevo—, no con un «¿seguro?».
    ///
    /// El precio es el del catálogo, sin regateo. Un margen distinto por objeto sería
    /// otra cosa que aprender, y aquí vender es el cierre del paseo, no un minijuego.
    /// </remarks>
    public sealed class ShippingPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _total;
        private readonly Label _hint;

        /// <summary>El botón de vender todo, que se aparta mientras se confirma.</summary>
        private Button _sellAll;

        /// <summary>
        /// El cartel de confirmación vive en su hueco propio entre la lista y el
        /// botón: así no hay que saber en qué posición del panel cae para quitarlo.
        /// </summary>
        private readonly VisualElement _confirmSlot;
        private VisualElement _confirmCard;

        private IInventoryService _inventory;
        private IEconomyService _economy;

        public ShippingPanel()
        {
            Root = UiTheme.Card("cajon");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 520;
            Root.style.maxHeight = Length.Percent(90);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Cajón de envíos"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _total = UiTheme.Body("", soft: true);
            _total.style.marginBottom = 8;
            Root.Add(_total);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            Root.Add(_list);

            _confirmSlot = new VisualElement();
            Root.Add(_confirmSlot);

            _sellAll = UiTheme.Action("Vender todo", AskConfirm);
            _sellAll.style.marginTop = 10;
            Root.Add(_sellAll);

            _hint = UiTheme.Body("Las herramientas y las semillas no se venden aquí.", soft: true);
            _hint.style.marginTop = 8;
            _hint.style.fontSize = 12;
            Root.Add(_hint);
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _inventory)) return;
            if (!ServiceRegistry.TryGet(out _economy)) return;

            // Reabrir empieza limpio: un cartel confirmado sobre una mochila que ya
            // cambió mientras estaba cerrado prometería vender lo que ya no está.
            CancelConfirm();

            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>Lo que se puede vender de lo que lleva encima, agrupado.</summary>
        private List<(string id, int quantity, long price)> Sellable()
        {
            var found = new Dictionary<string, int>();

            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                var stack = _inventory.At(i);
                if (stack.Quantity <= 0) continue;

                var item = _economy.GetItem(stack.CatalogId);
                if (item == null) continue;
                if (item.Category != ItemCategory.Material && item.Category != ItemCategory.Crop)
                    continue;

                found.TryGetValue(stack.CatalogId, out int had);
                found[stack.CatalogId] = had + stack.Quantity;
            }

            var result = new List<(string, int, long)>();
            foreach (var pair in found)
                result.Add((pair.Key, pair.Value, _economy.GetItem(pair.Key).Price));
            return result;
        }

        private void Rebuild()
        {
            _list.Clear();

            var sellable = Sellable();
            long sum = 0;
            for (int i = 0; i < sellable.Count; i++) sum += sellable[i].quantity * sellable[i].price;

            _total.text = sellable.Count == 0
                ? "No traes nada que vender."
                : $"Todo junto: {sum} nimbos";

            for (int i = 0; i < sellable.Count; i++)
                _list.Add(BuildRow(sellable[i], i % 2 == 1));
        }

        private VisualElement BuildRow((string id, int quantity, long price) entry, bool alternate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 8;
            row.style.marginBottom = 4;
            if (alternate) row.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(row, UiTheme.Radius);

            var item = _economy.GetItem(entry.id);
            var name = UiTheme.Body($"{item?.DisplayName ?? entry.id} ×{entry.quantity}");
            name.style.flexGrow = 1;
            row.Add(name);

            var pill = UiTheme.Pill($"{entry.quantity * entry.price}", UiTheme.Butter);
            row.Add(pill);

            row.Add(UiTheme.Secondary("Vender", () => Sell(entry.id, entry.quantity, entry.price)));
            return row;
        }

        private void Sell(string catalogId, int quantity, long price)
        {
            if (!_inventory.TryTake(catalogId, quantity)) return;

            _economy.AddCoins(quantity * price, $"vendido {catalogId}");
            Rebuild();
        }

        /// <summary>
        /// «Vender todo» no vende: enseña qué se va a llevar y espera un segundo clic.
        /// </summary>
        /// <remarks>
        /// Hay encargos de material que pagan por entregar exactamente lo que este
        /// botón funde, y la venta no se puede deshacer. Con la mochila vacía no hay
        /// nada que confirmar: se contesta en el aviso de siempre y punto.
        /// </remarks>
        private void AskConfirm()
        {
            var sellable = Sellable();
            if (sellable.Count == 0)
            {
                _hint.text = "No traes nada que vender.";
                return;
            }

            CancelConfirm();
            _confirmCard = BuildConfirm(sellable);
            _confirmSlot.Add(_confirmCard);

            // Dos caminos vivos hacia la misma venta es uno de más.
            _sellAll.style.display = DisplayStyle.None;
        }

        /// <summary>El cartel del título, traído aquí: se pregunta enseñando la pérdida.</summary>
        private VisualElement BuildConfirm(List<(string id, int quantity, long price)> sellable)
        {
            var card = UiTheme.Card("confirmar-venta");
            card.style.backgroundColor = UiTheme.Rose;

            card.Add(UiTheme.Title("¿Venderlo todo?"));
            card.Add(UiTheme.Body(
                "Se va de la mochila y no vuelve. Mira el tablón antes: algún vecino " +
                "puede estar esperando justo esto."));

            long sum = 0;
            for (int i = 0; i < sellable.Count; i++)
            {
                var entry = sellable[i];
                sum += entry.quantity * entry.price;

                var item = _economy.GetItem(entry.id);
                card.Add(UiTheme.Body(
                    $"{item?.DisplayName ?? entry.id} ×{entry.quantity} — " +
                    $"{entry.quantity * entry.price} nimbos", soft: true));
            }

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 14;

            var cancel = UiTheme.Secondary("Mejor no", CancelConfirm);
            cancel.style.flexGrow = 1;
            cancel.style.marginRight = 8;
            row.Add(cancel);

            var go = UiTheme.Action($"Vender todo ({sum} nimbos)", ConfirmEverything);
            go.style.flexGrow = 1;
            row.Add(go);

            card.Add(row);
            return card;
        }

        private void CancelConfirm()
        {
            if (_confirmCard == null) return;

            _confirmSlot.Remove(_confirmCard);
            _confirmCard = null;
            _sellAll.style.display = DisplayStyle.Flex;
        }

        private void ConfirmEverything()
        {
            CancelConfirm();
            SellEverything();
        }

        private void SellEverything()
        {
            var sellable = Sellable();
            for (int i = 0; i < sellable.Count; i++)
                Sell(sellable[i].id, sellable[i].quantity, sellable[i].price);

            _hint.text = sellable.Count == 0
                ? "No traías nada."
                : "Vendido. La isla te lo agradece.";
        }
    }
}
