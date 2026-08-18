using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.PlayerView
{
    /// <summary>Con qué está a punto de interactuar el jugador.</summary>
    public enum TargetKind { None = 0, Islander = 1, Node = 2, FarmTile = 3,
                             Hammock = 4, Bench = 5, ShippingBox = 6,
                             Door = 7, Exit = 8, Food = 9, Board = 10,
                             FishingSpot = 11, Stage = 12, Stove = 13 }

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

        /// <summary>Desde qué parte del radio empieza a contar como borde de la isla.</summary>
        public const float RimFraction = 0.78f;

        private PlayerBody _body;
        private Nimbo.Player.PlayerService _player;
        private IGatheringService _gathering;
        private IInventoryService _inventory;
        private IIslanderRegistry _registry;
        private IFarmingService _farming;
        private IEconomyService _economy;
        private IIslandService _island;
        private IBuildService _build;
        private ISocialService _social;
        private IGiftService _gifts;
        private IRequestService _requests;
        private IMinigameService _minigames;
        private IPlayerProgression _progression;
        private IConductService _conduct;
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
            ServiceRegistry.TryGet(out _build);
            ServiceRegistry.TryGet(out _social);
            ServiceRegistry.TryGet(out _gifts);
            ServiceRegistry.TryGet(out _requests);
            ServiceRegistry.TryGet(out _minigames);
            ServiceRegistry.TryGet(out _progression);
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
            bool accompanied = false;
            if (_registry != null)
            {
                foreach (var islander in _registry.All)
                {
                    if (!TryGetIslanderPosition(islander.Id, out var position)) continue;

                    // De paso, el eje de Actitud (§14.3.1): estar cerca de alguien en
                    // vez de andar solo. Se aprovecha este recorrido, que ya mira a
                    // todos los vecinos con sus posiciones en su cadencia lenta, así
                    // que medirlo no añade ni una iteración.
                    if (!accompanied && Near(origin, position, CompanyRange)) accompanied = true;

                    if (!InRange(origin, position, ref best)) continue;

                    Kind = TargetKind.Islander;
                    TargetId = islander.Id;
                    Prompt = MeetPrompt(islander.Identity.ShortName);
                }
            }

            NoteCompany(accompanied);

            if (Kind == TargetKind.Islander) return;

            if (TryTargetDoor()) return;

            // Los muebles de casa antes que el huerto y los nodos: la parcela llega
            // hasta cerca del porche, y estando delante de tu propia mesa lo que
            // quieres es usarla, no labrar la casilla que tengas bajo los pies.
            if (TryTargetHome()) return;

            if (TryTargetBoard()) return;

            // El concierto por delante de la caña: si hay fiesta en la plaza y estás
            // ahí, lo que quieres es subirte, no ponerte a pescar de espaldas.
            if (TryTargetStage()) return;
            if (TryTargetFishingSpot()) return;

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

            if (_island == null || _build == null) return false;

            foreach (var zoneId in _island.ZoneIds)
            {
                if (!_island.IsUnlocked(zoneId)) continue;

                // El centro se le pide a quien coloca, no a la isla. `TryGetSpawnPoint`
                // devuelve un punto **al azar** dentro del circulo de la zona —sirve
                // para plantar ahí a un vecino que va a esa zona— y las zonas miden de
                // diez a veinte metros de radio. Midiendo contra eso, el cartel de
                // «Entrar» aparecía y desaparecía por media zona seis veces por segundo
                // y no había forma de saber dónde estaba la puerta.
                if (!_build.TryGetWorldCentre(zoneId, out var centre)) continue;

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

            if (Near(position, Data.Player.PlayerHome.Stove, range))
            {
                Kind = TargetKind.Stove;
                Prompt = "Ponerte a cocinar";
                return true;
            }

            return false;
        }

        /// <summary>
        /// ¿Está en el borde de la isla con la caña en la mano?
        /// </summary>
        /// <remarks>
        /// En el borde y no en el embarcadero, aunque el embarcadero exista: esa zona
        /// pide doce vecinos y una bandera de suceso para abrirse, así que pescar habría
        /// nacido bloqueado hasta el final de la partida. El borde está desde el primer
        /// día y lo tienen las dos islas, incluida la tuya.
        ///
        /// Pide llevar la caña **en la mano** y no solo tenerla, igual que talar pide el
        /// hacha. Con solo tenerla, el cartel de pescar saldría encima de cualquier otra
        /// cosa cada vez que pasas cerca del borde.
        /// </remarks>
        private bool TryTargetFishingSpot()
        {
            if (_inventory == null || _inventory.ToolInHand != ToolKind.FishingRod) return false;

            var position = transform.position;
            if (Data.World.Archipelago.OnBridge(position)) return false;

            var side = Data.World.Archipelago.SideOf(position);
            var centre = Data.World.Archipelago.CentreOf(side);
            float radius = Data.World.Archipelago.RadiusOf(side);

            // En proporción y no en metros: la isla del jugador mide menos de la mitad
            // que la aldea, y un margen fijo la dejaría casi entera contando como borde.
            //
            // Y con margen de sobra —desde el 78%— porque el contorno de un prado es
            // irregular: `BuildSurface` lo mueve entre el 86% y el 100% del radio. Un
            // umbral pegado al borde deja el sitio de pescar en la parte que se hunde
            // hacia el vacío, y ahí no se puede estar de pie.
            float dx = position.x - centre.x;
            float dz = position.z - centre.z;
            if (dx * dx + dz * dz < radius * radius * RimFraction * RimFraction) return false;

            Kind = TargetKind.FishingSpot;
            TargetId = "";
            Prompt = "Echar la caña";
            return true;
        }

        /// <summary>
        /// ¿Hay concierto y está donde se toca?
        /// </summary>
        /// <remarks>
        /// En el escenario cuando la aldea tiene escenario, y si no en el Árbol Nimbo,
        /// que es donde se junta todo el mundo desde el primer día. El concierto lo pone
        /// el calendario y ocurre haya escenario o no; atar el cartel a un edificio que
        /// tarda ocho vecinos en abrirse habría dejado el minijuego sin puerta durante
        /// media partida.
        /// </remarks>
        private bool TryTargetStage()
        {
            if (_minigames == null || !_minigames.ConcertRunning) return false;

            var position = transform.position;
            var spot = Vector3.zero;   // el Árbol Nimbo, en el centro de la plaza

            if (_island != null && _build != null &&
                _island.IsUnlocked(Data.World.Archipelago.StageZone) &&
                _build.TryGetWorldCentre(Data.World.Archipelago.StageZone, out var stage))
                spot = stage;

            if (!Near(position, spot, 7f)) return false;

            Kind = TargetKind.Stage;
            TargetId = "";
            Prompt = "Subir a tocar";
            return true;
        }

        /// <summary>
        /// ¿Está delante del tablón de encargos de la plaza?
        /// </summary>
        /// <remarks>
        /// El cartel dice cuántas cosas hay pendientes antes de abrirlo. Es la
        /// diferencia entre un tablón y una puerta: se lee de lejos si hay algo que
        /// leer, y si no lo hay uno sigue andando sin haber abierto una pantalla para
        /// nada.
        /// </remarks>
        private bool TryTargetBoard()
        {
            if (!Near(transform.position, Data.World.Archipelago.RequestBoard,
                      Data.World.Archipelago.RequestBoardRange))
                return false;

            Kind = TargetKind.Board;
            TargetId = "";

            int pending = _requests?.OpenCount ?? 0;
            Prompt = pending switch
            {
                0 => "Tablón de encargos — hoy no hay nada",
                1 => "Tablón de encargos — hay una cosa",
                _ => $"Tablón de encargos — hay {pending} cosas",
            };
            return true;
        }

        /// <summary>A cuánto cuenta como estar acompañado, en metros.</summary>
        private const float CompanyRange = 8f;

        /// <summary>
        /// Apunta si estabas con alguien o a tu aire.
        /// </summary>
        /// <remarks>
        /// Solo puertas afuera. Dentro de una casa no hay vecinos que valgan —el
        /// interior es una escena aparte y está vacía—, así que contar ahí diría que
        /// todo el que amuebla su cabaña es un ermitaño.
        ///
        /// El peso son los segundos que han pasado desde la última vez que se miró, que
        /// es el intervalo de refresco: así el eje mide **rato** y no cuántas veces se
        /// ha recalculado el objetivo.
        /// </remarks>
        private void NoteCompany(bool accompanied)
        {
            if (_conduct == null && !ServiceRegistry.TryGet(out _conduct)) return;
            if (_interior != null && _interior.Inside) return;

            // En minutos, como la Energía: los cuatro ejes tienen que tardar parecido
            // en creerse o la confianza de uno no significa lo mismo que la de otro.
            _conduct.Note(Data.Islanders.PersonalityAxis.Attitude, accompanied,
                          _refreshInterval / 60f);
        }

        /// <summary>El nivel de la isla, de 1 a 5. Sin isla registrada, el más fácil.</summary>
        private int IslandLevel() =>
            _island == null ? 1 : Mathf.Clamp(_island.State.Level, 1, 5);

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
                    Swing(WideHoe, (x, y) =>
                    {
                        if (_farming.Till(x, y) == FarmError.Ok) _player.SpendVigor(1.5f);
                    });
                    break;

                case Data.Farming.TileState.Tilled:
                    if (!SelectedSeed(out _)) return;
                    _farming.Plant(_tileX, _tileY, _inventory.InHand.CatalogId);
                    break;

                case Data.Farming.TileState.Planted:
                    if (tool != ToolKind.WateringCan || !_player.CanUseTool) return;
                    Swing(WideCan, (x, y) =>
                    {
                        if (_farming.Water(x, y) == FarmError.Ok) _player.SpendVigor(1f);
                    });
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

        /// <summary>¿La azada de la mano abre tres surcos? (§12.4)</summary>
        private bool WideHoe => (_inventory?.ToolTierInHand ?? 1) >= 2;

        /// <summary>
        /// ¿La regadera moja tres casillas?
        /// </summary>
        /// <remarks>
        /// Por dos caminos que llegan al mismo sitio: la regadera grande (§12.4) y
        /// Cultivo 3 (§12.3). Es a propósito — el jugador que se dedica al huerto lo
        /// consigue regando, y el que se dedica al taller lo consigue fabricando. Dos
        /// vías separadas tienen que poder llegar a lo mismo por su cuenta o dejan de
        /// ser cinco caminos y vuelven a ser una lista.
        /// </remarks>
        private bool WideCan =>
            (_inventory?.ToolTierInHand ?? 1) >= 2
            || (_progression?.IsUnlocked(Unlock.WideWatering) ?? false);

        /// <summary>
        /// Aplica el gesto a la casilla de delante y, si la herramienta es ancha, a las
        /// dos de al lado.
        /// </summary>
        /// <remarks>
        /// A los lados y no hacia delante: barrer hacia delante alcanza casillas que no
        /// se ven y de las que no salía ningún cartel, así que el jugador labra cosas
        /// sin haberlas mirado. De lado se ve lo que se está segando.
        ///
        /// Las de fuera del huerto no hacen nada: el servicio ya contesta
        /// <c>OutOfBounds</c> o <c>NotYourLandYet</c>, así que la barrida en el borde no
        /// necesita comprobarse aquí.
        /// </remarks>
        private void Swing(bool wide, System.Action<int, int> act)
        {
            act(_tileX, _tileY);
            if (!wide) return;

            // Se barre en cruz con la mirada: mirando al norte o al sur se abre a los
            // lados en X, y mirando al este o al oeste en Z.
            var forward = transform.forward;
            bool sweepX = Mathf.Abs(forward.z) >= Mathf.Abs(forward.x);

            for (int step = -1; step <= 1; step += 2)
                act(sweepX ? _tileX + step : _tileX, sweepX ? _tileY : _tileY + step);
        }

        private string PromptFor(in NodeDefinition definition)
        {
            var required = definition.RequiredTool;
            if (required == ToolKind.None)
                return $"Coger {definition.DisplayName}{Yield(definition)}";

            var inHand = _inventory?.ToolInHand ?? ToolKind.None;
            if (inHand == required)
                return $"{VerbFor(required)} {definition.DisplayName}{Yield(definition)}";

            // Decir qué falta, no solo que no se puede. «Hace falta un hacha» es
            // información; «no puedes» es una puerta cerrada sin cartel.
            return $"{definition.DisplayName} — hace falta {NameOf(required)}";
        }

        /// <summary>
        /// Qué suelta el nodo, cuando el jugador ya sabe leerlos (Recolección 2).
        /// </summary>
        /// <remarks>
        /// Antes de eso el cartel dice qué es pero no qué da, así que hay que talarlo
        /// para averiguarlo. Es un desbloqueo pequeño y de los que más se notan: deja de
        /// hacer falta acordarse de qué árbol daba savia.
        /// </remarks>
        private string Yield(in NodeDefinition definition)
        {
            if (_progression == null || !_progression.IsUnlocked(Unlock.ReadTheNode)) return "";
            if (string.IsNullOrEmpty(definition.DropId)) return "";

            var item = _economy?.GetItem(definition.DropId);
            return item == null ? "" : $" · da {item.DisplayName.ToLowerInvariant()}";
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

                case TargetKind.Board:
                    EventBus.Publish(new RequestBoardRead());
                    break;

                case TargetKind.Stove:
                    EventBus.Publish(new StationUsed(CraftStationKind.Kitchen));
                    break;

                // La dificultad sube con la isla: cuanto más crece la aldea, más lejos
                // pican los peces y más larga es la canción. Es el único medidor de
                // avance que existe hoy; cuando el protagonista tenga niveles (§12)
                // será el suyo.
                case TargetKind.FishingSpot:
                    EventBus.Publish(new MinigameRequested(MinigameKind.Fishing, IslandLevel()));
                    break;

                case TargetKind.Stage:
                    EventBus.Publish(new MinigameRequested(MinigameKind.Rhythm, IslandLevel()));
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

            // La guadaña larga siega en arco: se lleva por delante lo que tenga al lado
            // (§12.4). Solo con la guadaña, y solo si lo de delante ha salido bien —si
            // no llegas ni a la primera mata, no estás segando un arco.
            if (tool == ToolKind.Scythe && result == GatherResult.Ok &&
                (_inventory?.ToolTierInHand ?? 1) >= 2)
                SweepNearby();
        }

        /// <summary>
        /// Siega también lo que tenga a los lados, hasta dos matas más.
        /// </summary>
        /// <remarks>
        /// Se cobra vigor por cada una, que es lo justo: la guadaña larga ahorra
        /// pulsaciones, no trabajo. Y se para a las dos porque en un prado de hierba
        /// una barrida sin tope se llevaría media isla de un golpe.
        /// </remarks>
        private void SweepNearby()
        {
            const float ArcRange = 3.2f;
            const int MaxExtra = 2;

            var origin = transform.position;
            int cut = 0;

            var nodes = _gathering.Nodes;
            for (int i = 0; i < nodes.Count && cut < MaxExtra; i++)
            {
                var node = nodes[i];
                if (node.IsDepleted || node.InstanceId == TargetId) continue;
                if (!Near(origin, new Vector3(node.X, node.Y, node.Z), ArcRange)) continue;
                if (!_gathering.TryGetDefinition(node.NodeId, out var definition)) continue;
                if (definition.RequiredTool != ToolKind.Scythe) continue;
                if (!_player.CanUseTool) return;

                if (_gathering.Gather(node.InstanceId, ToolKind.Scythe, out _) != GatherResult.Ok)
                    continue;

                _player.SpendVigor(2f);
                cut++;
            }
        }
    }
}
