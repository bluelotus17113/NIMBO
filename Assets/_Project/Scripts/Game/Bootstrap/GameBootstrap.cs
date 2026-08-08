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
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Behaviour;
using Nimbo.Simulation.Needs;
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

        private GameClock _clock;
        private SaveGame _save;
        private SimulationService _simulation;
        private RequestService _requests;
        private SocialService _social;
        private IslandService _island;
        private IslanderBrain _brain;
        private EconomyService _economy;

        private long _lastAutosaveMinute;
        private bool _running;

        public GameClock Clock => _clock;
        public SaveGame Save => _save;

        private void Awake()
        {
            // Persistente entre escenas: la simulación no puede pararse porque el
            // jugador entre al editor de interiores.
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Build()
        {
            EnsureConfigs();

            _save = SaveSystem.Read();
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

            var generator = new RequestGenerator(registry, personalities, _requestConfig);
            _requests = new RequestService(registry, _simulation, generator, _requestConfig, _clock);
            _requests.LoadFrom(_save.Requests);

            _brain = new IslanderBrain(registry, _island, personalities, _social, _clock);

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
            ServiceRegistry.Register<IIslanderFactory>(factory);

            if (isNewGame) PopulateNewIsland(registry, factory);

            CatchUpOfflineTime();

            _running = true;
            EventBus.Publish(new GameLoaded());
            Debug.Log($"Isla Nimbo lista — {registry.Count} habitantes, {_clock}");
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
            ServiceRegistry.Clear();
        }
    }
}
