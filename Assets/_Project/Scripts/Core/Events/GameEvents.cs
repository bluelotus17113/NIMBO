using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Data.Social;

namespace Nimbo.Core.Events
{
    // Todos los eventos del juego viven en este fichero, y solo el orquestador lo
    // edita. Tenerlos juntos es lo que permite ver de un vistazo si dos módulos se
    // están hablando por la espalda.
    //
    // Convención: nombre en pasado (algo YA pasó). Un evento no es una orden.

    // --- ciclo de vida del habitante ---------------------------------------

    public readonly struct IslanderCreated
    {
        public readonly string IslanderId;
        public IslanderCreated(string islanderId) => IslanderId = islanderId;
    }

    public readonly struct IslanderMovedIn
    {
        public readonly string IslanderId;
        public readonly string BuildingId;
        public readonly int UnitIndex;
        public IslanderMovedIn(string islanderId, string buildingId, int unitIndex)
        {
            IslanderId = islanderId; BuildingId = buildingId; UnitIndex = unitIndex;
        }
    }

    public readonly struct IslanderLeft
    {
        public readonly string IslanderId;
        public IslanderLeft(string islanderId) => IslanderId = islanderId;
    }

    // --- necesidades y ánimo -----------------------------------------------

    public readonly struct NeedBandChanged
    {
        public readonly string IslanderId;
        public readonly NeedKind Need;
        public readonly NeedBand From;
        public readonly NeedBand To;
        public NeedBandChanged(string islanderId, NeedKind need, NeedBand from, NeedBand to)
        {
            IslanderId = islanderId; Need = need; From = from; To = to;
        }
    }

    public readonly struct EmotionShown
    {
        public readonly string IslanderId;
        public readonly Emotion Emotion;
        public readonly float Seconds;
        public EmotionShown(string islanderId, Emotion emotion, float seconds)
        {
            IslanderId = islanderId; Emotion = emotion; Seconds = seconds;
        }
    }

    public readonly struct HappinessChanged
    {
        public readonly string IslanderId;
        public readonly float From;
        public readonly float To;
        public HappinessChanged(string islanderId, float from, float to)
        {
            IslanderId = islanderId; From = from; To = to;
        }
    }

    // --- peticiones ---------------------------------------------------------

    public readonly struct RequestRaised
    {
        public readonly IslanderRequest Request;
        public RequestRaised(IslanderRequest request) => Request = request;
    }

    public readonly struct RequestResolved
    {
        public readonly string RequestId;
        public readonly string IslanderId;
        public readonly bool Satisfied;
        public RequestResolved(string requestId, string islanderId, bool satisfied)
        {
            RequestId = requestId; IslanderId = islanderId; Satisfied = satisfied;
        }
    }

    public readonly struct RequestExpired
    {
        public readonly string RequestId;
        public readonly string IslanderId;
        public RequestExpired(string requestId, string islanderId)
        {
            RequestId = requestId; IslanderId = islanderId;
        }
    }

    // --- progresión ---------------------------------------------------------

    public readonly struct IslanderLeveledUp
    {
        public readonly string IslanderId;
        public readonly int NewLevel;
        public IslanderLeveledUp(string islanderId, int newLevel)
        {
            IslanderId = islanderId; NewLevel = newLevel;
        }
    }

    // --- social -------------------------------------------------------------

    public readonly struct AffinityChanged
    {
        public readonly string FromId;
        public readonly string ToId;
        public readonly float Delta;
        public readonly float NewValue;
        public AffinityChanged(string fromId, string toId, float delta, float newValue)
        {
            FromId = fromId; ToId = toId; Delta = delta; NewValue = newValue;
        }
    }

    public readonly struct FriendshipStageChanged
    {
        public readonly string FromId;
        public readonly string ToId;
        public readonly FriendshipStage Stage;
        public FriendshipStageChanged(string fromId, string toId, FriendshipStage stage)
        {
            FromId = fromId; ToId = toId; Stage = stage;
        }
    }

