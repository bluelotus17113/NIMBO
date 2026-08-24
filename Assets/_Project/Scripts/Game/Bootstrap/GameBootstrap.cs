using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Save;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Housing;
using Nimbo.Housing.Catalog;
using Nimbo.Island;
using Nimbo.Crafting;
using Nimbo.Gathering;
using Nimbo.Farming;
using Nimbo.Items;
using Nimbo.Island.Decor;
using Nimbo.Personality.Runtime;
using Nimbo.Player;
using Nimbo.Simulation;
using Nimbo.Simulation.Behaviour;
using Nimbo.Simulation.Jobs;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Progression;
using Nimbo.Simulation.Gifts;
using Nimbo.Simulation.Wardrobe;
using Nimbo.Simulation.Requests;
using Nimbo.Social;
using Nimbo.Social.Romance;
using UnityEngine;

namespace Nimbo.Game.Bootstrap
{
    /// <summary>
    /// Enciende el juego: carga la partida, monta los servicios y hace correr el reloj.
    /// </summary>
    /// <remarks>
    /// Es el único sitio donde se sabe qué implementación cumple cada contrato. Todo
    /// lo demás pide interfaces al <see cref="ServiceRegistry"/> y no se entera nunca
    /// de quién hay detrás. Si un módulo hubiera que cambiarlo entero, se cambia aquí
    /// y en ningún otro fichero.
    /// </remarks>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Configuración (si se deja vacío se usan los valores por defecto)")]
        [SerializeField] private NeedsConfig _needsConfig;
        [SerializeField] private RequestConfig _requestConfig;
        [SerializeField] private PlayerProgressionConfig _progressionConfig;
        [SerializeField] private SocialConfig _socialConfig;

        [Header("Partida nueva")]
        [Tooltip("Habitantes que ya viven en la isla al empezar.")]
        [SerializeField, Range(0, 12)] private int _starterIslanders = 3;
        [SerializeField] private long _startingCoins = 200;

        [Header("Guardado automático")]
        [Tooltip("Minutos de juego entre guardados. 0 lo desactiva.")]
        [SerializeField] private int _autosaveMinutes = 60;

        [Header("Arranque")]
        [Tooltip("Si está marcado, espera a que el menú diga por dónde empezar.")]
        [SerializeField] private bool _startFromMenu = true;

        private bool _forceNewGame;
        private string _protagonistName;
        private AppearanceData? _protagonistLook;

        private GameClock _clock;
        private SaveGame _save;
        private SimulationService _simulation;
        private RequestService _requests;
        private SocialService _social;
        private IslandService _island;
        private BuildService _build;
        private IslanderBrain _brain;
        private EconomyService _economy;
        private JobService _jobs;
        private NimboTree _tree;
        private WardrobeService _wardrobe;
        private GiftService _gifts;
        private AchievementService _achievements;
        private PlayerService _player;
        private InventoryService _inventory;
        private FarmingService _farming;
        private GatheringService _gathering;
        private CraftingService _crafting;
        private HomeUpgradeService _homeUpgrades;
        private WeddingPlanner _weddings;
        private Nimbo.Events.EventsService _events;
        private Nimbo.Events.Minigames.MinigameService _minigames;
        private PlayerProgressionService _progression;
        private ConductService _conduct;
        private Nimbo.Events.Scheduling.VillageEvents _villageEvents;
        private Nimbo.Events.Scheduling.EventSparks _sparks;

        private IslanderRegistry _registry;
        private long _lastAutosaveMinute;
        private bool _running;
        private bool _built;

        public GameClock Clock => _clock;
        public SaveGame Save => _save;

        private void Awake()
        {
            // Persistente entre escenas: la simulación no puede pararse porque el
            // jugador entre al editor de interiores.
            DontDestroyOnLoad(gameObject);

            // Con menú, montar aquí sería tirar el trabajo: hasta que el jugador no
            // elija, no se sabe siquiera si la partida que hay que cargar es la
            // guardada o una nueva.
            if (!_startFromMenu) Build();
        }

        private void Start()
        {
            if (_built) Launch();
        }

