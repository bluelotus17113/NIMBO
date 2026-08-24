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

    /// <summary>Tu pareja ha echado una mano en el huerto (§14.5).</summary>
    /// <remarks>
    /// Trae la casilla porque quien dibuja el huerto tiene que repintarla: sin eso, la
    /// tierra sigue seca en pantalla hasta que el jugador pase por encima, y lo único
    /// que se nota de tener pareja es que no se nota.
    /// </remarks>
    public readonly struct SpouseHelped
    {
        public readonly string IslanderId;
        public readonly int X;
        public readonly int Y;

        public SpouseHelped(string islanderId, int x, int y)
        {
            IslanderId = islanderId; X = x; Y = y;
        }
    }

    /// <summary>Te han contestado a la declaración (§14).</summary>
    /// <remarks>
    /// Trae la frase hecha porque el «no» tiene que **enseñar**: nombra el eje que más
    /// lejos quedó, y con eso el jugador aprende el sistema sin tutorial y sin ver un
    /// número. Componerla aquí y no en la pantalla es lo que permite que la cuenten a
    /// la vez el cartel y la crónica sin escribirla dos veces.
    /// </remarks>
    public readonly struct CourtshipAnswered
    {
        public readonly string IslanderId;
        public readonly bool Accepted;
        public readonly string Line;

        public CourtshipAnswered(string islanderId, bool accepted, string line)
        {
            IslanderId = islanderId; Accepted = accepted; Line = line;
        }
    }

    /// <summary>Una de las cinco vías del protagonista ha subido.</summary>
    public readonly struct SkillLeveledUp
    {
        public readonly Data.Player.SkillKind Skill;
        public readonly int NewLevel;
        public SkillLeveledUp(Data.Player.SkillKind skill, int newLevel)
        {
            Skill = skill; NewLevel = newLevel;
        }
    }

    /// <summary>Se ha ganado algo nuevo que hacer.</summary>
    /// <remarks>
    /// Va aparte de <see cref="SkillLeveledUp"/> aunque llegue en el mismo momento:
    /// subir de nivel pasa siempre y se cuenta con un número, y desbloquear algo pasa
    /// de vez en cuando y hay que explicarlo con palabras.
    /// </remarks>
    public readonly struct UnlockGained
    {
        public readonly Services.Contracts.Unlock Unlock;
        public UnlockGained(Services.Contracts.Unlock unlock) => Unlock = unlock;
    }

    /// <summary>
    /// El jugador quiere ponerse a jugar a uno de los tres.
    /// </summary>
    /// <remarks>
    /// Lo publica quien tiene el sitio —el borde de la isla, el escenario, la pestaña
    /// de cocina— y lo recoge la interfaz, que es la única que sabe abrir una ventana.
    /// Sin esto, el mundo tendría que conocer los paneles.
    /// </remarks>
    public readonly struct MinigameRequested
    {
        public readonly Services.Contracts.MinigameKind Kind;
        public readonly int Difficulty;

        /// <summary>Para la cocina, el id de la receta. Los otros dos no lo usan.</summary>
        public readonly string Context;

        public MinigameRequested(Services.Contracts.MinigameKind kind, int difficulty,
                                 string context = null)
        {
            Kind = kind; Difficulty = difficulty; Context = context;
        }
    }

    /// <summary>El jugador se ha puesto delante del tablón de la plaza y ha pulsado.</summary>
    /// <remarks>
    /// Un aviso propio en vez de colarlo por <see cref="StationUsed"/>: el tablón no es
    /// una mesa de trabajo, y meterlo en ese enum obliga a quien lo lea a preguntarse
    /// qué se craftea ahí.
    /// </remarks>
    public readonly struct RequestBoardRead { }

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

    /// <summary>
    /// Hay boda, y falta tal día. Lo publica el planificador con antelación.
    /// </summary>
    /// <remarks>
    /// Va con días de aviso a propósito: la boda de dos vecinos ocurre sin el jugador
    /// —es su historia, no la suya— pero enterarse a toro pasado no tiene ninguna
    /// gracia. Con el aviso se puede ir, y no ir también significa algo.
    /// </remarks>
    public readonly struct WeddingAnnounced
    {
        public readonly string AId;
        public readonly string BId;
        /// <summary>Día de juego en que se celebra.</summary>
        public readonly int Day;
        public WeddingAnnounced(string aId, string bId, int day)
        {
            AId = aId; BId = bId; Day = day;
        }
    }

    /// <summary>Se han casado. Ya está hecho cuando esto se publica.</summary>
    public readonly struct WeddingHeld
    {
        public readonly string AId;
        public readonly string BId;
        public WeddingHeld(string aId, string bId)
        {
            AId = aId; BId = bId;
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

    /// <summary>Ha empezado algo en la aldea: un concierto, un festival, un mercadillo.</summary>
    /// <remarks>
    /// El calendario ya avisaba de esto con eventos de C# (<c>EventScheduler.EventStarted</c>),
    /// pero solo se le puede escuchar desde dentro de <c>Nimbo.Events</c>. El sonido y la
    /// interfaz viven fuera y solo ven <c>Nimbo.Core</c>, así que lo mismo sale también
    /// por el bus. Los dos avisos son a propósito: el de dentro lleva la definición
    /// entera, este lleva lo que necesita quien está lejos.
    /// </remarks>
    public readonly struct VillageEventStarted
    {
        public readonly string EventId;
        public readonly string DisplayName;
        public readonly int Hour;
        public VillageEventStarted(string eventId, string displayName, int hour)
        {
            EventId = eventId; DisplayName = displayName; Hour = hour;
        }
    }

    public readonly struct VillageEventEnded
    {
        public readonly string EventId;
        public VillageEventEnded(string eventId) => EventId = eventId;
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

    /// <summary>
    /// Le ha dado a un nodo y aún aguanta. Va aparte de <see cref="NodeGathered"/>,
    /// que solo se publica cuando cae.
    /// </summary>
    /// <remarks>
    /// Un abedul son seis hachazos. Sin este aviso, los cinco primeros no producen
    /// nada —ni ruido, ni movimiento, ni un número— y se leen como que la herramienta
    /// no sirve. Es el aviso que necesita quien dibuja para sacudir el árbol y quien
    /// pone el sonido para que suene el hacha.
    /// </remarks>
    public readonly struct NodeHit
    {
        public readonly string InstanceId;
        public readonly string NodeId;

        /// <summary>Golpes que le quedan después de este. Nunca cero: eso es caer.</summary>
        public readonly int HitsLeft;

        public NodeHit(string instanceId, string nodeId, int hitsLeft)
        {
            InstanceId = instanceId; NodeId = nodeId; HitsLeft = hitsLeft;
        }
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

    /// <summary>
    /// Un nodo se ha apartado de donde estaba. Le han puesto un edificio encima.
    /// </summary>
    /// <remarks>
    /// Va aparte de <see cref="NodeRespawned"/> aunque quien dibuja haga lo mismo con
    /// los dos —rehacerlo—, porque no significan lo mismo: uno es «ha vuelto» y este
    /// es «se ha movido», y el día que alguien quiera contar cuántos han vuelto no
    /// tendrá que averiguar cuáles de esos avisos eran mudanzas.
    /// </remarks>
    public readonly struct NodeMoved
    {
        public readonly string InstanceId;
        public NodeMoved(string instanceId) => InstanceId = instanceId;
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
    /// <summary>
    /// El Árbol Nimbo ha hablado, haya dado lo suyo o ya lo hayan cobrado. El texto va
    /// compuesto porque quien lo publica ve tipos que la interfaz no conoce.
    /// </summary>
    public readonly struct TreeSpoke
    {
        public readonly string Text;
        public readonly int Coins;
        public readonly string ItemId;

        public TreeSpoke(string text, int coins, string itemId)
        { Text = text; Coins = coins; ItemId = itemId; }
    }

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

    /// <summary>
    /// Hace falta el puntero: hay algo abierto que se clica.
    /// </summary>
    /// <remarks>
    /// Va por aviso porque quien lo sabe y quien lo necesita no se ven: los paneles
    /// están en <c>Nimbo.UI</c> y la cámara en <c>Nimbo.Art</c>, y esos dos ensamblados
    /// no se referencian. Sin esto, la cámara tendría que buscar paneles por la
    /// jerarquía o habría que enganchar <c>Nimbo.Art</c> a la interfaz entera para
    /// preguntarle una sola cosa.
    ///
    /// Lo pide la mochila, la ficha de un vecino, la tienda, el mapa, el modo
    /// construcción, el creador de personajes, el panel de pruebas y la pausa. Lo que
    /// tienen en común es que todos se manejan clicando, y con el ratón capturado para
    /// girar la cámara no se puede clicar nada.
    /// </remarks>
    public readonly struct PointerNeeded
    {
        public readonly bool Needed;
        public PointerNeeded(bool needed) => Needed = needed;
    }

    public readonly struct GamePaused
    {
        public readonly bool Paused;
        public GamePaused(bool paused) => Paused = paused;
    }

    /// <summary>
    /// Se ha entrado o salido del modo decorar, que solo existe en la calle.
    /// </summary>
    /// <remarks>
    /// Es hermano de <c>BuildModeChanged</c> y <c>FurnishModeChanged</c>, aunque este
    /// no mueve la cámara: decorar se hace sobre un plano cenital dibujado en la
    /// propia interfaz. Lo que comparte con ellos es lo que importa — se come la
    /// pantalla, apaga el resto de la interfaz y hay que salir de él.
    /// </remarks>
    public readonly struct DecorModeChanged
    {
        public readonly bool Decorating;
        public DecorModeChanged(bool decorating) => Decorating = decorating;
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