    public readonly struct ConflictStageChanged
    {
        public readonly string FromId;
        public readonly string ToId;
        public readonly ConflictStage Stage;
        public ConflictStageChanged(string fromId, string toId, ConflictStage stage)
        {
            FromId = fromId; ToId = toId; Stage = stage;
        }
    }

    public readonly struct RomanceStageChanged
    {
        public readonly string FromId;
        public readonly string ToId;
        public readonly RomanceStage Stage;
        public RomanceStageChanged(string fromId, string toId, RomanceStage stage)
        {
            FromId = fromId; ToId = toId; Stage = stage;
        }
    }

    public readonly struct BabyBorn
    {
        public readonly string ParentAId;
        public readonly string ParentBId;
        public readonly string ChildId;
        public BabyBorn(string parentAId, string parentBId, string childId)
        {
            ParentAId = parentAId; ParentBId = parentBId; ChildId = childId;
        }
    }

    // --- economía -----------------------------------------------------------

    public readonly struct CoinsChanged
    {
        public readonly long Delta;
        public readonly long Total;
        public readonly string Reason;
        public CoinsChanged(long delta, long total, string reason)
        {
            Delta = delta; Total = total; Reason = reason;
        }
    }

    public readonly struct ItemAcquired
    {
        public readonly string CatalogId;
        public readonly int Quantity;
        public ItemAcquired(string catalogId, int quantity)
        {
            CatalogId = catalogId; Quantity = quantity;
        }
    }

    public readonly struct ItemGifted
    {
        public readonly string CatalogId;
        public readonly string IslanderId;
        public readonly int Opinion;   // -1 lo odia, 0 indiferente, +1 le encanta
        public ItemGifted(string catalogId, string islanderId, int opinion)
        {
            CatalogId = catalogId; IslanderId = islanderId; Opinion = opinion;
        }
    }

    // --- mundo y tiempo -----------------------------------------------------

    public readonly struct HourPassed
    {
        public readonly int Hour;      // 0-23
        public readonly int Day;       // día de juego, empezando en 1
        public HourPassed(int hour, int day) { Hour = hour; Day = day; }
    }

    public readonly struct DayPassed
    {
        public readonly int Day;
        public DayPassed(int day) => Day = day;
    }

    public readonly struct BuildingUnlocked
    {
        public readonly string BuildingId;
        public BuildingUnlocked(string buildingId) => BuildingId = buildingId;
    }

    public readonly struct HomeUpgraded
    {
        public readonly string IslanderId;
        public readonly int Level;
        public readonly int Size;     // casillas de lado tras la ampliación
        public HomeUpgraded(string islanderId, int level, int size)
        {
            IslanderId = islanderId; Level = level; Size = size;
        }
    }

    // --- la aldea: mochila, huerto, recolección y crafteo --------------------

    public readonly struct InventoryChanged
    {
        public readonly int Slot;   // -1 si cambió más de uno
        public InventoryChanged(int slot) => Slot = slot;
    }

    public readonly struct SlotSelected
    {
        public readonly int Slot;
        public SlotSelected(int slot) => Slot = slot;
    }

    public readonly struct NodeGathered
    {
        public readonly string InstanceId;
        public readonly string DropId;
        public readonly int Quantity;
        public NodeGathered(string instanceId, string dropId, int quantity)
        {
            InstanceId = instanceId; DropId = dropId; Quantity = quantity;
        }
    }

    public readonly struct NodeRespawned
    {
        public readonly string InstanceId;
        public NodeRespawned(string instanceId) => InstanceId = instanceId;
    }

    public readonly struct TileChanged
    {
        public readonly int X;
        public readonly int Y;
        public TileChanged(int x, int y) { X = x; Y = y; }
    }

