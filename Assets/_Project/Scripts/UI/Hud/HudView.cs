using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Hud
{
    /// <summary>
    /// Lo único que se queda encima de la isla: la hora a un lado, las monedas y el
    /// menú al otro.
    /// </summary>
    /// <remarks>
    /// El reloj se refresca por fotograma pero el resto solo cuando llega su aviso.
    /// Reconstruir la interfaz entera cada fotograma es la forma más rápida de que un
    /// juego tranquilo se coma una batería de portátil.
    ///
    /// Era una barra crema de lado a lado con tres bloques repartidos. Ahora son dos
    /// cápsulas sueltas sobre el cielo: ocupan la mitad y, sobre todo, dejan ver la
    /// isla por el medio, que es lo que uno ha venido a mirar. El contador de
    /// peticiones no se enseña cuando está a cero: un cero permanente es ruido, y lo
    /// que hay que ver es cuándo deja de serlo.
    /// </remarks>
    public sealed class HudView : System.IDisposable
    {
        private readonly GameClock _clock;
        private readonly Label _clockLabel;
        private readonly Label _dayLabel;
        private readonly Label _coinsLabel;
        private readonly Label _requestBadge;
        private readonly VisualElement _badgeHolder;

        public VisualElement Root { get; }

        public HudView(GameClock clock, System.Action onMenu)
        {
            _clock = clock;

            Root = new VisualElement { name = "hud" };
            var s = Root.style;
            s.flexDirection = FlexDirection.Row;
            s.alignItems = Align.FlexStart;
            s.justifyContent = Justify.SpaceBetween;
            s.marginLeft = s.marginRight = s.marginTop = UiTheme.SpaceL;

            // --- reloj ---
            var timeBlock = Capsule();
            timeBlock.style.flexDirection = FlexDirection.Column;
            timeBlock.style.alignItems = Align.FlexStart;

            _clockLabel = new Label("00:00");
            _clockLabel.style.fontSize = 26;
            _clockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _clockLabel.style.color = UiTheme.Ink;
            timeBlock.Add(_clockLabel);

            _dayLabel = UiTheme.Body("Día 1", soft: true);
            _dayLabel.style.marginTop = 1;
            timeBlock.Add(_dayLabel);
            Root.Add(timeBlock);

            // --- monedas, peticiones y menú, todo a la derecha ---
            var right = new VisualElement();
            right.style.flexDirection = FlexDirection.Row;
            right.style.alignItems = Align.Center;

            var coinBlock = Capsule();
            coinBlock.style.flexDirection = FlexDirection.Row;
            coinBlock.style.alignItems = Align.Center;
            coinBlock.style.marginRight = UiTheme.SpaceS;

            // La moneda es un círculo mantequilla con el borde melocotón: un icono de
            // formas, como manda el contrato, y no un emoji que cambia con el sistema.
            var coin = UiTheme.Dot(UiTheme.Butter, 16);
            coin.style.borderTopWidth = coin.style.borderBottomWidth =
                coin.style.borderLeftWidth = coin.style.borderRightWidth = 2;
            coin.style.borderTopColor = coin.style.borderBottomColor =
                coin.style.borderLeftColor = coin.style.borderRightColor = UiTheme.PeachDeep;
            coin.style.marginRight = UiTheme.SpaceS;
            coinBlock.Add(coin);

            _coinsLabel = new Label("0");
            _coinsLabel.style.fontSize = 18;
            _coinsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _coinsLabel.style.color = UiTheme.Ink;
            coinBlock.Add(_coinsLabel);
            right.Add(coinBlock);

            // --- peticiones ---
            _badgeHolder = Capsule();
            _badgeHolder.style.flexDirection = FlexDirection.Row;
            _badgeHolder.style.alignItems = Align.Center;
            _badgeHolder.style.marginRight = UiTheme.SpaceS;
            _badgeHolder.style.display = DisplayStyle.None;

            _requestBadge = UiTheme.Pill("0", UiTheme.Peach);
            _requestBadge.style.marginRight = UiTheme.SpaceS;
            _badgeHolder.Add(_requestBadge);

            var label = UiTheme.Body("piden algo", soft: true);
            _badgeHolder.Add(label);
            right.Add(_badgeHolder);

            right.Add(MenuButton(onMenu));
            Root.Add(right);

            EventBus.Subscribe<CoinsChanged>(OnCoins);
            EventBus.Subscribe<RequestRaised>(OnRequestsChanged);
            EventBus.Subscribe<RequestResolved>(OnRequestsChanged);
            EventBus.Subscribe<RequestExpired>(OnRequestsChanged);

            RefreshCoins();
            RefreshBadge();
        }

        /// <summary>Una cápsula crema suelta sobre el cielo. El bloque del HUD.</summary>
        private static VisualElement Capsule()
        {
            var capsule = new VisualElement();
            var s = capsule.style;
            s.backgroundColor = UiTheme.Cream;
            s.paddingLeft = s.paddingRight = UiTheme.SpaceL;
            s.paddingTop = s.paddingBottom = UiTheme.SpaceS;
            UiTheme.SetRadius(capsule, UiTheme.RadiusPill);
            return capsule;
        }

        /// <summary>
        /// La puerta al menú. Es el único botón que se queda en pantalla, y lleva
        /// escrita su tecla: quien la aprenda no vuelve a pulsarlo.
        /// </summary>
        private static VisualElement MenuButton(System.Action onMenu)
        {
            var button = new VisualElement { name = "boton-menu" };
            var s = button.style;
            s.flexDirection = FlexDirection.Row;
            s.alignItems = Align.Center;
            s.backgroundColor = UiTheme.Cream;
            s.paddingLeft = s.paddingRight = UiTheme.SpaceL;
            s.paddingTop = s.paddingBottom = UiTheme.SpaceS;
            UiTheme.SetRadius(button, UiTheme.RadiusPill);
            UiTheme.Animate(button, 120);
            UiTheme.Hoverable(button, UiTheme.Cream, UiTheme.CreamPress);
            UiTheme.Pressable(button);

            // Tres rayas apiladas: el icono de menú de toda la vida, hecho de formas.
            var lines = new VisualElement();
            lines.style.marginRight = UiTheme.SpaceS;
            for (int i = 0; i < 3; i++)
            {
                var line = new VisualElement();
                line.style.width = 15;
                line.style.height = 2;
                line.style.marginBottom = i < 2 ? 3 : 0;
                line.style.backgroundColor = UiTheme.InkSoft;
                UiTheme.SetRadius(line, 1);
                lines.Add(line);
            }
            button.Add(lines);

            var text = new Label("Menú");
            text.style.fontSize = 15;
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.style.color = UiTheme.Ink;
            button.Add(text);

            var key = UiTheme.Caption("Tab");
            key.style.marginLeft = UiTheme.SpaceS;
            button.Add(key);

            button.RegisterCallback<ClickEvent>(_ => onMenu());
            return button;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<CoinsChanged>(OnCoins);
            EventBus.Unsubscribe<RequestRaised>(OnRequestsChanged);
            EventBus.Unsubscribe<RequestResolved>(OnRequestsChanged);
            EventBus.Unsubscribe<RequestExpired>(OnRequestsChanged);
        }

        /// <summary>Solo el reloj: es lo único que cambia sin que pase nada más.</summary>
        public void Tick()
        {
            _clockLabel.text = _clock.FormatClock();
            _dayLabel.text = $"Día {_clock.Day} · {PhaseName(_clock.Phase)}";
        }

        private static string PhaseName(DayPhase phase) => phase switch
        {
            DayPhase.Dawn => "amanece",
            DayPhase.Morning => "mañana",
            DayPhase.Afternoon => "tarde",
            DayPhase.Evening => "atardece",
            _ => "noche",
        };

        private void OnCoins(CoinsChanged _) => RefreshCoins();
        private void OnRequestsChanged<T>(T _) => RefreshBadge();

        private void RefreshCoins()
        {
            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
                _coinsLabel.text = economy.Wallet.Coins.ToString();
        }

        private void RefreshBadge()
        {
            if (!ServiceRegistry.TryGet<IRequestService>(out var requests)) return;

            int open = requests.OpenCount;
            _requestBadge.text = open.ToString();

            // A cero desaparece entero. Un contador que siempre dice cero deja de
            // mirarse, y entonces tampoco se ve el día que dice dos.
            _badgeHolder.style.display = open == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
