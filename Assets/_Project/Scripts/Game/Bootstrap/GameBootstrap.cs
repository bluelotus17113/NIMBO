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
using Nimbo.Island.Decor;
using Nimbo.Personality.Runtime;
using Nimbo.Player;
using Nimbo.Simulation;
using Nimbo.Simulation.Behaviour;
using Nimbo.Simulation.Jobs;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Progression;
using Nimbo.Simulation.Wardrobe;
using Nimbo.Simulation.Requests;
using Nimbo.Social;
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

        private GameClock _clock;
        private SaveGame _save;
        private SimulationService _simulation;
        private RequestService _requests;
        private SocialService _social;
        private IslandService _island;
        private IslanderBrain _brain;
        private EconomyService _economy;
        private JobService _jobs;
        private NimboTree _tree;
        private WardrobeService _wardrobe;
        private AchievementService _achievements;
        private PlayerService _player;

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
        public void StartGame(bool newGame)
        {
            if (_built) return;

            _forceNewGame = newGame;
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

        private void OnDayPassed(DayPassed evt) => ApplyDayRhythm(evt.Day);

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

            var factory = new IslanderFactory(registry, personalities, _clock,
                                              FoodIds(itemCatalog));

            _social = new SocialService(registry, personalities, _simulation, factory,
                                        _clock, _socialConfig);

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

            // La paga se engancha aquí y no al encender la partida, y no es un
            // detalle: poblar una isla nueva ya desbloquea logros —el primer
            // edificio, el primer amigo— y esos avisos salen dentro de este mismo
            // Build. Suscribiéndose después, los primeros logros de cada partida se
            // conseguían y no pagaban ni un nimbo. Se veía en el guardado: dos
            // logros hechos y las monedas intactas.
            EventBus.Subscribe<AchievementUnlocked>(OnAchievementUnlocked);

            var generator = new RequestGenerator(registry, personalities, _requestConfig);
            _requests = new RequestService(registry, _simulation, generator, _requestConfig, _clock);
            _requests.LoadFrom(_save.Requests);

            _brain = new IslanderBrain(registry, _island, personalities, _social, _clock);
            _jobs = new JobService(registry, _simulation, _island, _clock);
            _tree = new NimboTree(_save, _clock, registry);
            _wardrobe = new WardrobeService(registry, _simulation, personalities);

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
            ServiceRegistry.Register<IEconomyService>(_economy);
            ServiceRegistry.Register<IIslandService>(_island);
            ServiceRegistry.Register<IDecorService>(decor);
            ServiceRegistry.Register<IAchievementService>(_achievements);
            ServiceRegistry.Register<PlayerService>(_player);
            ServiceRegistry.Register<IIslanderFactory>(factory);
            ServiceRegistry.Register<IJobService>(_jobs);
            ServiceRegistry.Register<NimboTree>(_tree);
            ServiceRegistry.Register<WardrobeService>(_wardrobe);

            if (isNewGame) PopulateNewIsland(registry, factory);

            // Que nadie empiece en paro: la economía no cierra sin sueldos, y buscar
            // trabajo a mano para doce habitantes no es una decisión interesante.
            _jobs.EmployEveryone();

            _registry = registry;
            _built = true;
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
            EventBus.Unsubscribe<DayPassed>(OnDayPassed);
            EventBus.Unsubscribe<AchievementUnlocked>(OnAchievementUnlocked);
            ServiceRegistry.Clear();
        }
    }
}