    public readonly struct CropHarvested
    {
        public readonly string CropId;
        public readonly int Quantity;
        public CropHarvested(string cropId, int quantity)
        {
            CropId = cropId; Quantity = quantity;
        }
    }

    public readonly struct ItemCrafted
    {
        public readonly string RecipeId;
        public readonly string OutputId;
        public readonly int Quantity;
        public ItemCrafted(string recipeId, string outputId, int quantity)
        {
            RecipeId = recipeId; OutputId = outputId; Quantity = quantity;
        }
    }

    // --- interiores ---------------------------------------------------------

    /// <summary>
    /// Ha entrado en una casa. La clave dice de quién: vacía es la del jugador.
    /// </summary>
    public readonly struct InteriorEntered
    {
        public readonly string HomeKey;
        public readonly string DisplayName;
        public InteriorEntered(string homeKey, string displayName)
        {
            HomeKey = homeKey; DisplayName = displayName;
        }
    }

    /// <summary>Ha salido a la calle.</summary>
    public readonly struct InteriorExited { }

    /// <summary>
    /// Se ha entrado o salido del modo amueblar, que solo existe dentro de una casa.
    /// </summary>
    public readonly struct FurnishModeChanged
    {
        public readonly bool Furnishing;
        public FurnishModeChanged(bool furnishing) => Furnishing = furnishing;
    }

    /// <summary>
    /// Qué mueble se está colocando. Vacío para no colocar nada.
    /// </summary>
    /// <remarks>
    /// Va por aviso y no por reflexión como el menú de construir. Aquello se hizo así
    /// para que la interfaz no tuviera que ver el ensamblado del arte, pero busca «la
    /// primera pieza con una propiedad que se llame Selected» y ahora hay dos que
    /// colocan cosas: la primera que apareciera en la lista se quedaría con lo que
    /// elige el jugador en la otra.
    /// </remarks>
    public readonly struct FurnishSelectionChanged
    {
        public readonly string CatalogId;
        public FurnishSelectionChanged(string catalogId) => CatalogId = catalogId;
    }

    /// <summary>Un edificio de la aldea se ha puesto o se ha movido de sitio.</summary>
    public readonly struct BuildingMoved
    {
        public readonly string ZoneId;
        public BuildingMoved(string zoneId) => ZoneId = zoneId;
    }

    /// <summary>Se ha entrado o salido del modo construcción.</summary>
    public readonly struct BuildModeChanged
    {
        public readonly bool Building;
        public BuildModeChanged(bool building) => Building = building;
    }

    /// <summary>Ha dormido. El reloj ya está en la mañana siguiente.</summary>
    public readonly struct Slept
    {
        public readonly int Day;
        public Slept(int day) => Day = day;
    }

    /// <summary>
    /// Se ha puesto delante de un sitio de trabajo. La interfaz abre lo que toque.
    /// </summary>
    /// <remarks>
    /// Va por evento y no llamando a la interfaz porque quien lo detecta vive en
    /// <c>Nimbo.Art</c> y los paneles en <c>Nimbo.UI</c>, que no se ven entre sí.
    /// </remarks>
    public readonly struct StationUsed
    {
        public readonly CraftStationKind Station;
        public StationUsed(CraftStationKind station) => Station = station;
    }

    /// <summary>Qué sitio de trabajo se ha usado.</summary>
    public enum CraftStationKind { Bench = 0, Kitchen = 1, Shipping = 2 }

    /// <summary>El protagonista ya existe y está puesto en el mundo.</summary>
    public readonly struct PlayerSpawned { }

    public readonly struct VigorChanged
    {
        public readonly float Vigor;
        public VigorChanged(float vigor) => Vigor = vigor;
    }

    // --- logros -------------------------------------------------------------

    public readonly struct AchievementUnlocked
    {
        public readonly string AchievementId;
        public readonly long Reward;
        public AchievementUnlocked(string achievementId, long reward)
        {
            AchievementId = achievementId; Reward = reward;
        }
    }

    // --- adornos de la isla -------------------------------------------------