        /// <summary>
        /// Enciende la partida. La llama el menú, y solo una vez.
        /// </summary>
        /// <param name="newGame">
        /// Cierto para empezar de cero dejando atrás lo guardado.
        /// </param>
        /// <param name="displayName">
        /// Cómo se llama el protagonista. Vacío para que lo ponga la fábrica.
        /// </param>
        /// <param name="appearance">
        /// La cara que el jugador acaba de hacerse en el creador. Null para sortearla.
        /// </param>
        public void StartGame(bool newGame, string displayName = null,
                              AppearanceData? appearance = null)
        {
            if (_built) return;

            _forceNewGame = newGame;
            _protagonistName = displayName;
            _protagonistLook = appearance;
            Build();
            Launch();
        }

        /// <summary>
        /// El aviso de que la partida está lista va aquí y no en Awake, y esto no es
        /// un detalle: Unity ejecuta Awake y OnEnable objeto por objeto, así que si
        /// se publicara en Awake, los que se suscriben en su OnEnable —el mundo y la
        /// interfaz— aún no existirían y se perderían el aviso. La isla arrancaba con
        /// todos los servicios montados y sin un solo muñeco dibujado.
        /// </summary>
        private void Launch()
        {
            EventBus.Publish(new GameLoaded());
            CatchUpOfflineTime();

            _running = true;
            EventBus.Subscribe<DayPassed>(OnDayPassed);
            ApplyDayRhythm(_clock.Day);

            Debug.Log($"Isla Nimbo lista — {_registry.Count} habitantes, {_clock}");
        }

        private void OnDayPassed(DayPassed evt)
        {
            ApplyDayRhythm(evt.Day);

            // El huerto y los nodos avanzan por días, no por fotogramas, y por este
            // mismo camino: al volver de estar fuera el reloj publica un DayPassed por
            // cada día saltado, así que ponerse al día no necesita código aparte.
            _farming.AdvanceDay();
            _gathering.AdvanceDay();
        }

        /// <summary>
        /// Paga el logro. Lo hace el arranque y no el servicio de logros a propósito:
        /// un módulo que lleva la cuenta de lo que haces no tiene por qué saber
        /// ingresar dinero, y si lo supiera habría dos sitios desde los que entran
        /// nimbos en la partida.
        /// </summary>
        private void OnAchievementUnlocked(AchievementUnlocked evt)
        {
            if (evt.Reward <= 0) return;
            _economy.AddCoins(evt.Reward, $"logro {evt.AchievementId}");
        }

        /// <summary>
        /// Le da su carácter al día: el domingo se descansa mejor y el sábado la isla
        /// amanece de buen humor.
        /// </summary>
        private void ApplyDayRhythm(int day)
        {
            var weekday = WeeklyRhythm.DayOf(day);
            _simulation.SetRestMultiplier(WeeklyRhythm.RestMultiplier(weekday));

            float moodBonus = WeeklyRhythm.MoodBonus(weekday);
            if (moodBonus <= 0f) return;

            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                _simulation.ApplyHappiness(all[i].Id, moodBonus);
        }

