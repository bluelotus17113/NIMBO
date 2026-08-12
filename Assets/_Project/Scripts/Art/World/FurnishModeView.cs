using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Amueblar la casa desde dentro: la habitación desde arriba, la rejilla marcada
    /// y un mueble pegado al ratón.
    /// </summary>
    /// <remarks>
    /// Es el modo construcción de la aldea, pero de puertas adentro. Se parecen a
    /// propósito: se coloca igual, se gira igual y se sale igual, así que quien ha
    /// movido una tienda ya sabe poner una silla.
    ///
    /// Lo que cambia es de dónde salen las cosas. Fuera se colocan edificios que ya
    /// tienes abiertos y puedes moverlos las veces que quieras; aquí se coloca lo que
    /// llevas en el inventario, así que **poner gasta y recoger devuelve**. Sin eso,
    /// una silla comprada una vez amueblaba la casa entera.
    /// </remarks>
    public sealed class FurnishModeView : MonoBehaviour
    {
        private InteriorView _interior;
        private IHousingService _housing;
        private IEconomyService _economy;

        private Camera _camera;
        private Transform _gridRoot;
        private GameObject _ghost;
        private string _ghostShape = "";
        private int _cellX, _cellY;

        public bool Active { get; private set; }

        /// <summary>Lo que se está colocando. Vacío para no colocar nada.</summary>
        public string Holding { get; private set; } = "";

        /// <summary>Hacia dónde mira lo que se está colocando.</summary>
        public Facing Orientation { get; private set; } = Facing.South;

        private void OnEnable()
        {
            EventBus.Subscribe<FurnishModeChanged>(OnModeChanged);
            EventBus.Subscribe<FurnishSelectionChanged>(OnSelectionChanged);
            EventBus.Subscribe<InteriorExited>(OnInteriorExited);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FurnishModeChanged>(OnModeChanged);
            EventBus.Unsubscribe<FurnishSelectionChanged>(OnSelectionChanged);
            EventBus.Unsubscribe<InteriorExited>(OnInteriorExited);
        }

        private void OnModeChanged(FurnishModeChanged evt)
        {
            if (evt.Furnishing) Enter(); else Leave();
        }

        private void OnSelectionChanged(FurnishSelectionChanged evt) => Holding = evt.CatalogId ?? "";

        /// <summary>
        /// Salir por la puerta amueblando cierra el modo.
        /// </summary>
        /// <remarks>
        /// La habitación se destruye al salir, así que quedarse en modo amueblar sería
        /// tener el ratón colocando sillas en una casa que ya no existe.
        /// </remarks>
        private void OnInteriorExited(InteriorExited _)
        {
            if (!Active) return;
            Leave();
            EventBus.Publish(new FurnishModeChanged(false));
        }

        private void Enter()
        {
            if (Active) return;

            _interior ??= FindFirstObjectByType<InteriorView>();

            // Solo se amuebla estando dentro. Desde la calle no hay habitación que
            // enseñar y la cámara se iría a mirar el vacío de debajo del mundo.
            if (_interior == null || !_interior.Inside || _interior.CurrentRoom == null) return;
            if (!ServiceRegistry.TryGet(out _housing)) return;
            ServiceRegistry.TryGet(out _economy);

            Active = true;
            Holding = "";
            Orientation = Facing.South;

            // La cámara no se toca.
            //
            // El modo construcción de la aldea sí la cambia, porque doscientos metros
            // de isla no caben en el plano de andar. Un cuarto sí cabe: la cámara de
            // estar dentro ya lo enseña entero. Se probó la cenital pura y era peor —
            // mirando un mueble justo desde arriba solo se le ve la tapa, y una silla
            // y una maceta desde ahí son dos cuadrados del mismo tamaño.
            _camera = Camera.main;

            // Sin mando mientras se amuebla: el ratón está colocando muebles y las
            // teclas de andar meterían al muñeco justo debajo de lo que estás poniendo.
            Player(true);

            BuildGridOverlay();
        }

        private void Leave()
        {
            if (!Active) return;

            Active = false;
            Holding = "";

            // Las mallas hay que tirarlas a mano: destruir el objeto que las usa no
            // borra la malla, y así cada vuelta al modo dejaría veinte tiradas.
            if (_gridRoot != null)
            {
                foreach (var filter in _gridRoot.GetComponentsInChildren<MeshFilter>())
                    if (filter.sharedMesh != null) Destroy(filter.sharedMesh);

                Destroy(_gridRoot.gameObject);
            }

            if (_ghost != null)
            {
                var filter = _ghost.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);

                Destroy(_ghost);
            }
            _gridRoot = null;
            _ghost = null;
            _ghostShape = "";

            Player(false);
        }

        private void Player(bool frozen)
        {
            var body = GameObject.Find("Protagonista");
            if (body == null) return;
            body.GetComponent<PlayerView.PlayerBody>()?.Freeze(frozen);
        }

        /// <summary>
        /// Las casillas del cuarto, marcadas sobre el suelo.
        /// </summary>
        /// <remarks>
        /// Son rayas, no losas. Pintar una losa por casilla repinta el suelo entero:
        /// se probó y el cuarto pasaba a ser un tablero de damas donde ya no se
        /// distinguía ni la madera ni la sombra de lo que había puesto.
        /// </remarks>
        private void BuildGridOverlay()
        {
            var room = _interior.CurrentRoom;

            _gridRoot = new GameObject("Rejilla de la casa").transform;
            _gridRoot.SetParent(transform, worldPositionStays: false);
            _gridRoot.position = _interior.RoomOrigin;

            var material = ToonPalette.Solid(new Color32(0x8A, 0x73, 0x50, 255));
            float width = room.Width * InteriorView.Tile;
            float depth = room.Height * InteriorView.Tile;

            for (int x = 0; x <= room.Width; x++)
                Line($"v_{x}", new Vector3(x * InteriorView.Tile, 0.05f, depth * 0.5f),
                     new Vector3(LineWidth, 0.02f, depth));

            for (int y = 0; y <= room.Height; y++)
                Line($"h_{y}", new Vector3(width * 0.5f, 0.05f, y * InteriorView.Tile),
                     new Vector3(width, 0.02f, LineWidth));

            void Line(string name, Vector3 at, Vector3 size)
            {
                var bar = new GameObject(name);
                bar.transform.SetParent(_gridRoot, worldPositionStays: false);
                bar.transform.localPosition = at;

                bar.AddComponent<MeshFilter>().sharedMesh = MeshShapes.Box(size);
                bar.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private const float LineWidth = 0.06f;

        private void Update()
        {
            if (!Active || _camera == null) return;

            // R gira lo que llevas, como en cualquier juego de colocar cosas.
            if (Input.GetKeyDown(KeyCode.R)) Rotate();

            if (!TryCellUnderMouse(out _cellX, out _cellY)) { HideGhost(); return; }

            // El botón derecho recoge lo que haya bajo el ratón. Aquí no hace falta
            // para mover la vista —la habitación cabe entera en pantalla—, y recoger
            // sin tener que soltar antes lo que llevas es lo que uno espera.
            if (Input.GetMouseButtonDown(1)) { TryPickUpAt(_cellX, _cellY); return; }

            if (string.IsNullOrEmpty(Holding)) { HideGhost(); return; }

            ShowGhost(_cellX, _cellY, CanPlaceAt(_cellX, _cellY) == PlacementError.None);

            if (Input.GetMouseButtonDown(0)) TryPlaceAt(_cellX, _cellY);
        }

        public void Rotate() => Orientation = (Facing)(((int)Orientation + 1) % 4);

        /// <summary>Si cabe ahí lo que se lleva, y por qué no si no cabe.</summary>
        public PlacementError CanPlaceAt(int cellX, int cellY)
        {
            if (!Active || string.IsNullOrEmpty(Holding)) return PlacementError.UnknownCatalogId;

            var room = _interior?.CurrentRoom;
            if (room == null) return PlacementError.OutOfBounds;

            if (_economy != null && _economy.Inventory.CountOf(Holding) <= 0)
                return PlacementError.NotOwned;

            return _housing.CanPlace(room, Holding, new GridCoord(cellX, cellY), Orientation);
        }

        /// <summary>
        /// Coloca lo que se lleva en esa casilla. Es lo mismo que hace el clic.
        /// </summary>
        /// <remarks>
        /// Público a propósito: el ratón no se puede fingir desde una prueba, así que
        /// si el clic hiciera el trabajo por su cuenta lo único que se podría probar
        /// sería el servicio, que ya funcionaba. Lo que se rompe es la costura.
        /// </remarks>
        public PlacementError TryPlaceAt(int cellX, int cellY)
        {
            var error = CanPlaceAt(cellX, cellY);
            if (error != PlacementError.None) return error;

            var room = _interior.CurrentRoom;
            error = _housing.Place(room, Holding, new GridCoord(cellX, cellY), Orientation);
            if (error != PlacementError.None) return error;

            _economy?.Inventory.Remove(Holding, 1);
            _interior.RefreshFurniture();

            // Si era el último, se suelta. Seguir enseñando el fantasma de algo que ya
            // no tienes es prometer una silla que el siguiente clic va a rechazar.
            if (_economy != null && _economy.Inventory.CountOf(Holding) <= 0)
            {
                Holding = "";
                EventBus.Publish(new FurnishSelectionChanged(""));
            }

            return PlacementError.None;
        }

        /// <summary>Recoge lo que haya en esa casilla y lo devuelve al inventario.</summary>
        public bool TryPickUpAt(int cellX, int cellY)
        {
            if (!Active) return false;

            var room = _interior?.CurrentRoom;
            if (room == null) return false;
            if (!TryFindAt(room, cellX, cellY, out var placed)) return false;
            if (!_housing.Remove(room, placed.InstanceId)) return false;

            _economy?.Inventory.Add(placed.CatalogId, 1);
            _interior.RefreshFurniture();
            return true;
        }

        /// <summary>
        /// Qué mueble ocupa esa casilla.
        /// </summary>
        /// <remarks>
        /// Con varias capas puede haber dos: la vela encima de la mesa está en la misma
        /// casilla que la mesa. Se coge el de más arriba, que es el que se ve y el que
        /// el jugador cree estar señalando.
        /// </remarks>
        private static bool TryFindAt(RoomLayout room, int cellX, int cellY, out PlacedObject found)
        {
            found = default;
            bool any = false;

            foreach (var placed in room.Objects)
            {
                InteriorView.Footprint(placed.CatalogId, placed.Facing, out int cellsX, out int cellsY);
                if (cellX < placed.Origin.X || cellX >= placed.Origin.X + cellsX) continue;
                if (cellY < placed.Origin.Y || cellY >= placed.Origin.Y + cellsY) continue;

                if (any && placed.Layer < found.Layer) continue;

                found = placed;
                any = true;
            }

            return any;
        }

        /// <summary>Sobre qué casilla de la habitación está el ratón.</summary>
        private bool TryCellUnderMouse(out int cellX, out int cellY)
        {
            cellX = cellY = 0;

            // Se cruza con el plano del suelo del cuarto, no con un rayo físico: los
            // muebles tienen malla y un rayo físico dejaría de poder señalar la
            // casilla ocupada, que es justo la que hay que señalar para recogerla.
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;

            float floor = _interior.RoomOrigin.y;
            float t = (floor - ray.origin.y) / ray.direction.y;
            if (t <= 0f) return false;

            InteriorView.CellAt(ray.origin + ray.direction * t - _interior.RoomOrigin,
                                out cellX, out cellY);
            return true;
        }

        private void ShowGhost(int cellX, int cellY, bool valid)
        {
            InteriorView.Footprint(Holding, Orientation, out int cellsX, out int cellsY);
            float height = InteriorView.HeightOf(Holding);

            if (_ghost == null)
            {
                _ghost = new GameObject("fantasma");
                _ghost.transform.SetParent(transform, worldPositionStays: false);
                _ghost.AddComponent<MeshFilter>();
                _ghost.AddComponent<MeshRenderer>();
            }

            _ghost.SetActive(true);

            // La malla solo se rehace cuando cambia lo que llevas o cómo lo llevas.
            // Rehacerla en cada movimiento del ratón es una malla nueva sesenta veces
            // por segundo, y la vieja hay que ir destruyéndola a mano.
            string shape = $"{Holding}:{Orientation}";
            if (shape != _ghostShape)
            {
                var filter = _ghost.GetComponent<MeshFilter>();
                if (filter.sharedMesh != null) Destroy(filter.sharedMesh);

                filter.sharedMesh = MeshShapes.Box(new Vector3(cellsX * InteriorView.Tile * 0.9f,
                                                               height,
                                                               cellsY * InteriorView.Tile * 0.9f));
                _ghostShape = shape;
            }

            _ghost.transform.position = _interior.RoomOrigin
                                      + InteriorView.CentreOf(cellX, cellY, cellsX, cellsY)
                                      + Vector3.up * (height * 0.5f);

            _ghost.GetComponent<MeshRenderer>().sharedMaterial = ToonPalette.Solid(
                valid ? new Color32(0xB8, 0xE6, 0xC8, 255) : new Color32(0xF5, 0xC0, 0xCB, 255));
        }

        private void HideGhost()
        {
            if (_ghost != null) _ghost.SetActive(false);
        }

        private void OnDestroy() => Leave();
    }
}
