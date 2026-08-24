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
    ///
    /// Las tres esquinas derechas son atajos, no solo lectura: el badge abre el
    /// tablón —por el mismo aviso que ya usa el tablón de la plaza— y los otros dos
    /// avisan por callback porque el HUD no conoce paneles y no puede abrirlos él
    /// solo. Quien los enchufa es UiRoot, que es quien tiene las ventanas.
    /// </remarks>
    public sealed class HudView : System.IDisposable
    {
        private readonly GameClock _clock;
        private readonly Label _clockLabel;
        private readonly Label _dayLabel;
        private readonly Label _coinsLabel;
        private readonly Label _requestBadge;
        private readonly Label _levelLabel;
        private readonly VisualElement _badgeHolder;

        public VisualElement Root { get; }

        /// <summary>
        /// Pulsar los nimbos quiere decir «quiero gastarlos». Lo abre UiRoot.
        /// </summary>
        /// <remarks>
        /// Queda en nada hasta que UiRoot le ponga función: el HUD va antes en el
        /// montaje que las tiendas, así que ni siquiera podría pedirle la referencia.
        /// </remarks>
        public System.Action OnCoinsClicked;

        /// <summary>Pulsar el nivel de aldeano abre las cinco vías. Lo abre UiRoot.</summary>
        public System.Action OnLevelClicked;

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
            //
            // Clicable a propósito: el número que se está mirando es también el
            // botón de «quiero gastarlos».
            _coinsLabel = new Label("0");
            _coinsLabel.style.fontSize = 18;
            _coinsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _coinsLabel.style.color = UiTheme.AccentDeep;

            var coinBlock = new VisualElement { name = "hud-nimbos" };
            // Sin tooltip a propósito: la franja de avisos (GateNotice) recoge
            // cualquier tooltip visible como explicación de bloqueo, y un texto
            // informativo siempre visible le ganaría el pase a lo que sí está cerrado.
            coinBlock.RegisterCallback<ClickEvent>(_ => OnCoinsClicked?.Invoke());
            coinBlock.style.flexDirection = FlexDirection.Row;
            coinBlock.style.alignItems = Align.Center;
            var coinName = UiTheme.Body("nimbos", soft: true);
            coinName.style.marginRight = 8;
            coinBlock.Add(coinName);
            coinBlock.Add(_coinsLabel);
            Root.Add(coinBlock);

            // --- nivel de aldeano ---
            //
            // La media de las cinco vías (§12.1). No se gana por su cuenta y no
            // desbloquea nada: está aquí porque hace falta un número que diga «voy por
            // aquí» sin abrir ninguna pantalla. Lo que abre cosas son las vías, y esas
            // se miran a propósito.
            var levelBlock = new VisualElement { name = "hud-aldeano" };
            // Sin tooltip: mismo porqué que hud-nimbos.
            levelBlock.RegisterCallback<ClickEvent>(_ => OnLevelClicked?.Invoke());
            levelBlock.style.flexDirection = FlexDirection.Row;
            levelBlock.style.alignItems = Align.Center;
            var levelName = UiTheme.Body("aldeano", soft: true);
            levelName.style.marginRight = 8;
            levelBlock.Add(levelName);
            _levelLabel = UiTheme.Chip("1", UiTheme.Lavender);
            _levelLabel.style.marginRight = 0;
            levelBlock.Add(_levelLabel);
            Root.Add(levelBlock);

            // --- peticiones ---
            //
            // El badge publica el mismo aviso que el tablón de la plaza
            // (PlayerInteractor, TargetKind.Board): UiRoot ya lo escucha y lo convierte
            // en abrir la pantalla. Un canal nuevo para el mismo gesto sería un segundo
            // sitio que mantener sincronizado.
            _badgeHolder = new VisualElement { name = "hud-peticiones" };
            // Sin tooltip: mismo porqué que hud-nimbos.
            _badgeHolder.RegisterCallback<ClickEvent>(_ =>
                EventBus.Publish(new RequestBoardRead()));
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
            EventBus.Subscribe<SkillLeveledUp>(OnSkillLeveledUp);

            RefreshCoins();
            RefreshBadge();
            RefreshLevel();
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<CoinsChanged>(OnCoins);
            EventBus.Unsubscribe<RequestRaised>(OnRequestsChanged);
            EventBus.Unsubscribe<RequestResolved>(OnRequestsChanged);
            EventBus.Unsubscribe<RequestExpired>(OnRequestsChanged);
            EventBus.Unsubscribe<SkillLeveledUp>(OnSkillLeveledUp);
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

        private void OnSkillLeveledUp(SkillLeveledUp _) => RefreshLevel();

        private void RefreshLevel()
        {
            if (ServiceRegistry.TryGet<IPlayerProgression>(out var progression))
                _levelLabel.text = progression.VillagerLevel.ToString();
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