        private void Build()
        {
            EnsureConfigs();

            // Empezar de nuevo aparta lo que había antes de tocar nada. El respaldo
            // normal no sirve para esto: en cuanto la isla nueva autoguarde, la copia
            // de seguridad pasaría a ser de la isla nueva y la vieja desaparecería.
            if (_forceNewGame) SaveSystem.Archive();

            _save = _forceNewGame ? null : SaveSystem.Read();
            bool isNewGame = _save == null;
            if (isNewGame) _save = NewSave();

            _clock = new GameClock(_save.ElapsedMinutes);
            _lastAutosaveMinute = _clock.ElapsedMinutes;

            var registry = new IslanderRegistry(_save.Islanders);
            var personalities = PersonalityRoster.CreateService();

            // Los dos catálogos leen sus JSON de Resources/Config en el constructor.
            var itemCatalog = new ItemCatalog();
            var furniture = new FurnitureCatalog();

            _simulation = new SimulationService(registry, personalities, _needsConfig);
            _economy = new EconomyService(itemCatalog, _save);
            _island = new IslandService(registry, _save);

            // La colocación se engancha después de construir la isla: el servicio de
            // isla pregunta dónde está cada edificio, y el de construcción necesita el
            // guardado, así que se montan por separado y se presentan aquí.
            _build = new BuildService(_save);
            _island.UseBuildService(_build);

            var factory = new IslanderFactory(registry, personalities, _clock,
                                              FoodIds(itemCatalog));

            // Con la lista de triángulos de la partida: duran seis días, así que tienen
            // que sobrevivir a cerrar el juego. Uno que solo viviera en memoria se
            // resolvería solo al cargar, sin que nadie lo viera.
            // Y con las banderas de la partida, que es donde el vecino anota lo que ya te
            // ha contado. Sin ellas todo funciona igual salvo al reabrir el juego: la
            // sesión nueva no sabría que ayer te contó lo de la riña y podría repetírtelo
            // mientras la historia siga fresca. Un vecino que repite es peor que uno que
            // calla, porque delata que no se acuerda de verdad.
            _social = new SocialService(registry, personalities, _simulation, factory,
                                        _clock, _socialConfig, _save.Triangles, _save.Weddings,
                                        _save.Flags);

            var housing = new HousingService(furniture, _save, registry);

            // Los adornos van después de la isla porque preguntan si la zona está
            // abierta: decorar un sitio que todavía no existe no tiene sentido.
            var decor = new DecorService(new DecorCatalog(), _save, _island);

            // Los logros se enteran de todo por el EventBus y no los llama nadie.
            // Se construyen los últimos para no perderse nada de lo que publiquen
            // los demás al montarse.
            _achievements = new AchievementService(new AchievementCatalog(), _save, _clock);

            // El protagonista. Se construye siempre, exista o no todavía: si la
            // partida es nueva, el creador de personajes lo rellenará y a partir de
            // ahí ya hay a quien seguir.
            _player = new PlayerService(_save.Player, _clock);

            // Las cinco vías. Va antes que el huerto, la recolección y el crafteo
            // porque se suscribe a lo que publican y tiene que estar escuchando desde
            // el primer hachazo. No llama a nadie: solo cuenta lo que la isla ya
            // contaba.
            _progression = new PlayerProgressionService(_save.Player, _progressionConfig);

            // La conducta observada (§14.3). Va con la progresión porque es la otra
            // mitad de «quién es el protagonista»: una cuenta lo que sabe hacer y la
            // otra cómo lo hace. Se la alimentan el cuerpo, el buscador de objetivo y
            // el social; ella sola escucha lo que fabrica.
            _conduct = new ConductService(_save.Player);

            // El orden aquí sí manda: la mochila la necesitan los otros tres, y el
            // crafteo necesita además la isla para saber de qué nivel va.
            _inventory = new InventoryService(_save.Player, _economy);
            _farming = new FarmingService(new CropCatalog(), _save.Farm, _inventory);
            // Con la isla delante: es lo que le deja esquivar los edificios al sembrar.
            _gathering = new GatheringService(new NodeCatalog(), _save.Gathering,
                                              _inventory, _clock, _build);
            _crafting = new CraftingService(new RecipeCatalog(), _inventory, _island);

            // Las ampliaciones de casa van aquí abajo y no con el resto de vivienda
            // porque cobran la obra de la mochila: hasta que no existe el inventario no
            // se pueden construir.
            _homeUpgrades = new HomeUpgradeService(_save, registry, _economy, _inventory);

            // La paga se engancha aquí y no al encender la partida, y no es un
            // detalle: poblar una isla nueva ya desbloquea logros —el primer
            // edificio, el primer amigo— y esos avisos salen dentro de este mismo
            // Build. Suscribiéndose después, los primeros logros de cada partida se
            // conseguían y no pagaban ni un nimbo. Se veía en el guardado: dos
            // logros hechos y las monedas intactas.
            EventBus.Subscribe<AchievementUnlocked>(OnAchievementUnlocked);

            // Las peticiones van después de la mochila y la recolección, y ahora sí es
            // un orden con motivo: el generador pregunta a los nodos qué materiales
            // suelta la isla para no encargar nada imposible, y el servicio cobra de la
            // mochila y de la despensa lo que el vecino haya pedido.
            var generator = new RequestGenerator(registry, personalities, _requestConfig,
                                                 _gathering, _economy);
            _requests = new RequestService(registry, _simulation, generator, _requestConfig,
                                           _clock, _inventory, _economy);
            _requests.LoadFrom(_save.Requests);

            // El planificador de bodas. Va después del social porque necesita casarlos,
            // y se suscribe a DayPassed él solo: nadie le llama, se entera del calendario.
            _weddings = new WeddingPlanner(_save, registry, _social, _simulation);

            // El módulo de eventos: sucesos, sueños, conciertos y el tablón de noticias.
            //
            // Estaba escrito y probado y **no lo construía nadie**, así que no existía en
            // el juego. Se enciende aquí, con la partida y el reloj, para que la crónica
            // se guarde: lo que uno quiere leer al volver es justo lo que pasó mientras
            // no estaba, y un tablón que solo viva en memoria se vacía en ese momento.
            // Sin `using` y con el nombre entero: `using Nimbo.Core.Events` ya está
            // puesto arriba, y con los dos importados «Events» se vuelve ambiguo.
            _events = new Nimbo.Events.EventsService(null, _save, _clock);

            // Los tres minijuegos. Estaban escritos, probados y sin construir por nadie:
            // lógica pura sin arranque, sin pantalla y sin premio. Van detrás del módulo
            // de eventos porque el de ritmo pregunta al calendario si hay concierto
            // puesto — tocar en la fiesta del pueblo tiene que notarse en el pueblo.
            _minigames = new Nimbo.Events.Minigames.MinigameService(null, _events.Scheduler);

            // Poner una fiesta en el calendario (§15.3), y que de ella salga algo: en
            // una fiesta los vecinos coinciden, y ahí nacen los flechazos.
            _villageEvents = new Nimbo.Events.Scheduling.VillageEvents(_events.Scheduler, _clock);
            _sparks = new Nimbo.Events.Scheduling.EventSparks(_events.Scheduler);

            _brain = new IslanderBrain(registry, _island, personalities, _social, _clock);
            _jobs = new JobService(registry, _simulation, _island, _clock);
            _tree = new NimboTree(_save, _clock, registry);
            _wardrobe = new WardrobeService(registry, _simulation, personalities);
            _gifts = new GiftService(registry, _simulation, personalities, _inventory, _wardrobe);

            // El orden de registro da igual, pero el de construcción no: EconomyService
            // busca ISimulationService por el registro cuando alguien hace un regalo,
            // así que tiene que estar puesto antes de que corra el primer fotograma.
            ServiceRegistry.Register<GameClock>(_clock);
            ServiceRegistry.Register<IIslanderRegistry>(registry);
            ServiceRegistry.Register<IPersonalityService>(personalities);
            ServiceRegistry.Register<ISimulationService>(_simulation);
            ServiceRegistry.Register<IRequestService>(_requests);
            ServiceRegistry.Register<ISocialService>(_social);
            ServiceRegistry.Register<IHousingService>(housing);
            ServiceRegistry.Register<IHomeUpgradeService>(_homeUpgrades);
            ServiceRegistry.Register<IChronicleService>(_events.News);
            ServiceRegistry.Register<IMinigameService>(_minigames);
            ServiceRegistry.Register<IEconomyService>(_economy);
            ServiceRegistry.Register<IIslandService>(_island);
            ServiceRegistry.Register<IDecorService>(decor);
            ServiceRegistry.Register<IBuildService>(_build);
            ServiceRegistry.Register<IAchievementService>(_achievements);
            ServiceRegistry.Register<PlayerService>(_player);
            ServiceRegistry.Register<IPlayerProgression>(_progression);
            ServiceRegistry.Register<IConductService>(_conduct);
            ServiceRegistry.Register<IVillageEvents>(_villageEvents);
            ServiceRegistry.Register<IInventoryService>(_inventory);
            ServiceRegistry.Register<IFarmingService>(_farming);
            ServiceRegistry.Register<IGatheringService>(_gathering);
            ServiceRegistry.Register<ICraftingService>(_crafting);
            ServiceRegistry.Register<IIslanderFactory>(factory);
            ServiceRegistry.Register<IJobService>(_jobs);
            ServiceRegistry.Register<NimboTree>(_tree);
            ServiceRegistry.Register<ITreeService>(_tree);
            ServiceRegistry.Register<WardrobeService>(_wardrobe);
            ServiceRegistry.Register<IGiftService>(_gifts);

            if (isNewGame)
            {
                PopulateNewIsland(registry, factory);
                GiveStarterKit();
                CreateProtagonist(factory);
            }

            // Que nadie empiece en paro: la economía no cierra sin sueldos, y buscar
            // trabajo a mano para doce habitantes no es una decisión interesante.
            _jobs.EmployEveryone();

            _registry = registry;
            _built = true;
        }

