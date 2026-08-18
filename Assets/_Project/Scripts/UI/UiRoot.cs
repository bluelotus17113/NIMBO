using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.UI.Creator;
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
        private Achievements.AchievementsPanel _achievements;
        private Achievements.AchievementToast _toast;
        private Chronicle.ChroniclePanel _chronicle;
        private Requests.RequestBoardPanel _board;
        private Minigames.MinigamePanel _minigame;
        private Player.SkillsPanel _skills;
        private Player.HotbarView _hotbar;
        private Player.BagPanel _bag;
        private Player.CraftPanel _craft;
        private Player.ShippingPanel _shipping;
        private Player.MapPanel _map;
        private Player.BuildPanel _build;
        private Player.FurnishPanel _furnish;
        private Player.DoorFade _fade;
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
            EventBus.Unsubscribe<StationUsed>(OnStationUsed);
            EventBus.Unsubscribe<RequestBoardRead>(OnRequestBoardRead);
            EventBus.Unsubscribe<MinigameRequested>(OnMinigameRequested);
            EventBus.Unsubscribe<BuildModeChanged>(OnBuildModeChanged);
            EventBus.Unsubscribe<FurnishModeChanged>(OnFurnishModeChanged);
            EventBus.Unsubscribe<InteriorEntered>(OnInteriorEntered);
            EventBus.Unsubscribe<InteriorExited>(OnInteriorExited);
            _toast?.Unsubscribe();
            _furnish?.Unsubscribe();
            _hotbar?.Unsubscribe();
            _fade?.Unsubscribe();
            _hud?.Dispose();
            _shop?.Dispose();
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

            _achievements = new Achievements.AchievementsPanel();
            body.Add(_achievements.Root);

            _chronicle = new Chronicle.ChroniclePanel();
            body.Add(_chronicle.Root);

            _board = new Requests.RequestBoardPanel();
            body.Add(_board.Root);

            _minigame = new Minigames.MinigamePanel();
            body.Add(_minigame.Root);

            _skills = new Player.SkillsPanel();
            body.Add(_skills.Root);

            _bag = new Player.BagPanel();
            body.Add(_bag.Root);

            _craft = new Player.CraftPanel();
            // Las recetas de cocina no se hacen de un clic: se juegan. El menú avisa y
            // aquí se abre el minijuego, porque el menú no sabe de otras ventanas.
            _craft.OnCook = recipe => EventBus.Publish(new MinigameRequested(
                MinigameKind.Cooking, DifficultyOf(recipe), recipe.RecipeId));
            body.Add(_craft.Root);

            _shipping = new Player.ShippingPanel();
            body.Add(_shipping.Root);

            _map = new Player.MapPanel();
            body.Add(_map.Root);

            _build = new Player.BuildPanel();
            body.Add(_build.Root);

            _furnish = new Player.FurnishPanel();
            _furnish.Subscribe();
            body.Add(_furnish.Root);

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

            // El cartel de logro va suelto sobre todo lo demás, así que se cuelga de
            // la raíz y no del cuerpo: dentro del cuerpo lo colocaría el flexbox y
            // acabaría empujando a los paneles en vez de flotar sobre ellos.
            _toast = new Achievements.AchievementToast();
            _toast.Subscribe();
            root.Add(_toast.Root);

            // La barra va la última y al final de la raíz: es lo que el jugador mira
            // sin querer, y tiene que quedar pegada abajo por debajo de todo panel.
            _hotbar = new Player.HotbarView();
            _hotbar.Subscribe();
            _hotbar.Rebuild();
            root.Add(_hotbar.Root);

            // El fundido va el último de todos: tiene que taparlo todo, incluido el
            // cartel de logro y la barra.
            _fade = new Player.DoorFade();
            _fade.Subscribe();
            root.Add(_fade.Root);

            EventBus.Subscribe<IslanderCreated>(OnRosterChanged);
            EventBus.Subscribe<IslanderLeft>(OnRosterChanged);
            EventBus.Subscribe<StationUsed>(OnStationUsed);
            EventBus.Subscribe<RequestBoardRead>(OnRequestBoardRead);
            EventBus.Subscribe<MinigameRequested>(OnMinigameRequested);
            EventBus.Subscribe<BuildModeChanged>(OnBuildModeChanged);
            EventBus.Subscribe<FurnishModeChanged>(OnFurnishModeChanged);
            EventBus.Subscribe<InteriorEntered>(OnInteriorEntered);
            EventBus.Subscribe<InteriorExited>(OnInteriorExited);

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
            _buildButton = Add("Construir", () => EventBus.Publish(new BuildModeChanged(!_buildMode)));

            // Dentro de casa se amuebla; fuera se construye. Es el mismo sitio de la
            // fila porque es el mismo gesto, y así no hay nunca dos botones de colocar
            // cosas encendidos a la vez diciendo cada uno una cosa distinta.
            _furnishButton = Add("Amueblar", () => EventBus.Publish(new FurnishModeChanged(!_furnishMode)));
            _furnishButton.style.display = DisplayStyle.None;

            // Ampliar tu cabaña, y solo estando dentro de ella. En el mismo sitio que
            // amueblar porque es el mismo gesto de ocuparse de tu casa, y así no hace
            // falta un botón más en la fila para algo que se usa dos veces por partida.
            _expandButton = Add("Ampliar la casa", ExpandHome);
            _expandButton.style.display = DisplayStyle.None;

            Add("Mapa", () =>
            {
                if (_map.IsShowing) _map.Hide(); else _map.Show();
            });
            Add("Mochila", () =>
            {
                if (_bag.IsShowing) _bag.Hide(); else _bag.Show();
            });
            Add("Hacer", () =>
            {
                if (_craft.IsShowing) _craft.Hide(); else _craft.Show();
            });
            // Las cinco vías, junto a la mochila y el crafteo: es información del
            // protagonista, no de la aldea, y va con lo suyo.
            Add("Vías", () =>
            {
                if (_skills.IsShowing) _skills.Hide(); else _skills.Show();
            });
            // Los encargos tienen botón **además** del tablón de la plaza, igual que
            // «Hacer» convive con la mesa de trabajo. El tablón es donde uno mira al
            // pasar y dice de lejos cuántas cosas hay; el botón es para no cruzar el
            // puente solo para comprobar que no hay ninguna.
            Add("Encargos", () =>
            {
                if (_board.IsShowing) _board.Hide(); else _board.Show();
            });
            // La crónica va justo antes de los logros y no al final de la fila: es lo
            // que se abre al entrar para ver qué pasó anoche, y lo que se abre primero
            // no puede estar en el último sitio donde se busca.
            Add("Crónica", () =>
            {
                if (_chronicle.IsShowing) _chronicle.Hide(); else _chronicle.Show();
            });
            Add("Logros", () =>
            {
                if (_achievements.IsShowing) _achievements.Hide(); else _achievements.Show();
            });
            Add("Nuevo habitante", () => _creator.Show());
            return _actions;

            Button Add(string text, System.Action onClick)
            {
                var button = UiTheme.Action(text, onClick);
                button.style.marginRight = 8;
                _actions.Add(button);
                return button;
            }
        }

        /// <summary>
        /// Lo difícil que es cocinar una receta: cuántas cosas lleva.
        /// </summary>
        /// <remarks>
        /// Una sopa de dos ingredientes son tres pasos y unas croquetas de cinco son
        /// seis. Sale gratis del dato que ya está en la receta y ordena la cocina sola:
        /// lo que cuesta reunir cuesta también hacerlo.
        /// </remarks>
        /// <summary>
        /// Amplía tu cabaña, y si no se puede dice por qué.
        /// </summary>
        /// <remarks>
        /// El aviso va al cartel de logros porque no hay otro sitio donde quepa un
        /// «te faltan 20 de piedra» sin abrir una pantalla, y abrir una pantalla para
        /// leer un motivo es justo lo que hace que nadie lo lea.
        /// </remarks>
        private void ExpandHome()
        {
            if (!ServiceRegistry.TryGet<IHomeUpgradeService>(out var homes)) return;

            var verdict = homes.CanUpgradePlayerHome();
            if (verdict == UpgradeRejection.Ok && homes.UpgradePlayerHome())
            {
                int size = homes.SizeOfLevel(homes.PlayerLevel);
                _toast?.Push("Casa ampliada", $"Tu cabaña es ahora de {size}×{size}.");
                return;
            }

            _toast?.Push("Todavía no", verdict switch
            {
                UpgradeRejection.MaxedOut => "No se puede ampliar más.",
                UpgradeRejection.NotEnoughCoins => "No te llegan los nimbos.",
                UpgradeRejection.NotEnoughMaterials => "Falta obra. Hay que traer material.",
                _ => "Ahora mismo no se puede.",
            });
        }

        private static int DifficultyOf(Recipe recipe) =>
            Mathf.Clamp(recipe.Ingredients?.Count ?? 1, 1, 5);

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

        /// <summary>
        /// El jugador se ha puesto delante de un mueble de su casa: se abre lo que
        /// toque. Es lo que hace que la mesa de trabajo sea un sitio al que ir y no
        /// otro botón en la fila de arriba.
        /// </summary>
        private void OnStationUsed(StationUsed evt)
        {
            switch (evt.Station)
            {
                case CraftStationKind.Shipping:
                    if (_shipping.IsShowing) _shipping.Hide(); else _shipping.Show();
                    break;

                // El fogón abre el mismo menú pero ya en su pestaña. Llegar a la cocina
                // y tener que buscar la cocina en una fila de tres botones es de las
                // cosas que hacen dudar de si has pulsado lo que querías.
                case CraftStationKind.Kitchen:
                    if (_craft.IsShowing) _craft.Hide(); else _craft.Show(CraftStation.Kitchen);
                    break;

                default:
                    if (_craft.IsShowing) _craft.Hide(); else _craft.Show();
                    break;
            }
        }

        /// <summary>
        /// Se abre el minijuego que han pedido, y lo que estuviera abierto se cierra.
        /// </summary>
        /// <remarks>
        /// Si no se puede arrancar —no hay servicio, o ya hay otro a medias— no se abre
        /// nada. Una ventana vacía es peor que ninguna: parece que el juego se ha roto.
        /// </remarks>
        private void OnMinigameRequested(MinigameRequested evt)
        {
            if (_minigame == null) return;

            _craft.Hide();
            _minigame.Show(evt.Kind, evt.Difficulty, evt.Context);
        }

        /// <summary>Ha leído el tablón de la plaza: se abre la misma pantalla que el botón.</summary>
        private void OnRequestBoardRead(RequestBoardRead _)
        {
            if (_board == null) return;
            if (_board.IsShowing) _board.Hide(); else _board.Show();
        }

        private bool _buildMode;
        private bool _furnishMode;
        private bool _indoors;
        private Button _buildButton;
        private Button _furnishButton;
        private Button _expandButton;

        /// <summary>
        /// Ha entrado en una casa: la fila de acciones cambia de oficio.
        /// </summary>
        /// <remarks>
        /// Construir de puertas adentro movía la cámara a la aldea con el jugador
        /// medio kilómetro por debajo, así que ese botón se va mientras estás dentro.
        /// </remarks>
        private void OnInteriorEntered(InteriorEntered _) => SetIndoors(true);

        private void OnInteriorExited(InteriorExited _) => SetIndoors(false);

        private void SetIndoors(bool indoors)
        {
            _indoors = indoors;
            if (_buildButton == null || _furnishButton == null) return;

            _buildButton.style.display = indoors ? DisplayStyle.None : DisplayStyle.Flex;
            _furnishButton.style.display = indoors ? DisplayStyle.Flex : DisplayStyle.None;
            if (_expandButton != null)
                _expandButton.style.display = indoors ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Amueblando, como construyendo: fuera todo lo de andar por la isla.</summary>
        private void OnFurnishModeChanged(FurnishModeChanged evt)
        {
            // Amueblar solo existe dentro de una casa. El aviso puede llegar de una
            // tecla pulsada en la calle, y encender el menú allí sería ofrecer un modo
            // que quien coloca va a rechazar sin decir nada.
            if (evt.Furnishing && !_indoors) return;

            _furnishMode = evt.Furnishing;

            var play = evt.Furnishing ? DisplayStyle.None : DisplayStyle.Flex;
            _hotbar.Root.style.display = play;
            _islanderStrip.style.display = play;
            _actions.style.display = play;

            if (evt.Furnishing)
            {
                _bag.Hide();
                _craft.Hide();
                _shipping.Hide();
                _map.Hide();
                _panel.Hide();
                _furnish.Show();
            }
            else _furnish.Hide();
        }

        /// <summary>
        /// Al construir, la interfaz de andar por la isla sobra: se apaga entera y
        /// queda solo el menú de edificios.
        /// </summary>
        /// <remarks>
        /// La barra de abajo y la fila de habitantes no valen para nada mirando la
        /// aldea desde el aire, y encima taparían justo las casillas del borde.
        /// </remarks>
        private void OnBuildModeChanged(BuildModeChanged evt)
        {
            _buildMode = evt.Building;

            var play = evt.Building ? DisplayStyle.None : DisplayStyle.Flex;
            _hotbar.Root.style.display = play;
            _islanderStrip.style.display = play;
            _actions.style.display = play;

            if (evt.Building)
            {
                _bag.Hide();
                _craft.Hide();
                _shipping.Hide();
                _map.Hide();
                _build.Show();
            }
            else _build.Hide();
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
        /// <summary>
        /// Enseña en la barra lo que el protagonista tiene delante.
        /// </summary>
        /// <remarks>
        /// La interfaz busca al interactor en la escena en vez de que él le hable:
        /// <c>Nimbo.UI</c> está por debajo de <c>Nimbo.Art</c> en el grafo de
        /// ensamblados, y al revés se cerraría un ciclo. Se busca una sola vez.
        /// </remarks>
        private Component _interactor;
        private System.Reflection.PropertyInfo _promptProperty;

        private Component _buildView;
        private System.Reflection.PropertyInfo _buildSelected;

        private void PushBuildSelection()
        {
            if (_buildView == null)
            {
                var world = GameObject.Find("Mundo");
                if (world == null) return;

                foreach (var component in world.GetComponents<Component>())
                {
                    var property = component.GetType().GetProperty("Selected");
                    if (property == null || property.PropertyType != typeof(string)) continue;

                    _buildView = component;
                    _buildSelected = property;
                    break;
                }
                if (_buildView == null) return;
            }

            _buildSelected.SetValue(_buildView, _build.Selected);
        }

        private void RefreshPrompt()
        {
            if (_interactor == null)
            {
                var found = GameObject.Find("Protagonista");
                if (found == null) return;

                foreach (var component in found.GetComponents<Component>())
                {
                    var property = component.GetType().GetProperty("Prompt");
                    if (property == null) continue;

                    _interactor = component;
                    _promptProperty = property;
                    break;
                }
                if (_interactor == null) return;
            }

            _hotbar.SetPrompt(_promptProperty.GetValue(_interactor) as string);
        }

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

        /// <summary>¿Hay algo abierto que se maneje con el ratón?</summary>
        private bool AnyPanelOpen =>
            _panel.IsShowing || _shop.IsShowing || _decor.IsShowing ||
            _achievements.IsShowing || _bag.IsShowing || _craft.IsShowing || _board.IsShowing ||
            _minigame.IsShowing || _skills.IsShowing ||
            _shipping.IsShowing || _map.IsShowing || _build.IsShowing ||
            _furnish.IsShowing || _creator.IsShowing || _chronicle.IsShowing;

        private bool _pointerWasNeeded;

        /// <summary>
        /// Avisa cuando se abre o se cierra algo, para que la cámara suelte el ratón.
        /// </summary>
        /// <remarks>
        /// Se mira cada fotograma en vez de avisar desde cada <c>Show</c> y cada
        /// <c>Hide</c>. Son once paneles y varios se cierran solos —el creador al
        /// terminar, la ficha cuando el vecino se va de la isla—: enganchando el aviso
        /// a mano en cada sitio, el día que alguien añada un panel o un camino de
        /// cierre nuevo, el ratón se queda capturado con un panel abierto delante y no
        /// hay forma de cerrarlo. Comparar un booleano sesenta veces por segundo no le
        /// cuesta nada a nadie.
        /// </remarks>
        private void RefreshPointer()
        {
            bool needed = AnyPanelOpen;
            if (needed == _pointerWasNeeded) return;

            _pointerWasNeeded = needed;
            EventBus.Publish(new PointerNeeded(needed));
        }

        private void Update()
        {
            if (!_mounted) return;

            _hud.Tick();

            // Sin escalar: el cartel de logro tiene que terminar de irse aunque el
            // juego esté en pausa, en vez de quedarse clavado en pantalla.
            _toast.Tick(Time.unscaledDeltaTime);
            _fade.Tick(Time.unscaledDeltaTime);

            // El minijuego de ritmo va con reloj propio, así que necesita el fotograma
            // entero y no el refresco lento de las listas: a dos veces por segundo no
            // hay ritmo que valga. Los otros dos lo ignoran.
            _minigame.Tick(Time.unscaledDeltaTime);
            _hotbar.Tick();
            RefreshPrompt();
            RefreshPointer();

            // Tab abre y cierra la mochila. Es la tecla que todo el mundo prueba
            // primero en un juego con inventario.
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_bag.IsShowing) _bag.Hide(); else _bag.Show();
            }

            // M de mapa. Con dos islas y un puente, saber de qué lado estás pasa a ser
            // una pregunta de verdad y merece su tecla.
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (_map.IsShowing) _map.Hide(); else _map.Show();
            }

            // B de amueblar, y solo dentro de casa: fuera esa tecla no hace nada en vez
            // de abrir un menú que no se puede usar.
            if (Input.GetKeyDown(KeyCode.B) && (_indoors || _furnishMode))
                EventBus.Publish(new FurnishModeChanged(!_furnishMode));

            // Las listas y las barras a ritmo lento: nadie nota que una barra de
            // hambre se mueva dos veces por segundo en vez de sesenta, y reconstruir
            // listas cada fotograma es lo que calienta el portátil.
            _sinceRefresh += Time.unscaledDeltaTime;
            if (_sinceRefresh < _refreshInterval) return;

            _sinceRefresh = 0f;
            _panel.Refresh();
            if (_shop.IsShowing) _shop.Refresh();

            // La mochila abierta se refresca sola: si recoges algo con ella delante
            // —pasa, porque no para el juego— tiene que aparecer sin cerrarla.
            if (_bag.IsShowing) _bag.Rebuild();

            // El mapa abierto se refresca: los vecinos andan, y uno que enseñe dónde
            // estaban al abrirlo miente a los diez segundos.
            if (_map.IsShowing) _map.Refresh();

            // Y el menú de muebles, que va restando de lo que te queda a cada silla
            // que pones y sumando a cada una que recoges.
            if (_furnish.IsShowing) _furnish.Rebuild();

            // Lo elegido en el menú viaja hasta quien dibuja el fantasma. Se busca por
            // reflexión igual que el interactor: el que pinta vive en Nimbo.Art, que
            // esta capa no puede ver sin cerrar un ciclo entre ensamblados.
            if (_buildMode) PushBuildSelection();
        }
    }
}
