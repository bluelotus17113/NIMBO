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
    /// La barra de arriba: reloj, monedas y cuántas peticiones esperan respuesta.
    /// </summary>
    /// <remarks>
    /// El reloj se refresca por fotograma pero el resto solo cuando llega su aviso.
    /// Reconstruir la interfaz entera cada fotograma es la forma más rápida de que un
    /// juego tranquilo se coma una batería de portátil.
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

        public HudView(GameClock clock)
        {
            _clock = clock;

            Root = new VisualElement { name = "hud" };
            var s = Root.style;
            s.flexDirection = FlexDirection.Row;
            s.alignItems = Align.Center;
            s.justifyContent = Justify.SpaceBetween;
            s.paddingLeft = s.paddingRight = 18;
            s.paddingTop = s.paddingBottom = 10;
            s.backgroundColor = UiTheme.Panel;
            s.marginLeft = s.marginRight = s.marginTop = UiTheme.Gap;
            UiTheme.SetRadius(Root, UiTheme.Radius);

            // --- reloj ---
            var timeBlock = new VisualElement();
            timeBlock.style.flexDirection = FlexDirection.Row;
            timeBlock.style.alignItems = Align.Center;

            _clockLabel = new Label("00:00");
            _clockLabel.style.fontSize = 24;
            _clockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _clockLabel.style.color = UiTheme.Ink;
            timeBlock.Add(_clockLabel);

            _dayLabel = UiTheme.Body("Día 1", soft: true);
            _dayLabel.style.marginLeft = 10;
            timeBlock.Add(_dayLabel);
            Root.Add(timeBlock);

            // --- monedas ---
            _coinsLabel = new Label("0");
            _coinsLabel.style.fontSize = 18;
            _coinsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _coinsLabel.style.color = UiTheme.AccentDeep;

            var coinBlock = new VisualElement();
            coinBlock.style.flexDirection = FlexDirection.Row;
            coinBlock.style.alignItems = Align.Center;
            var coinName = UiTheme.Body("nimbos", soft: true);
            coinName.style.marginRight = 8;
            coinBlock.Add(coinName);
            coinBlock.Add(_coinsLabel);
            Root.Add(coinBlock);

            // --- peticiones ---
            _badgeHolder = new VisualElement();
            _badgeHolder.style.flexDirection = FlexDirection.Row;
            _badgeHolder.style.alignItems = Align.Center;

            var label = UiTheme.Body("peticiones", soft: true);
            label.style.marginRight = 8;
            _badgeHolder.Add(label);

            _requestBadge = UiTheme.Chip("0", UiTheme.InkSoft);
            _requestBadge.style.marginRight = 0;
            _badgeHolder.Add(_requestBadge);
            Root.Add(_badgeHolder);

            EventBus.Subscribe<CoinsChanged>(OnCoins);
            EventBus.Subscribe<RequestRaised>(OnRequestsChanged);
            EventBus.Subscribe<RequestResolved>(OnRequestsChanged);
            EventBus.Subscribe<RequestExpired>(OnRequestsChanged);

            RefreshCoins();
            RefreshBadge();
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
            _requestBadge.style.backgroundColor = open == 0 ? UiTheme.InkSoft : UiTheme.Accent;
        }
    }
}