        /// <summary>
        /// Crea al protagonista con lo que el jugador se hizo en el creador.
        /// </summary>
        /// <remarks>
        /// Si no viene nada —una partida arrancada desde un test, o desde el editor
        /// con el menú apagado— se sortea. Que exista siempre es más importante que
        /// que sea tuyo: sin él no hay a quien seguir y la isla se queda sin nadie a
        /// los mandos, que es peor que una cara al azar.
        ///
        /// Aparece al borde de la plaza y no en el centro: en el centro está el Árbol
        /// Nimbo, que mide veintiséis metros.
        /// </remarks>
        private void CreateProtagonist(IIslanderFactory factory)
        {
            if (_save.Player.Created) return;

            var look = _protagonistLook ?? factory.CreateRandom().Appearance;
            // El creador devuelve «Sin nombre» si dejas el campo vacío, y eso se lee
            // raro en la ficha y en los diálogos. Se trata igual que no haber puesto
            // nada: un nombre del banco es mejor que un hueco.
            string typed = _protagonistName?.Trim();
            string name = string.IsNullOrEmpty(typed) || typed == "Sin nombre"
                ? factory.CreateRandom().Identity.DisplayName
                : typed;

            // Al sur del huerto, con la parcela y la cabaña por delante.
            //
            // El lado importa tanto como la distancia. La cámara arranca detrás del
            // protagonista, o sea unos diez metros hacia -z: apareciendo al norte de
            // la parcela, la cámara caía justo encima de ella y el jugador miraba al
            // lado contrario — salías de espaldas a tu propio huerto. Desde aquí lo
            // primero que se ve es la tierra, y detrás la casa.
            _player.Create(name, look, Data.Player.PlayerHome.Spawn);
        }

