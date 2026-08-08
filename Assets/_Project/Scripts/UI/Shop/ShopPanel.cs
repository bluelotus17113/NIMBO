using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Shop
{
    /// <summary>
    /// La tienda: lo que hay hoy, lo que cuesta y si el jugador puede permitírselo.
    /// </summary>
    /// <remarks>
    /// El botón de comprar se apaga en vez de desaparecer cuando falta dinero o nivel.
    /// Es deliberado: ver lo que todavía no puedes comprar es la mitad de la razón
    /// para seguir jugando, y un hueco vacío no da esa información.
    /// </remarks>
    public sealed class ShopPanel : System.IDisposable
    {
        private readonly Label _title;
        private readonly Label _coins;
        private readonly VisualElement _list;

        private string _shopId;

        public VisualElement Root { get; }

        public ShopPanel()
        {
            Root = new VisualElement { name = "tienda" };
            Root.style.width = 420;
            Root.style.display = DisplayStyle.None;

            var card = UiTheme.Card();

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;

            _title = UiTheme.Title("Tienda");
            header.Add(_title);

            _coins = new Label("0");
            _coins.style.fontSize = 17;
            _coins.style.unityFontStyleAndWeight = FontStyle.Bold;
            _coins.style.color = UiTheme.AccentDeep;
            header.Add(_coins);

            card.Add(header);
            Root.Add(card);

            var listCard = UiTheme.Card();
            _list = new VisualElement();
            listCard.Add(_list);
            Root.Add(listCard);

            var close = UiTheme.Action("Cerrar", Hide);
            close.style.backgroundColor = UiTheme.InkSoft;
            Root.Add(close);

            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
        }

        public void Dispose() => EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);

        private void OnCoinsChanged(CoinsChanged _) => Refresh();

        public bool IsShowing => _shopId != null;

        public void Show(string shopId)
        {
            _shopId = shopId;
            Root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            _shopId = null;
            Root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            if (_shopId == null) return;
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return;

            _title.text = ShopName(_shopId);
            _coins.text = $"{economy.Wallet.Coins} nimbos";

            _list.Clear();

            int highestLevel = HighestIslanderLevel();
            var stock = economy.StockOf(_shopId);

            if (stock == null || stock.Count == 0)
            {
                _list.Add(UiTheme.Body("Hoy no queda nada. Vuelve mañana.", soft: true));
                return;
            }

            for (int i = 0; i < stock.Count; i++)
            {
                var item = economy.GetItem(stock[i]);
                if (item == null) continue;
                _list.Add(BuildRow(item, economy, highestLevel));
            }
        }

        private VisualElement BuildRow(IItemDefinition item, IEconomyService economy, int level)
        {
            var row = new VisualElement();
            row.style.marginBottom = 12;

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            top.style.alignItems = Align.Center;

            var name = UiTheme.Body(item.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            top.Add(name);
            top.Add(UiTheme.Chip($"{item.Price}", UiTheme.Accent));
            row.Add(top);

            var description = UiTheme.Body(item.Description, soft: true);
            description.style.marginBottom = 6;
            row.Add(description);

            bool affordable = economy.Wallet.CanAfford(item.Price);
            bool unlocked = level >= item.UnlockLevel;

            string label = !unlocked ? $"Nivel {item.UnlockLevel}"
                         : !affordable ? "No te llega"
                         : "Comprar";

            string catalogId = item.CatalogId;
            var button = UiTheme.Action(label, () =>
            {
                if (economy.TryBuy(catalogId)) Refresh();
            });

            if (!affordable || !unlocked)
            {
                button.SetEnabled(false);
                button.style.backgroundColor = UiTheme.InkSoft;
            }

            row.Add(button);
            return row;
        }

        /// <summary>El nivel más alto de la isla, que es el que abre el catálogo.</summary>
        private static int HighestIslanderLevel()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return 1;

            int highest = 1;
            var all = registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Progression.Level > highest) highest = all[i].Progression.Level;
            return highest;
        }

        private static string ShopName(string shopId) => shopId switch
        {
            "tienda_comida" => "Tienda de comida",
            "tienda_muebles" => "Tienda de muebles",
            "tienda_ropa" => "Tienda de ropa",
            _ => "Tienda",
        };
    }
}
