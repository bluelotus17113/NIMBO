using Nimbo.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// La capa que va por encima de todo: el menú de inicio, la pausa y los ajustes.
    /// </summary>
    /// <remarks>
    /// Tiene su propio <c>UIDocument</c>, con una capa por encima de la del juego.
    /// Podría haber ido dentro de <c>UiRoot</c>, pero <c>UiRoot</c> no existe hasta
    /// que hay partida cargada, y el menú tiene que dibujarse justo antes de eso.
    ///
    /// Cuando no hay nada que enseñar se apaga con <c>display: none</c> en vez de
    /// vaciarse: un elemento vacío a pantalla completa sigue tragándose los clics, y
    /// el jugador se encuentra con una isla que no responde sin saber por qué.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        private enum Screen { Title, Pause, Options, Creator, Hidden }

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _content;
        private VisualElement _clouds;

        private TitleScreen _title;
        private PausePanel _pause;
        private OptionsPanel _options;
        private Creator.CreatorPanel _creator;

        private Screen _screen = Screen.Title;
        private Screen _optionsCameFrom = Screen.Title;
        private bool _gameRunning;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<GamePaused>(OnGamePaused);
            EventBus.Subscribe<NewGameRequested>(OnNewGameRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            EventBus.Unsubscribe<NewGameRequested>(OnNewGameRequested);
        }

        private void Start()
        {
            Build();
            Show(Screen.Title);
        }

        /// <summary>
        /// Pidió partida nueva: antes de encender nada, que se haga a sí mismo.
        /// </summary>
        /// <remarks>
        /// Lo primero que se ve del juego es tu cara, no la isla. La isla la enciende
        /// el aviso de que el creador ha terminado, y lo recoge el control de flujo.
        /// </remarks>
        private void OnNewGameRequested(NewGameRequested _) => Show(Screen.Creator);

        /// <summary>Ya hay partida: el menú se aparta y se queda esperando a Escape.</summary>
        private void OnGameLoaded(GameLoaded _)
        {
            _gameRunning = true;
            Show(Screen.Hidden);
        }

        /// <summary>
        /// Alguien ha pausado desde fuera. Es lo que permite que el día que haya un
        /// botón de pausa en el HUD no haya que tocar nada de aquí.
        /// </summary>
        private void OnGamePaused(GamePaused evt)
        {
            if (!_gameRunning) return;
            if (evt.Paused && _screen == Screen.Hidden) Show(Screen.Pause);
            else if (!evt.Paused && _screen != Screen.Hidden) Show(Screen.Hidden);
        }

        private void Build()
        {
            _root = _document.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1;
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;

            _clouds = BuildClouds();
            _root.Add(_clouds);

            _content = new VisualElement();
            _content.style.alignItems = Align.Center;
            _root.Add(_content);

            _title = new TitleScreen(() => Show(Screen.Options, from: Screen.Title));
            _pause = new PausePanel(
                onResume: () => EventBus.Publish(new GamePaused(false)),
                onOptions: () => Show(Screen.Options, from: Screen.Pause));
            _options = new OptionsPanel(() => Show(_optionsCameFrom));
            _creator = new Creator.CreatorPanel();
        }

        private void Show(Screen screen, Screen from = Screen.Title)
        {
            if (screen == Screen.Options) _optionsCameFrom = from;
            _screen = screen;

            if (screen == Screen.Hidden)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _root.style.display = DisplayStyle.Flex;
            _content.Clear();

            // Sin partida detrás, el fondo es cielo liso. Con partida detrás, es un
            // velo: se sigue viendo la isla, que es lo que hace que la pausa se
            // sienta un alto y no otra pantalla.
            _root.style.backgroundColor = _gameRunning
                ? new Color(UiTheme.Sky.r, UiTheme.Sky.g, UiTheme.Sky.b, 0.82f)
                : UiTheme.Sky;

            _clouds.style.display = _gameRunning || screen == Screen.Creator
                ? DisplayStyle.None : DisplayStyle.Flex;

            switch (screen)
            {
                case Screen.Title:
                    _content.Add(_title.Root);
                    break;
                case Screen.Pause:
                    _pause.Refresh();
                    _content.Add(_pause.Root);
                    break;
                case Screen.Options:
                    _content.Add(_options.Root);
                    break;
                case Screen.Creator:
                    _content.Add(_creator.Root);
                    _creator.ShowForProtagonist();
                    break;
            }
        }

        // ── Las nubes del fondo ──────────────────────────────────────────────
        //
        // El menú de un juego sobre islas flotantes no puede ser una pantalla
        // quieta. Son ocho cápsulas pálidas cruzando despacio: cuesta cuatro líneas
        // y es la diferencia entre «esto está vivo» y «esto es un formulario».

        private const int CloudCount = 8;
        private readonly VisualElement[] _cloudElements = new VisualElement[CloudCount];
        private readonly float[] _cloudX = new float[CloudCount];
        private readonly float[] _cloudSpeed = new float[CloudCount];
        private readonly float[] _cloudWidth = new float[CloudCount];

        private VisualElement BuildClouds()
        {
            var layer = new VisualElement();
            layer.style.position = Position.Absolute;
            layer.style.left = layer.style.right = layer.style.top = layer.style.bottom = 0;

            // Sin esto, la capa de nubes se come los clics de los botones que hay
            // detrás en el orden de dibujo. Es el fallo clásico de los fondos
            // decorativos en UI Toolkit.
            layer.pickingMode = PickingMode.Ignore;

            var rng = new System.Random(7);

            for (int i = 0; i < CloudCount; i++)
            {
                float width = 90f + (float)rng.NextDouble() * 190f;
                var cloud = new VisualElement();
                cloud.pickingMode = PickingMode.Ignore;
                cloud.style.position = Position.Absolute;
                cloud.style.width = width;
                cloud.style.height = width * 0.34f;
                cloud.style.top = Length.Percent(6f + (float)rng.NextDouble() * 82f);
                cloud.style.backgroundColor = new Color(1f, 1f, 1f, 0.34f);
                UiTheme.SetRadius(cloud, UiTheme.RadiusPill);

                _cloudElements[i] = cloud;
                _cloudWidth[i] = width;
                _cloudX[i] = (float)rng.NextDouble() * 1920f;
                _cloudSpeed[i] = 6f + (float)rng.NextDouble() * 16f;

                cloud.style.left = _cloudX[i];
                layer.Add(cloud);
            }

            return layer;
        }

        private void Update()
        {
            // Escape es el único mando del menú. El proyecto va con el sistema de
            // entrada antiguo (activeInputHandler: 0), así que Keyboard.current no
            // existe aquí y tiene que ser Input.GetKeyDown.
            if (_gameRunning && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_screen == Screen.Hidden) EventBus.Publish(new GamePaused(true));
                else if (_screen == Screen.Options) Show(_optionsCameFrom);
                else if (_screen == Screen.Creator) Show(Screen.Title);
                else EventBus.Publish(new GamePaused(false));
            }

            if (_screen == Screen.Hidden || _gameRunning) return;

            // Sin escalar: con el juego en pausa el tiempo va a cero, y unas nubes
            // congeladas detrás del menú parecerían la pantalla colgada.
            float dt = Time.unscaledDeltaTime;
            float width = _root.resolvedStyle.width;
            if (width <= 0f) width = 1920f;

            for (int i = 0; i < CloudCount; i++)
            {
                _cloudX[i] += _cloudSpeed[i] * dt;
                if (_cloudX[i] > width) _cloudX[i] = -_cloudWidth[i];
                _cloudElements[i].style.left = _cloudX[i];
            }
        }
    }
}