        /// <summary>
        /// Lo que lleva encima el primer día: las herramientas y unas semillas.
        /// </summary>
        /// <remarks>
        /// Se da de salida en vez de venderlo porque sin azada no se puede empezar el
        /// huerto, y sin huerto el primer día es solo pasear. Un juego que te hace
        /// ahorrar antes de dejarte jugar empieza mal.
        /// </remarks>
        private void GiveStarterKit()
        {
            foreach (var tool in new[] { "tool_azada", "tool_regadera", "tool_hacha", "tool_pico" })
                _inventory.TryStore(tool, 1, out _);

            var crops = _farming.Crops;
            if (crops.Count > 0) _inventory.TryStore(crops[0].SeedId, 8, out _);

            // Cuatro muebles para estrenar la casa.
            //
            // Van al inventario grande y no a la mochila: los muebles se colocan desde
            // dentro de casa, no se llevan encima. Sin esto se entra a un cuarto vacío
            // con un menú de amueblar que dice «no tienes muebles», y la primera vez
            // que uno entra en su casa es justo cuando quiere tocar algo.
            //
            // La vela va aparte a propósito: es de las que se apoyan encima de un
            // mueble, así que obliga a poner antes la mesita y enseña la regla sola.
            foreach (var piece in new[] { "furn_silla_de_madera_sencilla",
                                          "furn_mesita_de_noche",
                                          "furn_maceta_de_girasol_radiante",
                                          "furn_vela_infinita_nimba" })
                _economy.Inventory.Add(piece, 1);
        }

