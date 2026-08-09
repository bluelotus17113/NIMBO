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

    public readonly struct ContinueRequested { }

    /// <summary>Volver al menú principal desde la partida, guardando antes.</summary>
    public readonly struct ReturnToMenuRequested { }

    public readonly struct QuitRequested { }

    public readonly struct GamePaused
    {
        public readonly bool Paused;
        public GamePaused(bool paused) => Paused = paused;
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
