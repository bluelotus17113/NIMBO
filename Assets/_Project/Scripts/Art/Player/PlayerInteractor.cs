using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.PlayerView
{
    /// <summary>Con qué está a punto de interactuar el jugador.</summary>
    public enum TargetKind { None = 0, Islander = 1, Node = 2, FarmTile = 3,
                             Hammock = 4, Bench = 5, ShippingBox = 6,
                             Door = 7, Exit = 8, Food = 9 }

    /// <summary>
    /// Lo que el jugador tiene delante y qué pasa si pulsa.
    /// </summary>
    /// <remarks>
    /// Busca por **proximidad y ángulo**, no por raycast desde el ratón. Con una
    /// cámara que orbita, apuntar con el ratón obliga al jugador a corregir la puntería
    /// cada vez que gira la vista; con la proximidad basta con acercarse y mirar, que
    /// es lo que uno hace de todas formas.
    ///
    /// El objetivo se recalcula a ritmo lento, no cada fotograma: recorrer todos los
    /// nodos de la isla sesenta veces por segundo no cambia nada que el jugador pueda
    /// notar y es lo que calienta el portátil.
    /// </remarks>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("Hasta dónde llega el brazo, en metros.")]
        [SerializeField] private float _reach = 2.6f;

        [Tooltip("Cono por delante en el que cuenta lo que hay, en grados.")]
        [SerializeField, Range(20f, 180f)] private float _cone = 110f;

        [SerializeField] private float _refreshInterval = 0.15f;

        private PlayerBody _body;
        private Nimbo.Player.PlayerService _player;
        private IGatheringService _gathering;
        private IInventoryService _inventory;
        private IIslanderRegistry _registry;
        private IFarmingService _farming;
        private IEconomyService _economy;
        private IIslandService _island;
        private ISocialService _social;
        private IGiftService _gifts;
        private World.InteriorView _interior;
        private World.WorldView _world;

        private int _tileX, _tileY;

        private float _sinceRefresh;

        public TargetKind Kind { get; private set; }

        /// <summary>Identificador de lo que tiene delante. Vacío si no hay nada.</summary>
        public string TargetId { get; private set; } = "";

        /// <summary>Lo que se le enseña al jugador: «Hablar con Bea», «Talar».</summary>
        public string Prompt { get; private set; } = "";

        private void Awake() => _body = GetComponent<PlayerBody>();

        private void Start()
        {
            ServiceRegistry.TryGet(out _player);
            ServiceRegistry.TryGet(out _gathering);
            ServiceRegistry.TryGet(out _inventory);
            ServiceRegistry.TryGet(out _registry);
            ServiceRegistry.TryGet(out _farming);
            ServiceRegistry.TryGet(out _economy);
            ServiceRegistry.TryGet(out _island);
            ServiceRegistry.TryGet(out _social);
            ServiceRegistry.TryGet(out _gifts);
            _interior = FindFirstObjectByType<World.InteriorView>();

            // Se busca una vez. Estaba dentro del bucle que recorre a los vecinos, así
            // que con la isla llena eran cincuenta recorridos de jerarquía seis veces
            // por segundo mientras andas, para acabar siempre en el mismo objeto.
            _world = GetComponentInParent<World.WorldView>();
        }

        private void Update()
        {
            _sinceRefresh += Time.deltaTime;
            if (_sinceRefresh >= _refreshInterval)
            {
                _sinceRefresh = 0f;
                FindTarget();
            }

            // Sistema de entrada antiguo: nada de Keyboard.current, que aquí revienta.
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) Act();

            if (Kind == TargetKind.Islander && Input.GetKeyDown(KeyCode.F))
                EventBus.Publish(new IslanderFocused(TargetId));
        }

        private void FindTarget()
        {
            Kind = TargetKind.None;
            TargetId = "";
            Prompt = "";

            // Dentro de una casa no hay nada más que la puerta: ni vecinos, ni
            // huerto, ni árboles. Comprobarlo primero ahorra recorrer ciento veinte
            // nodos que están quinientos metros más arriba.
            if (_interior != null && _interior.Inside)
            {
                if (Near(transform.position,
                         World.InteriorView.Anchor + new Vector3(transform.position.x, 1f, 0.4f), 3f)
                    || transform.position.z <= World.InteriorView.Anchor.z + 2.2f)
                {
                    Kind = TargetKind.Exit;
                    Prompt = "Salir";
                }
                return;
            }

            var origin = transform.position;
            float best = _reach * _reach;

            // Los vecinos primero: si tienes a alguien al lado y un arbusto detrás,
            // lo que quieres es hablar con la persona.
            if (_registry != null)
            {
                foreach (var islander in _registry.All)
                {
                    if (!TryGetIslanderPosition(islander.Id, out var position)) continue;
                    if (!InRange(origin, position, ref best)) continue;

                    Kind = TargetKind.Islander;
                    TargetId = islander.Id;
                    Prompt = MeetPrompt(islander.Identity.ShortName);
                }
            }

            if (Kind == TargetKind.Islander) return;

            if (TryTargetDoor()) return;

            // Los muebles de casa antes que el huerto y los nodos: la parcela llega
            // hasta cerca del porche, y estando delante de tu propia mesa lo que
            // quieres es usarla, no labrar la casilla que tengas bajo los pies.
            if (TryTargetHome()) return;

            if (TryTargetFarmTile()) return;

            if (_gathering == null) return;

            var nodes = _gathering.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.IsDepleted) continue;

                var position = new Vector3(node.X, node.Y, node.Z);
                if (!InRange(origin, position, ref best)) continue;
                if (!_gathering.TryGetDefinition(node.NodeId, out var definition)) continue;

                Kind = TargetKind.Node;
                TargetId = node.InstanceId;
                Prompt = PromptFor(definition);
            }

            if (Kind == TargetKind.None) TryTargetFood();
        }

        /// <summary>
        /// Si no hay nada delante y llevas comida, la acción es comértela.
        /// </summary>
        /// <remarks>
        /// Va de último y solo cuando no hay nada más, así que no le quita el sitio a
        /// ningún otro verbo: delante de un árbol se tala aunque lleves una manzana.
        /// Así comer no necesita tecla propia y sale anunciado en el mismo cartel que
        /// todo lo demás.
        ///
        /// Es lo que hace que quitar el relleno de vigor de medianoche no sea un
        /// castigo: sin esto, quedarse sin vigor lejos de casa obligaba a volver
        /// andando despacio hasta la hamaca. Y le da a la cosecha un destino que no sea
        /// la caja de ventas.
        /// </remarks>
        private void TryTargetFood()
        {
            if (_inventory == null || _economy == null) return;

            var held = _inventory.InHand;
            if (held.Quantity <= 0 || string.IsNullOrEmpty(held.CatalogId)) return;

            var item = _economy.GetItem(held.CatalogId);
            if (item == null || item.Category != ItemCategory.Food) return;
            if (item.HungerRestore <= 0) return;

            // Lleno no se come: gastar una manzana para no recuperar nada es la clase
            // de error que uno comete una vez y no perdona.
            if (_player != null && _player.Vigor >= Data.Player.PlayerState.MaxVigor)
            {
                Kind = TargetKind.Food;
                TargetId = held.CatalogId;
                Prompt = $"{item.DisplayName} — no te hace falta todavía";
                return;
            }

            Kind = TargetKind.Food;
            TargetId = held.CatalogId;
            Prompt = $"Comerte {item.DisplayName}";
        }

        /// <summary>
        /// Se come lo que lleva en la mano y le devuelve vigor.
        /// </summary>
        /// <remarks>
        /// Lo que llena de hambre a un vecino es lo que da de vigor al protagonista:
        /// una sola cifra en el catálogo para las dos cosas. Con dos números separados
        /// habría que equilibrar la comida dos veces y acabarían diciendo cosas
        /// distintas del mismo plato.
        /// </remarks>
        private void EatHeld()
        {
            if (_player == null || _inventory == null || _economy == null) return;
            if (_player.Vigor >= Data.Player.PlayerState.MaxVigor) return;

            var item = _economy.GetItem(_inventory.InHand.CatalogId);
            if (item == null || item.HungerRestore <= 0) return;

            if (!_inventory.TryConsumeSelected(1)) return;

            _player.RestoreVigor(item.HungerRestore);
            _body.SetEmotion(Data.Islanders.Emotion.Happy);
            FindTarget();
        }

        /// <summary>
        /// Qué le vas a hacer al vecino que tienes delante: darle lo que llevas, o
        /// hablar con él si no llevas nada que se pueda dar.
        /// </summary>
        /// <remarks>
        /// Dice también la tecla de la ficha. Antes la única tecla de acción abría la
        /// ficha, así que acercarse a un vecino y pulsar era exactamente lo mismo que
        /// clicar su nombre en la interfaz: convivir era el pilar número uno del diseño
        /// y en el mundo era un atajo a una pantalla. Ahora pulsar es hablarle, y la
        /// ficha —que sigue siendo un modo en el que se entra a propósito— tiene la
        /// suya. El cartel la nombra, como todos los demás de este juego.
        /// </remarks>
        private string MeetPrompt(string shortName)
        {
            string held = _inventory?.InHand.CatalogId;
            if (_gifts != null && _gifts.IsGiftable(held))
            {
                var item = _economy?.GetItem(held);
                string what = item != null ? item.DisplayName : "eso";
                return $"Darle {what} a {shortName} — F para su ficha";
            }

            return $"Hablar con {shortName} — F para su ficha";
        }

        /// <summary>
        /// ¿Está delante de una puerta? La suya o la de un edificio de la aldea.
        /// </summary>
        /// <remarks>
        /// La puerta de la cabaña cae al sur de la fachada, que es por donde se llega
        /// desde el huerto. Las de la aldea, en el centro del edificio: los edificios
        /// se pueden mover ahora, así que clavar la puerta a un lado obligaría a
        /// girarla con el edificio y a que el jugador diera la vuelta para encontrarla.
        /// </remarks>
        private bool TryTargetDoor()
        {
            var position = transform.position;

            var cabinDoor = Data.Player.PlayerHome.Cabin + new Vector3(0f, 0f, -2.6f);
            if (Near(position, cabinDoor, 2.2f))
            {
                Kind = TargetKind.Door;
                TargetId = "";
                Prompt = "Entrar en tu casa";
                return true;
            }

            if (_island == null) return false;

            foreach (var zoneId in _island.ZoneIds)
            {
                if (!_island.IsUnlocked(zoneId)) continue;
                if (!_island.TryGetSpawnPoint(zoneId, out var centre)) continue;

                // Se mide contra el centro del edificio, y el edificio tiene paredes:
                // hay que llegar desde fuera. Con la casa a cinco metros de fondo, la
                // fachada ya está a dos y medio del centro, así que un radio corto
                // pedía estar dentro de la pared para que apareciera el cartel.
                if (!Near(position, centre, 4.6f)) continue;

                // Solo se entra en lo que tiene dentro: una plaza o un parque no.
                var purpose = _island.PurposeOf(zoneId);
                if (purpose is ZonePurpose.Social or ZonePurpose.Nature) continue;

                Kind = TargetKind.Door;
                TargetId = zoneId;
                Prompt = "Entrar";
                return true;
            }

            return false;
        }

        /// <summary>¿Está delante de alguno de los muebles de su casa?</summary>
        private bool TryTargetHome()
        {
            var position = transform.position;
            float range = Data.Player.PlayerHome.UseRange;

            if (Near(position, Data.Player.PlayerHome.Hammock, range))
            {
                Kind = TargetKind.Hammock;
                Prompt = "Dormir hasta mañana";
                return true;
            }

            if (Near(position, Data.Player.PlayerHome.Bench, range))
            {
                Kind = TargetKind.Bench;
                Prompt = "Ponerse a hacer cosas";
                return true;
            }

            if (Near(position, Data.Player.PlayerHome.ShippingBox, range))
            {
                Kind = TargetKind.ShippingBox;
                Prompt = "Vender lo que traigas";
                return true;
            }

            return false;
        }

        private static bool Near(Vector3 from, Vector3 to, float range)
        {
            float dx = from.x - to.x;
            float dz = from.z - to.z;
            return dx * dx + dz * dz <= range * range;
        }

        /// <summary>
        /// Dormir: adelanta el reloj a las ocho de la mañana siguiente.
        /// </summary>
        /// <remarks>
        /// Va por el reloj y no por un salto directo del día para que todo lo que
        /// escucha las horas —el huerto, los nodos, las agendas de los vecinos— se
        /// entere de cada hora que pasa. Si se saltara al día siguiente de golpe, te
        /// levantarías con el huerto sin crecer y la isla congelada donde la dejaste.
        /// </remarks>
        private void Sleep()
        {
            if (!ServiceRegistry.TryGet<Core.Time.GameClock>(out var clock)) return;

            int minutesToMorning = (24 - clock.Hour + 8) % 24 * 60 - clock.Minute;
            if (minutesToMorning <= 0) minutesToMorning += 24 * 60;

            clock.Advance(minutesToMorning);
            _player?.RestoreVigor(Data.Player.PlayerState.MaxVigor);
            _body.SetEmotion(Data.Islanders.Emotion.Happy);

            EventBus.Publish(new Slept(clock.Day));
        }

        /// <summary>
        /// ¿Estás de pie en el huerto? Entonces el objetivo es la casilla que pisas.
        /// </summary>
        /// <remarks>
        /// Se mira la casilla en la que estás, no la que tienes delante. En una rejilla
        /// de metro y medio, apuntar a la de al lado es una pelea constante: uno se
        /// pone encima de lo que quiere trabajar y ya está.
        /// </remarks>
        private bool TryTargetFarmTile()
        {
            if (_farming == null) return false;

            var position = transform.position;
            if (!Data.Farming.FarmPlot.TileAt(position.x, position.z,
                                              _farming.Width, _farming.Height,
                                              out _tileX, out _tileY))
                return false;

            var tile = _farming.TileAt(_tileX, _tileY);
            if (tile == null) return false;

            var tool = _inventory?.ToolInHand ?? ToolKind.None;

            Kind = TargetKind.FarmTile;
            TargetId = $"{_tileX},{_tileY}";
            Prompt = FarmPrompt(tile, tool);
            return true;
        }

        private string FarmPrompt(Data.Farming.FarmTile tile, ToolKind tool)
        {
            switch (tile.State)
            {
                case Data.Farming.TileState.Wild:
                    if (tool == ToolKind.Hoe) return "Labrar";
                    return ToolSlot(ToolKind.Hoe, out int hoeSlot)
                        ? $"Sin labrar — pulsa {(hoeSlot + 1) % 10} para la azada"
                        : "Sin labrar — hace falta una azada";

                case Data.Farming.TileState.Tilled:
                    if (SelectedSeed(out string seedName)) return $"Sembrar {seedName}";

                    // Aquí es donde el jugador se quedaba atascado: labraba las
                    // cuarenta y ocho casillas con la azada, el cartel decía «ya está
                    // labrada» y ahí acababa la conversación. Decir qué falta no basta
                    // si no dices dónde está: se busca el hueco que tiene semillas y
                    // se nombra la tecla.
                    return SeedSlot(out int slot)
                        ? $"Labrada — pulsa {(slot + 1) % 10} para las semillas"
                        : "Labrada — no llevas semillas encima";

                case Data.Farming.TileState.Planted:
                    if (tile.Watered) return "Regada, creciendo";
                    if (tool == ToolKind.WateringCan) return "Regar";
                    return ToolSlot(ToolKind.WateringCan, out int canSlot)
                        ? $"Le falta agua — pulsa {(canSlot + 1) % 10} para la regadera"
                        : "Le falta agua — hace falta una regadera";

                default:
                    return "Recoger";
            }
        }

        /// <summary>El primer hueco de la barra que tenga semillas.</summary>
        private bool SeedSlot(out int slot)
        {
            slot = -1;
            if (_inventory == null || _farming == null) return false;

            for (int i = 0; i < _inventory.HotbarSize; i++)
            {
                var stack = _inventory.At(i);
                if (stack.Quantity <= 0) continue;
                if (!_farming.TryGetCrop(stack.CatalogId, out _)) continue;

                slot = i;
                return true;
            }
            return false;
        }

        /// <summary>Las semillas que lleva en la mano, si es que lleva.</summary>
        private bool SelectedSeed(out string displayName)
        {
            displayName = "";
            if (_inventory == null || _farming == null) return false;

            string id = _inventory.InHand.CatalogId;
            if (string.IsNullOrEmpty(id)) return false;
            if (!_farming.TryGetCrop(id, out var crop)) return false;

            displayName = crop.DisplayName;
            return true;
        }

        /// <summary>El primer hueco de la barra con esa herramienta.</summary>
        private bool ToolSlot(ToolKind wanted, out int slot)
        {
            slot = -1;
            if (_inventory == null) return false;

            for (int i = 0; i < _inventory.HotbarSize; i++)
            {
                var stack = _inventory.At(i);
                if (stack.Quantity <= 0) continue;

                var item = _economy?.GetItem(stack.CatalogId);
                if (item == null || item.Tool != wanted) continue;

                slot = i;
                return true;
            }
            return false;
        }

        private void WorkFarmTile()
        {
            if (_farming == null || _player == null) return;

            var tile = _farming.TileAt(_tileX, _tileY);
            if (tile == null) return;

            var tool = _inventory?.ToolInHand ?? ToolKind.None;

            switch (tile.State)
            {
                case Data.Farming.TileState.Wild:
                    if (tool != ToolKind.Hoe || !_player.CanUseTool) return;
                    if (_farming.Till(_tileX, _tileY) == FarmError.Ok) _player.SpendVigor(1.5f);
                    break;

                case Data.Farming.TileState.Tilled:
                    if (!SelectedSeed(out _)) return;
                    _farming.Plant(_tileX, _tileY, _inventory.InHand.CatalogId);
                    break;

                case Data.Farming.TileState.Planted:
                    if (tool != ToolKind.WateringCan || !_player.CanUseTool) return;
                    if (_farming.Water(_tileX, _tileY) == FarmError.Ok) _player.SpendVigor(1f);
                    break;

                default:
                    int got = _farming.Harvest(_tileX, _tileY, out var error);
                    if (got > 0) _body.SetEmotion(Data.Islanders.Emotion.Happy);
                    else if (error == FarmError.InventoryFull) _body.SetEmotion(Data.Islanders.Emotion.Worried);
                    break;
            }

            // La casilla acaba de cambiar y el aviso que pinta el mundo ya salió del
            // servicio, pero el texto de la barra lo calculamos aquí: sin refrescarlo,
            // sigue diciendo «Labrar» sobre una casilla ya labrada hasta que te muevas.
            FindTarget();
        }

        private string PromptFor(in NodeDefinition definition)
        {
            var required = definition.RequiredTool;
            if (required == ToolKind.None) return $"Coger {definition.DisplayName}";

            var inHand = _inventory?.ToolInHand ?? ToolKind.None;
            if (inHand == required) return $"{VerbFor(required)} {definition.DisplayName}";

            // Decir qué falta, no solo que no se puede. «Hace falta un hacha» es
            // información; «no puedes» es una puerta cerrada sin cartel.
            return $"{definition.DisplayName} — hace falta {NameOf(required)}";
        }

        private static string VerbFor(ToolKind tool) => tool switch
        {
            ToolKind.Axe => "Talar",
            ToolKind.Pickaxe => "Picar",
            ToolKind.Scythe => "Segar",
            ToolKind.Hoe => "Labrar",
            ToolKind.WateringCan => "Regar",
            _ => "Coger",
        };

        private static string NameOf(ToolKind tool) => tool switch
        {
            ToolKind.Axe => "un hacha",
            ToolKind.Pickaxe => "un pico",
            ToolKind.Scythe => "una guadaña",
            ToolKind.Hoe => "una azada",
            ToolKind.WateringCan => "una regadera",
            _ => "algo",
        };

        /// <summary>
        /// ¿Está a mano y por delante? Actualiza <paramref name="best"/> para quedarse
        /// con lo más cercano de todo lo que valga.
        /// </summary>
        private bool InRange(Vector3 origin, Vector3 target, ref float best)
        {
            var flat = target - origin;
            flat.y = 0f;

            float distance = flat.sqrMagnitude;
            if (distance > best) return false;
            if (distance < 0.0001f) return false;

            float angle = Vector3.Angle(_body.Facing, flat.normalized);
            if (angle > _cone * 0.5f) return false;

            best = distance;
            return true;
        }

        private bool TryGetIslanderPosition(string islanderId, out Vector3 position)
        {
            position = default;
            if (_world == null) return false;
            if (!_world.TryGetIslander(islanderId, out var body)) return false;

            position = body.position;
            return true;
        }

        /// <summary>Hace lo que diga el objetivo. Es la única tecla de acción del juego.</summary>
        private void Act()
        {
            switch (Kind)
            {
                case TargetKind.Islander:
                    MeetIslander();
                    break;

                case TargetKind.Node:
                    GatherTarget();
                    break;

                case TargetKind.FarmTile:
                    WorkFarmTile();
                    break;

                case TargetKind.Hammock:
                    Sleep();
                    break;

                case TargetKind.Bench:
                    EventBus.Publish(new StationUsed(CraftStationKind.Bench));
                    break;

                case TargetKind.ShippingBox:
                    EventBus.Publish(new StationUsed(CraftStationKind.Shipping));
                    break;

                case TargetKind.Door:
                    EventBus.Publish(new InteriorEntered(TargetId, Prompt));
                    break;

                case TargetKind.Exit:
                    EventBus.Publish(new InteriorExited());
                    break;

                case TargetKind.Food:
                    EatHeld();
                    break;
            }
        }

        /// <summary>
        /// Hablarle, o darle lo que llevas en la mano si es algo que se pueda dar.
        /// </summary>
        /// <remarks>
        /// La cara que pone el vecino la deciden los servicios, que son los que saben
        /// de personalidades. Aquí solo se pone la del protagonista, que es lo único
        /// que este componente tiene delante.
        /// </remarks>
        private void MeetIslander()
        {
            string held = _inventory?.InHand.CatalogId;

            if (_gifts != null && _gifts.IsGiftable(held))
            {
                var result = _gifts.OfferHeld(TargetId, out int opinion);

                _body.SetEmotion(result switch
                {
                    GiftResult.Ok when opinion > 0 => Data.Islanders.Emotion.Ecstatic,
                    GiftResult.Ok when opinion < 0 => Data.Islanders.Emotion.Worried,
                    GiftResult.Ok => Data.Islanders.Emotion.Happy,
                    _ => Data.Islanders.Emotion.Neutral,
                });

                // Con el tope diario gastado no se ha dado nada y la mochila sigue
                // igual: se cae a la charla para que pulsar haga siempre algo.
                if (result != GiftResult.AlreadyToday) { FindTarget(); return; }
            }

            _social?.PlayerInteract(TargetId, SocialInteraction.Chat);
            _body.SetEmotion(Data.Islanders.Emotion.Happy);
            FindTarget();
        }

        private void GatherTarget()
        {
            if (_gathering == null || _player == null) return;

            var tool = _inventory?.ToolInHand ?? ToolKind.None;

            // Sin vigor no se usan herramientas, pero coger flores a mano sí: quedarse
            // sin poder hacer absolutamente nada es justo lo que este juego no hace.
            if (tool != ToolKind.None && !_player.CanUseTool) return;

            var result = _gathering.Gather(TargetId, tool, out int dropped);
            if (result == GatherResult.Hit || result == GatherResult.Ok)
                _player.SpendVigor(tool == ToolKind.None ? 0.5f : 2f);

            if (result == GatherResult.Ok && dropped > 0)
                _body.SetEmotion(Data.Islanders.Emotion.Happy);
        }
    }
}