        /// <summary>
        /// Los identificadores de comida del catálogo. Los usa la fábrica para
        /// sortearle a cada habitante lo que le encanta y lo que no traga.
        /// </summary>
        private static List<string> FoodIds(ItemCatalog catalog)
        {
            var ids = new List<string>();
            var all = catalog.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Category == ItemCategory.Food) ids.Add(all[i].CatalogId);
            return ids;
        }

        /// <summary>Crea configuraciones por defecto para poder arrancar sin assets.</summary>
        private void EnsureConfigs()
        {
            _needsConfig ??= ScriptableObject.CreateInstance<NeedsConfig>();
            _requestConfig ??= ScriptableObject.CreateInstance<RequestConfig>();
            _socialConfig ??= ScriptableObject.CreateInstance<SocialConfig>();
        }

        private SaveGame NewSave()
        {
            var save = new SaveGame
            {
                SaveId = System.Guid.NewGuid().ToString("N"),
                CreatedUtc = System.DateTime.UtcNow.ToString("O"),
                ElapsedMinutes = 8 * GameClock.MinutesPerHour,
            };
            save.Wallet.Coins = _startingCoins;
            return save;
        }

        private void PopulateNewIsland(IslanderRegistry registry, IIslanderFactory factory)
        {
            for (int i = 0; i < _starterIslanders; i++)
            {
                var islander = factory.CreateRandom();
                registry.Add(islander);
                _save.Islanders.Add(islander);
                EventBus.Publish(new IslanderCreated(islander.Id));

                if (!_island.TryAssignHome(islander))
                    Debug.LogWarning($"No quedaba casa para {islander.Identity.DisplayName}");

                _save.Island.ResidentIds.Add(islander.Id);
            }

            // Que se conozcan de entrada: una isla donde nadie ha hablado con nadie
            // tarda días de juego en producir la primera historia.
            var all = registry.All;
            for (int a = 0; a < all.Count; a++)
                for (int b = a + 1; b < all.Count; b++)
                    _social.Introduce(all[a].Id, all[b].Id);
        }

        /// <summary>
        /// Pone al día una partida que estuvo cerrada, con tope. El tope existe para
        /// que volver tras una semana no signifique encontrarse a todos hambrientos y
        /// enfadados: esto es un juego amable, no una penitencia.
        /// </summary>
        private void CatchUpOfflineTime()
        {
            const int MaxOfflineHours = 8;

            if (string.IsNullOrEmpty(_save.SavedUtc)) return;
            if (!System.DateTime.TryParse(_save.SavedUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var saved)) return;

            var away = System.DateTime.UtcNow - saved;
            if (away.TotalMinutes <= 0) return;

            // Un cuarto del tiempo real, y como mucho ocho horas de juego.
            int minutes = Mathf.Min((int)(away.TotalMinutes * 0.25f),
                                    MaxOfflineHours * GameClock.MinutesPerHour);
            if (minutes <= 0) return;

            Debug.Log($"Estuviste fuera {away.TotalHours:0.0} h reales: la isla avanza " +
                      $"{minutes / 60f:0.0} h de juego.");
            _clock.Advance(minutes);
        }

        private void Update()
        {
            if (!_running) return;

            _clock.Tick(UnityEngine.Time.deltaTime);
            _simulation.TickFrame(UnityEngine.Time.deltaTime);
            _player.Tick();

            if (_autosaveMinutes > 0 &&
                _clock.ElapsedMinutes - _lastAutosaveMinute >= _autosaveMinutes)
            {
                _lastAutosaveMinute = _clock.ElapsedMinutes;
                WriteSave();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) WriteSave();
        }

        private void OnApplicationQuit() => WriteSave();

        public void WriteSave()
        {
            if (_save == null) return;

            _save.ElapsedMinutes = _clock.ElapsedMinutes;
            _save.Requests = new List<Data.Requests.IslanderRequest>(_requests.Open);
            SaveSystem.Write(_save);
        }

        private void OnDestroy()
        {
            _simulation?.Dispose();
            _requests?.Dispose();
            _social?.Dispose();
            _island?.Dispose();
            _brain?.Dispose();
            _economy?.Dispose();
            _jobs?.Dispose();
            _achievements?.Dispose();
            _weddings?.Dispose();
            _events?.Dispose();
            _progression?.Dispose();
            _conduct?.Dispose();
            _sparks?.Dispose();
            EventBus.Unsubscribe<DayPassed>(OnDayPassed);
            EventBus.Unsubscribe<AchievementUnlocked>(OnAchievementUnlocked);
            ServiceRegistry.Clear();
        }
    }
}
