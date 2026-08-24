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
    public sealed class UiRoot : MonoBehaviour, Menu.IEscapeCloser
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
        private Player.HotbarView _hotbar;
        private Player.BagPanel _bag;
        private Player.CraftPanel _craft;
        private Player.ShippingPanel _shipping;
        private Chronicle.ChroniclePanel _chronicle;
        private Requests.RequestBoardPanel _board;
        private Village.EventsPanel _events;
        private Player.SkillsPanel _skills;
        private Minigames.MinigamePanel _minigame;
        private Player.MapPanel _map;
        private Player.BuildPanel _build;
        private Player.FurnishPanel _furnish;
        private Player.DoorFade _fade;
        private CreatorPanel _creator;
        private Hub.MenuHub _hub;
        private VisualElement _islanderStrip;

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
            EventBus.Unsubscribe<TreeSpoke>(OnTreeSpoke);
            EventBus.Unsubscribe<BuildModeChanged>(OnBuildModeChanged);
            EventBus.Unsubscribe<FurnishModeChanged>(OnFurnishModeChanged);
            EventBus.Unsubscribe<DecorModeChanged>(OnDecorModeChanged);
            EventBus.Unsubscribe<InteriorEntered>(OnInteriorEntered);
            EventBus.Unsubscribe<InteriorExited>(OnInteriorExited);
            EventBus.Unsubscribe<MenuOpened>(OnMenuOpened);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            _toast?.Unsubscribe();
            _furnish?.Unsubscribe();
            _hotbar?.Unsubscribe();
            _fade?.Unsubscribe();
            _hud?.Dispose();
            ServiceRegistry.Unregister<Menu.IEscapeCloser>();
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

            _hub = new Hub.MenuHub();

            _hud = new HudView(_clock, () => _hub.Toggle());
            root.Add(_hud.Root);

            // El cuerpo ya solo lleva lo que aparece por sí solo: la tienda al entrar
            // en una, el cajón al usarlo, el menú del modo en el que estés. Todo lo que
            // antes se abría con un botón de la fila vive ahora dentro del menú.
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.marginLeft = body.style.marginRight = UiTheme.SpaceL;
            body.style.marginTop = UiTheme.SpaceL;

            // Cada panel con su alto y no estirado hasta abajo: por omisión flexbox los
            // estira al alto de la fila, y el de decorar —que es de tamaño fijo por
            // dentro— quedaba con medio metro de crema vacía debajo del contenido.
            body.style.alignItems = Align.FlexStart;

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

            // Decorar va aquí y no dentro del menú: es un modo, como construir y como
            // amueblar. Se entra desde el pie de la columna y ocupa la pantalla él solo.
            _decor = new Decor.DecorPanel();
            body.Add(_decor.Root);

            // Su cruz tiene que salir del modo, no solo esconder la tarjeta: escondiendo
            // solo el panel, el reloj y la barra se quedan apagados y el jugador se
            // queda mirando la isla sin nada con lo que jugar y sin saber por qué.
            var closeDecor = _decor.Root.Q<Button>("cerrar");
            if (closeDecor != null)
                closeDecor.clickable =
                    new Clickable(() => EventBus.Publish(new DecorModeChanged(false)));

            _shipping = new Player.ShippingPanel();
            body.Add(_shipping.Root);

            // El minijuego va en el cuerpo y no en el menú a propósito: no se elige
            // desde una pestaña, lo abre el mundo —una receta que se cocina, un
            // concierto— y mientras dura no hay menú que valga.
            _minigame = new Minigames.MinigamePanel();
            body.Add(_minigame.Root);

            _build = new Player.BuildPanel();
            body.Add(_build.Root);

            _furnish = new Player.FurnishPanel();
            _furnish.Subscribe();
            body.Add(_furnish.Root);

            root.Add(body);

            // --- lo que vive dentro del menú ---
            _panel = new IslanderPanel();
            _bag = new Player.BagPanel();
            _craft = new Player.CraftPanel();
            // Las recetas de cocina no se hacen de un clic: se juegan. El menú avisa y
            // aquí se abre el minijuego, porque el menú no sabe de otras ventanas.
            _craft.OnCook = recipe => EventBus.Publish(new MinigameRequested(
                MinigameKind.Cooking, DifficultyOf(recipe), recipe.RecipeId));
            _map = new Player.MapPanel();
            _achievements = new Achievements.AchievementsPanel();

            _hub.Add("Mochila", UiTheme.Peach, _bag.Root, _bag.Show, _bag.Hide);
            _hub.Add("Hacer", UiTheme.Butter, _craft.Root, _craft.Show, _craft.Hide);
            _hub.Add("Vecinos", UiTheme.Mint, BuildNeighbours(), RebuildStrip, HideNeighbour);
            _hub.Add("Mapa", UiTheme.Sky, _map.Root, _map.Show, _map.Hide);
            _hub.Add("Logros", UiTheme.Rose, _achievements.Root,
                     _achievements.Show, _achievements.Hide);

            // **Estas cuatro no las quitó el rediseño: no las conocía.** La rama se
            // bifurcó antes de que existieran —en el UiRoot de la base no hay ni una
            // referencia a ellas— y la noche entera las fue añadiendo a la fila de
            // botones que este menú sustituye. Sin volver a colgarlas aquí, mezclar el
            // rediseño habría dejado sin puerta a la Crónica, al tablón de encargos, a
            // las fiestas y a las vías: cuatro sistemas enteros, cada uno con sus
            // pruebas en verde, invisibles para el jugador. Es la enfermedad de §18
            // provocada por una mezcla, que es la forma más tonta de cometerla.
            _chronicle = new Chronicle.ChroniclePanel();
            _hub.Add("Crónica", UiTheme.Lavender, _chronicle.Root,
                     _chronicle.Show, _chronicle.Hide);

            _board = new Requests.RequestBoardPanel();
            _hub.Add("Encargos", UiTheme.Sage, _board.Root, _board.Show, _board.Hide);

            _events = new Village.EventsPanel();
            _hub.Add("Fiestas", UiTheme.Butter, _events.Root, _events.Show, _events.Hide);

            _skills = new Player.SkillsPanel();
            _hub.Add("Vías", UiTheme.SkySoft, _skills.Root, _skills.Show, _skills.Hide);

            SetIndoors(false);

            // La pausa y los ajustes viven en la capa de encima y hasta ahora solo se
            // llegaba a ellos con Escape. Desde aquí también.
            _hub.SetSystemButton("Pausa y ajustes",
                                 () => EventBus.Publish(new GamePaused(true)));

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

            // El menú va por encima del reloj y de la barra —los tapa con su velo— pero
            // por debajo del cartel de logro y del fundido de las puertas.
            root.Add(_hub.Root);
            UiTheme.Animate(_hud.Root, 160);
            UiTheme.Animate(_hotbar.Root, 160);
            EventBus.Subscribe<MenuOpened>(OnMenuOpened);
            EventBus.Subscribe<GamePaused>(OnGamePaused);

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
            EventBus.Subscribe<TreeSpoke>(OnTreeSpoke);
            EventBus.Subscribe<BuildModeChanged>(OnBuildModeChanged);
            EventBus.Subscribe<FurnishModeChanged>(OnFurnishModeChanged);
            EventBus.Subscribe<DecorModeChanged>(OnDecorModeChanged);
            EventBus.Subscribe<InteriorEntered>(OnInteriorEntered);
            EventBus.Subscribe<InteriorExited>(OnInteriorExited);

            RebuildStrip();
            _mounted = true;

            // Escape lo lee MainMenuView, que es la capa de arriba; aquí solo se ofrece
            // «cierra lo que tengas abierto». De ahí que haya un único dueño de la tecla.
            ServiceRegistry.Register<Menu.IEscapeCloser>(this);
        }

        /// <summary>
        /// La sección de vecinos del menú: quién vive aquí, la ficha del que mires y
        /// la puerta para traer a alguien nuevo.
        /// </summary>
        /// <remarks>
        /// La lista estaba antes en una fila pegada al borde de abajo, encendida todo
        /// el rato. Leer quién se ha hecho amigo de quién es algo que uno hace al abrir
        /// el menú, no mientras riega el huerto, así que su sitio es este.
        /// </remarks>
        private VisualElement BuildNeighbours()
        {
            var section = new VisualElement { name = "vecinos" };
            section.Add(UiTheme.Header("Vecinos", null));

            _islanderStrip = new VisualElement();
            _islanderStrip.style.flexDirection = FlexDirection.Row;
            _islanderStrip.style.flexWrap = Wrap.Wrap;
            _islanderStrip.style.marginBottom = UiTheme.SpaceM;
            section.Add(_islanderStrip);

            section.Add(_panel.Root);

            var invite = UiTheme.Secondary("Que venga alguien nuevo", () =>
            {
                _hub.Close();
                _creator.Show();
            });
            invite.style.alignSelf = Align.FlexStart;
            invite.style.marginTop = UiTheme.SpaceM;
            section.Add(invite);

            return section;
        }

        /// <summary>Al salir de la sección se cierra la ficha, no la isla entera.</summary>
        private void HideNeighbour()
        {
            _panel.Hide();
            EventBus.Publish(new IslanderFocused(""));
        }

        /// <summary>
        /// El reloj y la barra se apartan mientras el menú está abierto. Dejarlos
        /// debajo del velo los deja legibles a medias, que es peor que no verlos.
        /// </summary>
        /// <summary>
        /// Con la partida en pausa, esta capa se calla: la pantalla es de la de
        /// arriba y el reloj está parado.
        /// </summary>
        private bool _paused;

        private void OnGamePaused(GamePaused evt)
        {
            _paused = evt.Paused;

            // El reloj y la barra se apartan también con la pausa, no solo con el menú.
            // El velo del cartel es azul y translúcido, así que sin esto se quedaban
            // detrás medio legibles: ni se leen ni dejan de verse.
            SetPlayChrome(!evt.Paused);
        }

        /// <summary>Enciende o apaga lo que solo sirve jugando: el reloj y la barra.</summary>
        private void SetPlayChrome(bool visible)
        {
            float opacity = visible ? 1f : 0f;
            _hud.Root.style.opacity = opacity;
            _hotbar.Root.style.opacity = opacity;
        }

        private void OnMenuOpened(MenuOpened evt) => SetPlayChrome(!evt.Open);

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
        /// <summary>Cocinar, tocar o pescar: se abre el minijuego que toque.</summary>
        /// <remarks>
        /// Si no se puede arrancar —no hay servicio, o ya hay otro a medias— no se abre
        /// nada. Una ventana vacía es peor que ninguna: parece que el juego se ha roto.
        /// </remarks>
        /// <summary>
        /// Lo difícil que es cocinar una receta: cuántas cosas lleva.
        /// </summary>
        /// <remarks>
        /// Una sopa de dos ingredientes son tres pasos y unas croquetas de cinco son
        /// seis. Sale gratis del dato que ya está en la receta y ordena la cocina sola:
        /// lo que cuesta reunir cuesta también hacerlo.
        /// </remarks>
        private static int DifficultyOf(Recipe recipe) =>
            Mathf.Clamp(recipe.Ingredients?.Count ?? 1, 1, 5);

        private void OnMinigameRequested(MinigameRequested evt)
        {
            if (_minigame == null) return;

            _hub.Close();
            _minigame.Show(evt.Kind, evt.Difficulty, evt.Context);
        }

        /// <summary>
        /// El Árbol Nimbo ha hablado: el mismo cartelito que los logros.
        /// </summary>
        /// <remarks>
        /// El toast va en cola y de uno en uno (AchievementToast.cs), que es justo lo
        /// que un ritual de apertura del día necesita: una frase que se lee de pasada,
        /// no una pantalla que hay que cerrar.
        /// </remarks>
        private void OnTreeSpoke(TreeSpoke evt) => _toast.Push("El Árbol Nimbo", evt.Text);

        /// <summary>Ha leído el tablón de la plaza: se abre su pestaña del menú.</summary>
        private void OnRequestBoardRead(RequestBoardRead _) => _hub.Toggle("Encargos");

        /// <summary>
        /// Cierra una cosa de las que estén abiertas. False si no había nada.
        /// </summary>
        /// <remarks>
        /// **Aquí se resolvió el choque de los dos diseños de Escape, y conviene que
        /// quede escrito.** La rama del rediseño repartía la tecla entre dos lectores
        /// —`UiRoot` y `MainMenuView`— con un `EscapeGuard` que daba el turno al primero
        /// que preguntara, porque Unity no promete en qué orden corren dos `Update` y sin
        /// el árbitro la pausa se abría y se cerraba en el mismo fotograma. La rama de la
        /// noche hizo lo otro: **un solo lector**, que pregunta aquí antes de pausar.
        ///
        /// Se queda el segundo, y no por gusto: con un único lector el problema que el
        /// árbitro resolvía **no puede ocurrir**, así que `EscapeGuard` deja de tener
        /// motivo. Un mecanismo que existe para arbitrar entre dos deja de hacer falta
        /// cuando hay uno. Lo que sí se conserva entero es lo que hacían sus dos lecturas:
        /// salir del modo activo y cerrar el menú, y está aquí abajo.
        ///
        /// Una pulsación, un cierre: Escape no es «limpiar la pantalla», es «sácame de
        /// donde estoy».
        /// </remarks>
        public bool CloseTopPanel()
        {
            if (_furnishMode) { EventBus.Publish(new FurnishModeChanged(false)); return true; }
            if (_buildMode) { EventBus.Publish(new BuildModeChanged(false)); return true; }
            if (_decorMode) { EventBus.Publish(new DecorModeChanged(false)); return true; }

            if (_minigame.IsShowing) { _minigame.Hide(); return true; }
            if (_shipping.IsShowing) { _shipping.Hide(); return true; }
            if (_shop.IsShowing) { _shop.Hide(); return true; }
            if (_creator.IsShowing) { _creator.Hide(); return true; }

            if (_hub.IsOpen) { _hub.Close(); return true; }

            return false;
        }

        private void OnStationUsed(StationUsed evt)
        {
            switch (evt.Station)
            {
                case CraftStationKind.Shipping:
                    if (_shipping.IsShowing) _shipping.Hide(); else _shipping.Show();
                    break;

                default:
                    // La mesa de trabajo abre el menú por «Hacer». Es la misma pantalla
                    // que la del menú, así que enseñar una copia suelta encima sería
                    // tener dos sitios distintos para lo mismo.
                    _hub.Toggle("Hacer");
                    break;
            }
        }

        private bool _buildMode;
        private bool _furnishMode;
        private bool _decorMode;
        private bool _indoors;

        /// <summary>
        /// Ha entrado en un sitio con techo: cambia el modo que ofrece el menú y, si es
        /// una tienda, se pone el mostrador delante sin que haya que pedirlo.
        /// </summary>
        /// <remarks>
        /// Construir de puertas adentro movía la cámara a la aldea con el jugador medio
        /// kilómetro por debajo, así que ese modo se va mientras estás dentro.
        ///
        /// Las tiendas tenían tres botones encendidos en la esquina —comida, muebles y
        /// ropa— que se podían pulsar desde el otro extremo de la isla. Comprar es ir a
        /// la tienda: ahora se abre al entrar por la puerta y se cierra al salir.
        /// </remarks>
        private void OnInteriorEntered(InteriorEntered evt)
        {
            SetIndoors(true);

            string shopId = ShopIdOf(evt.HomeKey);
            if (shopId != null)
            {
                _hub.Close();
                _shop.Show(shopId);
            }
        }

        private void OnInteriorExited(InteriorExited _)
        {
            SetIndoors(false);
            _shop.Hide();
        }

        /// <summary>
        /// La tienda de una zona, o <c>null</c> si esa zona no vende nada.
        /// </summary>
        /// <remarks>
        /// Se hace con el nombre y no preguntándole a la isla porque el identificador de
        /// zona es el de la tienda con «zona_» delante —<c>zona_tienda_comida</c> contra
        /// <c>tienda_comida</c>— y esta capa no ve el catálogo de tiendas: la interfaz
        /// está por debajo de economía en el grafo de ensamblados. Si un día dejan de
        /// llamarse igual, la tienda no abre y no se rompe nada.
        /// </remarks>
        private static string ShopIdOf(string zoneId)
        {
            const string prefix = "zona_";
            if (string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith(prefix)) return null;

            string rest = zoneId[prefix.Length..];
            return rest.StartsWith("tienda_") ? rest : null;
        }

        /// <summary>
        /// Los modos que se pueden usar donde estás, y solo esos.
        /// </summary>
        /// <remarks>
        /// De puertas adentro se amuebla la habitación; en la calle se construye y se
        /// decora. Ofrecer los tres siempre sería enseñar dos botones que van a
        /// rechazar la pulsación sin decir por qué.
        /// </remarks>
        private void SetIndoors(bool indoors)
        {
            _indoors = indoors;
            if (_hub == null) return;

            _hub.ClearModes();

            if (indoors)
            {
                _hub.AddMode("Amueblar",
                             () => EventBus.Publish(new FurnishModeChanged(!_furnishMode)));
                return;
            }

            _hub.AddMode("Construir",
                         () => EventBus.Publish(new BuildModeChanged(!_buildMode)));
            _hub.AddMode("Decorar",
                         () => EventBus.Publish(new DecorModeChanged(!_decorMode)));
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
            _hud.Root.style.display = play;

            if (evt.Furnishing)
            {
                _hub.Close();
                _shipping.Hide();
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
            _hud.Root.style.display = play;

            if (evt.Building)
            {
                _hub.Close();
                _shop.Hide();
                _shipping.Hide();
                _build.Show();
            }
            else _build.Hide();
        }

        /// <summary>
        /// Decorando: fuera el reloj y la barra, y el plano de la zona en medio.
        /// </summary>
        /// <remarks>
        /// Este no mueve la cámara —se decora sobre un plano cenital dibujado en la
        /// interfaz, no pinchando en el mundo—, así que la isla se queda como estaba
        /// detrás. Lo que sí hace, como los otros dos modos, es apagar todo lo demás:
        /// mientras colocas bancos no hay nada que hacer con la mochila.
        /// </remarks>
        private void OnDecorModeChanged(DecorModeChanged evt)
        {
            // Decorar es de la calle. El aviso puede llegar con el jugador ya dentro
            // de una casa, y allí el plano no vale para nada.
            if (evt.Decorating && _indoors) return;

            _decorMode = evt.Decorating;

            var play = evt.Decorating ? DisplayStyle.None : DisplayStyle.Flex;
            _hotbar.Root.style.display = play;
            _hud.Root.style.display = play;

            if (evt.Decorating)
            {
                _hub.Close();
                _shop.Hide();
                _shipping.Hide();
                _decor.Show();
            }
            else _decor.Hide();
        }

        private void OnRosterChanged<T>(T _) => RebuildStrip();

        /// <summary>Un botón por habitante para abrir su ficha, dentro del menú.</summary>
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

                // El que estés mirando va en melocotón y el resto en crema: la lista
                // dice dónde estás sin necesitar una línea que lo explique.
                bool open = _panel.IsShowing && _panel.Root.name == id;
                var button = open
                    ? UiTheme.Action(islander.Identity.ShortName, () => Toggle(id))
                    : UiTheme.Secondary(islander.Identity.ShortName, () => Toggle(id));

                button.style.marginRight = UiTheme.SpaceS;
                button.style.marginBottom = UiTheme.SpaceS;
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
                RebuildStrip();
                return;
            }

            _panel.Root.name = islanderId;
            _panel.Show(islanderId);
            EventBus.Publish(new IslanderFocused(islanderId));
            RebuildStrip();
        }

        /// <summary>
        /// Las teclas del menú. Tab lo abre por la mochila, M por el mapa, Esc cierra
        /// lo que haya —y si no hay nada, pausa— y las flechas pasan de una sección a
        /// otra sin soltar el teclado.
        /// </summary>
        /// <remarks>
        /// Construyendo o amueblando no se atiende ninguna salvo la de salir: ahí la
        /// pantalla es del modo entero y abrir el menú encima dejaría dos cosas
        /// mandando sobre la misma cámara.
        ///
        /// Escape se va cerrando capas de fuera adentro: primero el modo, luego el
        /// menú, y solo cuando no queda nada abierto pausa la partida. Esa última es
        /// la puerta a la pausa y a los ajustes, que viven en la capa de encima.
        /// </remarks>
        private void ReadMenuKeys()
        {
            // En pausa manda la capa de arriba: ni Tab abre el menú por detrás del
            // cartel, ni Escape hace nada aquí.
            if (_paused) return;

            if (_buildMode || _furnishMode || _decorMode)
            {
                // La B entra y sale de construir y de amueblar, pero no de decorar:
                // ese modo se abre desde el menú y se sale con Esc, como cualquier
                // pantalla. Darle una tecla propia sin que nadie la pida sería
                // inventarse un atajo que no está escrito en ningún sitio.
                // Escape ya no se lee aquí: lo lee MainMenuView y baja hasta
                // CloseTopPanel, que sale del modo igual que hacía esta rama. Queda la B,
                // que es un atajo propio de construir y amueblar y no toca a nadie.
                bool salir = Input.GetKeyDown(Menu.GameKeys.Furnish) && !_decorMode;
                if (!salir) return;

                // Cada aviso por su lado y no con un operador entre medias: el bus
                // reparte por el tipo de lo que le des, y una expresión que devuelva lo
                // uno o lo otro lo convierte en <c>object</c> y no lo recibe nadie.
                if (_furnishMode) EventBus.Publish(new FurnishModeChanged(false));
                else if (_buildMode) EventBus.Publish(new BuildModeChanged(false));
                else EventBus.Publish(new DecorModeChanged(false));
                return;
            }

            // Tab es la tecla que todo el mundo prueba primero en un juego con
            // inventario, así que abre el menú por donde está la mochila.
            if (Input.GetKeyDown(Menu.GameKeys.Bag)) _hub.Toggle("Mochila");

            // M de mapa. Con dos islas y un puente, saber de qué lado estás pasa a ser
            // una pregunta de verdad y merece su tecla.
            if (Input.GetKeyDown(Menu.GameKeys.Map)) _hub.Toggle("Mapa");

            if (_hub.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) _hub.Step(1);
                if (Input.GetKeyDown(KeyCode.LeftArrow)) _hub.Step(-1);
                return;
            }

            // B entra en el modo que toque aquí: construir en la calle, amueblar dentro
            // de casa. Antes solo servía de puertas adentro y en la calle no hacía nada.
            if (!Input.GetKeyDown(Menu.GameKeys.Furnish)) return;

            if (_indoors) EventBus.Publish(new FurnishModeChanged(true));
            else EventBus.Publish(new BuildModeChanged(true));
        }

        private void Update()
        {
            if (!_mounted) return;

            _hud.Tick();

            // Sin escalar: el cartel de logro tiene que terminar de irse aunque el
            // juego esté en pausa, en vez de quedarse clavado en pantalla.
            _toast.Tick(Time.unscaledDeltaTime);
            _fade.Tick(Time.unscaledDeltaTime);
            RefreshPrompt();

            // Los huecos de la barra solo se cambian jugando: con el menú abierto la
            // barra ni se ve, y cambiar de herramienta a ciegas no lo quiere nadie.
            if (!_hub.IsOpen) _hotbar.Tick();

            ReadMenuKeys();

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

            // Los cinco paneles que se quedaban mintiendo mientras los mirabas: un
            // encargo que caduca, un logro que se desbloquea, una fiesta que empieza.
            // Cada uno firma lo que ha pintado y solo reconstruye si la firma cambió,
            // así que esto no redibuja cinco listas cada 0,4 s: compara cinco cadenas.
            //
            // Existían los cinco `Refresh()` y **no los llamaba nadie**. El comentario
            // de `RequestBoardPanel._signature` llegó a decir que este ciclo los
            // refrescaba, cuando era falso; lo cazó el verificador de nimbo-copy
            // leyendo un mecanismo que su autor daba por enchufado.
            if (_board.IsShowing) _board.Refresh();
            if (_achievements.IsShowing) _achievements.Refresh();
            if (_chronicle.IsShowing) _chronicle.Refresh();
            if (_events.IsShowing) _events.Refresh();
            if (_skills.IsShowing) _skills.Refresh();

            // Lo elegido en el menú viaja hasta quien dibuja el fantasma. Se busca por
            // reflexión igual que el interactor: el que pinta vive en Nimbo.Art, que
            // esta capa no puede ver sin cerrar un ciclo entre ensamblados.
            if (_buildMode) PushBuildSelection();
        }
    }
}
