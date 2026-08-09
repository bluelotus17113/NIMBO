using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.UI.Creator;
using Nimbo.UI.HousingEditor;
using Nimbo.UI.Hud;
using Nimbo.UI.Islander;
using Nimbo.UI.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI
{
    /// <summary>
    /// Monta la interfaz y la mantiene al día. Es el único MonoBehaviour de la capa
    /// de interfaz: todo lo demás son vistas normales que él coloca.
    /// </summary>
    /// <remarks>
    /// Necesita que el arranque haya registrado ya los servicios, así que espera a
    /// <c>GameLoaded</c> en vez de montarse en <c>Start</c>. Si se montara antes,
    /// pediría un <c>IRequestService</c> que todavía no existe y llenaría la consola
    /// de errores en el primer fotograma de cada partida.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiRoot : MonoBehaviour
    {
        [Tooltip("Cada cuántos segundos reales se refrescan las barras y las listas.")]
        [SerializeField, Range(0.1f, 2f)] private float _refreshInterval = 0.4f;

        private UIDocument _document;
        private HudView _hud;
        private IslanderPanel _panel;
        private ShopPanel _shop;
        private Decor.DecorPanel _decor;
        private HousingEditorPanel _housing;
        private CreatorPanel _creator;
        private VisualElement _islanderStrip;
        private VisualElement _actions;

        private GameClock _clock;
        private float _sinceRefresh;
        private bool _mounted;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable() => EventBus.Subscribe<GameLoaded>(OnGameLoaded);

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<IslanderCreated>(OnRosterChanged);
            EventBus.Unsubscribe<IslanderLeft>(OnRosterChanged);
            _hud?.Dispose();
            _shop?.Dispose();
            _housing?.Dispose();
        }

        private void OnGameLoaded(GameLoaded _)
        {
            // El reloj se pide al registro y no al arranque: Nimbo.Game depende de
            // Nimbo.UI, así que mirar en la otra dirección cerraría un ciclo entre
            // ensamblados y ni siquiera compilaría.
            if (!ServiceRegistry.TryGet<GameClock>(out _clock))
            {
                Debug.LogError("UiRoot: nadie ha registrado el reloj");
                return;
            }

            Mount();
        }

        private void Mount()
        {
            var root = _document.rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            _hud = new HudView(_clock);
            root.Add(_hud.Root);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.marginLeft = body.style.marginRight = UiTheme.Gap;
            body.style.marginTop = UiTheme.Gap;

            _panel = new IslanderPanel();
            body.Add(_panel.Root);

            _housing = new HousingEditorPanel();
            body.Add(_housing.Root);

            _creator = new CreatorPanel();
            _creator.OnFinished += OnIslanderCreated;
            body.Add(_creator.Root);

            // El resto del ancho queda libre a propósito: ahí va la isla en 3D, y la
            // interfaz no debe taparla más de lo imprescindible.
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            body.Add(spacer);

            _shop = new ShopPanel();
            body.Add(_shop.Root);

            _decor = new Decor.DecorPanel();
            body.Add(_decor.Root);

            root.Add(body);
            root.Add(BuildActionBar());

            _islanderStrip = new VisualElement();
            var strip = _islanderStrip.style;
            strip.flexDirection = FlexDirection.Row;
            strip.marginLeft = strip.marginRight = strip.marginBottom = UiTheme.Gap;
            strip.paddingTop = strip.paddingBottom = 8;
            strip.paddingLeft = strip.paddingRight = 10;
            strip.backgroundColor = UiTheme.Panel;
            UiTheme.SetRadius(_islanderStrip, UiTheme.Radius);
            root.Add(_islanderStrip);

            EventBus.Subscribe<IslanderCreated>(OnRosterChanged);
            EventBus.Subscribe<IslanderLeft>(OnRosterChanged);

            RebuildStrip();
            _mounted = true;
        }

        /// <summary>
        /// La fila de acciones: las tres pantallas que no cuelgan de un habitante.
        /// </summary>
        private VisualElement BuildActionBar()
        {
            _actions = new VisualElement();
            var s = _actions.style;
            s.flexDirection = FlexDirection.Row;
            s.marginLeft = s.marginRight = UiTheme.Gap;
            s.marginTop = UiTheme.Gap;
            s.paddingTop = s.paddingBottom = 8;
            s.paddingLeft = s.paddingRight = 10;
            s.backgroundColor = UiTheme.Panel;
            UiTheme.SetRadius(_actions, UiTheme.RadiusCard);

            Add("Comida", () => Toggle(() => _shop.Show("tienda_comida"), _shop.IsShowing));
            Add("Muebles", () => Toggle(() => _shop.Show("tienda_muebles"), _shop.IsShowing));
            Add("Ropa", () => Toggle(() => _shop.Show("tienda_ropa"), _shop.IsShowing));
            Add("Decorar", () =>
            {
                if (_decor.IsShowing) _decor.Hide(); else _decor.Show();
            });
            Add("Nuevo habitante", () => _creator.Show());
            return _actions;

            void Add(string text, System.Action onClick)
            {
                var button = UiTheme.Action(text, onClick);
                button.style.marginRight = 8;
                _actions.Add(button);
            }
        }

        /// <summary>Abre lo pedido, o lo cierra si ya estaba abierto.</summary>
        private void Toggle(System.Action open, bool alreadyOpen)
        {
            if (alreadyOpen) _shop.Hide(); else open();
        }

        /// <summary>
        /// El creador terminó. Aquí es donde el habitante entra de verdad en la isla:
        /// el panel solo lo modela, y quien decide si se queda es esto.
        /// </summary>
        private void OnIslanderCreated(Data.Islanders.IslanderData islander)
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;

            registry.Add(islander);
            EventBus.Publish(new IslanderCreated(islander.Id));
            _panel.Show(islander.Id);
        }

        private void OnRosterChanged<T>(T _) => RebuildStrip();

        /// <summary>La fila de abajo: un botón por habitante para abrir su ficha.</summary>
        private void RebuildStrip()
        {
            if (_islanderStrip == null) return;
            _islanderStrip.Clear();

            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry)) return;

            var all = registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                string id = islander.Id;

                var button = UiTheme.Action(islander.Identity.ShortName, () => Toggle(id));
                button.style.marginRight = 8;
                button.style.backgroundColor = UiTheme.PanelDark;
                _islanderStrip.Add(button);
            }

            if (all.Count == 0)
                _islanderStrip.Add(UiTheme.Body("La isla está vacía. Crea a alguien.", soft: true));
        }

        /// <summary>
        /// Abrir la ficha de alguien es «ir a verlo», no solo leer sus barras: se
        /// avisa de a quién se mira y la cámara se acerca a él. Cerrarla avisa con el
        /// identificador vacío y la cámara vuelve al plano general.
        /// </summary>
        private void Toggle(string islanderId)
        {
            if (_panel.IsShowing && _panel.Root.name == islanderId)
            {
                _panel.Hide();
                EventBus.Publish(new IslanderFocused(""));
                return;
            }

            _panel.Root.name = islanderId;
            _panel.Show(islanderId);
            EventBus.Publish(new IslanderFocused(islanderId));
        }

        private void Update()
        {
            if (!_mounted) return;

            _hud.Tick();

            // Las listas y las barras a ritmo lento: nadie nota que una barra de
            // hambre se mueva dos veces por segundo en vez de sesenta, y reconstruir
            // listas cada fotograma es lo que calienta el portátil.
            _sinceRefresh += Time.unscaledDeltaTime;
            if (_sinceRefresh < _refreshInterval) return;

            _sinceRefresh = 0f;
            _panel.Refresh();
            if (_shop.IsShowing) _shop.Refresh();
        }
    }
}