    public readonly struct DecorPlaced
    {
        public readonly string PlacementId;
        public readonly string CatalogId;
        public readonly string ZoneId;
        public DecorPlaced(string placementId, string catalogId, string zoneId)
        {
            PlacementId = placementId; CatalogId = catalogId; ZoneId = zoneId;
        }
    }

    public readonly struct DecorRemoved
    {
        public readonly string PlacementId;
        public DecorRemoved(string placementId) => PlacementId = placementId;
    }

    public readonly struct DecorMoved
    {
        public readonly string PlacementId;
        public DecorMoved(string placementId) => PlacementId = placementId;
    }

    /// <summary>
    /// El jugador ha puesto la atención en alguien. La cámara se acerca a mirarlo.
    /// Con el identificador vacío significa que ha dejado de mirar y se vuelve al
    /// plano general de la isla.
    /// </summary>
    public readonly struct IslanderFocused
    {
        public readonly string IslanderId;
        public IslanderFocused(string islanderId) => IslanderId = islanderId;
    }

    // --- vivienda -----------------------------------------------------------

    public readonly struct RoomEdited
    {
        public readonly string BuildingId;
        public readonly int UnitIndex;
        public RoomEdited(string buildingId, int unitIndex)
        {
            BuildingId = buildingId; UnitIndex = unitIndex;
        }
    }

    // --- partida ------------------------------------------------------------

    public readonly struct GameLoaded { }

    public readonly struct GameSaved
    {
        public readonly string Path;
        public GameSaved(string path) => Path = path;
    }

    // --- menú y flujo -------------------------------------------------------
    //
    // Son los únicos eventos que rompen la convención del pasado, y a propósito:
    // el menú no sabe cómo se enciende una partida ni tiene por qué. Publica lo
    // que el jugador ha pedido y quien sepa hacerlo lo hace. Es lo que permite
    // que Nimbo.UI no dependa de Nimbo.Game.

    public readonly struct NewGameRequested { }

    /// <summary>
    /// El jugador ya se ha hecho a sí mismo en el creador. La partida puede empezar.
    /// </summary>
    /// <remarks>
    /// Va aparte de <c>NewGameRequested</c> porque entre las dos cosas hay una
    /// pantalla: pedir partida nueva abre el creador, y es terminar el creador lo que
    /// enciende la isla. Sin este paso intermedio no habría dónde meter el creador
    /// salvo dentro del arranque, que es justo el sitio donde no puede ir.
    /// </remarks>
    public readonly struct ProtagonistCreated
    {
        public readonly string DisplayName;
        public readonly Data.Islanders.AppearanceData Appearance;
        public ProtagonistCreated(string displayName, Data.Islanders.AppearanceData appearance)
        {
            DisplayName = displayName; Appearance = appearance;
        }
    }

    public readonly struct ContinueRequested { }

    /// <summary>Volver al menú principal desde la partida, guardando antes.</summary>
    public readonly struct ReturnToMenuRequested { }

    public readonly struct QuitRequested { }

    public readonly struct GamePaused
    {
        public readonly bool Paused;
        public GamePaused(bool paused) => Paused = paused;
    }

    /// <summary>
    /// El menú se ha abierto o cerrado. No es una pausa: el reloj sigue y los vecinos
    /// siguen a lo suyo. Lo que se para es el protagonista, para que mirar la mochila
    /// no sea andar a ciegas con media isla tapada.
    /// </summary>
    public readonly struct MenuOpened
    {
        public readonly bool Open;
        public MenuOpened(bool open) => Open = open;
    }

    public enum AudioChannel { Music = 0, Sfx = 1, Voice = 2 }

    public readonly struct VolumeChanged
    {
        public readonly AudioChannel Channel;
        public readonly float Value;   // 0-1
        public VolumeChanged(AudioChannel channel, float value)
        {
            Channel = channel; Value = value;
        }
    }
}
