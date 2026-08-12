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
    /// El interior de una casa: se entra por la puerta y estás dentro, como en
    /// Animal Crossing.
    /// </summary>
    /// <remarks>
    /// La habitación se construye **apartada del mundo**, quinientos metros por
    /// debajo de las islas, y entrar es fundir a negro y teletransportar. No es una
    /// escena de Unity de verdad y es a propósito: el arranque tiene quince servicios
    /// montados y la isla se genera por código, así que cargar escena al cruzar una
    /// puerta significaría desmontarlo y rehacerlo todo. Una puerta que tarda dos
    /// segundos se carga el juego amable; así es instantánea y se siente igual.
    ///
    /// Solo hay una habitación viva a la vez. Se tira y se rehace al entrar en otra:
    /// generar la casa de doce vecinos por si acaso serían doce habitaciones que
    /// nadie está mirando.
    /// </remarks>
    public sealed class InteriorView : MonoBehaviour
    {
        /// <summary>Dónde se monta la habitación, lejos de todo lo demás.</summary>
        public static readonly Vector3 Anchor = new(0f, -500f, 0f);

        /// <summary>Tamaño de casilla de la rejilla de vivienda, en metros.</summary>
        public const float Tile = 1.5f;

        private Transform _room;
        private Transform _furniture;
        private IHousingService _housing;

        private static Mesh _slab;
        private static Mesh _wall;

        /// <summary>Cierto mientras el jugador está dentro de una casa.</summary>
        public bool Inside { get; private set; }

        /// <summary>
        /// La habitación en la que está metido, o null si está en la calle.
        /// </summary>
        /// <remarks>
        /// La expone para que el modo amueblar coloque en la misma que se está
        /// dibujando. Volver a pedirla por la clave sería resolverla dos veces y
        /// dejar la puerta abierta a amueblar una casa distinta de la que se ve.
        /// </remarks>
        public RoomLayout CurrentRoom { get; private set; }

        /// <summary>Dónde cae la esquina (0,0) de la habitación en el mundo.</summary>
        public Vector3 RoomOrigin => _room != null ? _room.position : Anchor;

        /// <summary>Dónde estaba fuera, para devolverlo al salir por la puerta.</summary>
        private Vector3 _outsideSpot;

        private void OnEnable()
        {
            EventBus.Subscribe<InteriorEntered>(OnEntered);
            EventBus.Subscribe<InteriorExited>(OnExited);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InteriorEntered>(OnEntered);
            EventBus.Unsubscribe<InteriorExited>(OnExited);
        }

        private void OnEntered(InteriorEntered evt)
        {
            if (!ServiceRegistry.TryGet(out _housing)) return;

            var layout = ResolveLayout(evt.HomeKey);
            if (layout == null) return;

            var body = GameObject.Find("Protagonista");
            if (body == null) return;

            // Se apunta dónde estaba ANTES de moverlo: al salir hay que devolverlo a
            // la puerta por la que entró, no a un sitio fijo.
            _outsideSpot = body.transform.position;

            Rebuild(layout);

            // Avisar ANTES de mover: si se avisa después, la red anticaída ya ha
            // corrido su Update con el jugador a quinientos metros bajo el mundo y lo
            // ha devuelto a la calle.
            var player = body.GetComponent<PlayerView.PlayerBody>();
            player?.SetIndoors(true);

            Teleport(body, Anchor + new Vector3(layout.Width * Tile * 0.5f, 1f, 1.4f));
            Inside = true;
        }

        private void OnExited(InteriorExited _)
        {
            if (!Inside) return;

            var body = GameObject.Find("Protagonista");
            if (body != null)
            {
                Teleport(body, _outsideSpot);
                body.GetComponent<PlayerView.PlayerBody>()?.SetIndoors(false);
            }

            if (_room != null) Destroy(_room.gameObject);
            _room = null;
            _furniture = null;
            CurrentRoom = null;
            Inside = false;
        }

        /// <summary>La habitación de esa clave: vacía es la tuya.</summary>
        private RoomLayout ResolveLayout(string homeKey)
        {
            if (string.IsNullOrEmpty(homeKey))
            {
                if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player)) return null;

                // Una cabaña recién estrenada viene sin suelo puesto: se rellena la
                // primera vez, o entrarías a un agujero negro con muebles flotando.
                var home = player.State.Home;
                if (home.FloorTiles.Count == 0) home.FillFloor("floor_madera_clara");
                return home;
            }

            // La clave puede ser un habitante o un edificio de la aldea, y hasta ahora
            // solo se probaba lo primero: al llamar a la puerta de una tienda se
            // preguntaba por el habitante «zona_tienda_comida», no existía ninguno, y
            // el cartel decía «Entrar» y no pasaba absolutamente nada.
            var islanderHome = _housing.GetHomeOf(homeKey);
            if (islanderHome != null) return islanderHome;

            return _housing.GetLayout(homeKey, 0);
        }

        private void Rebuild(RoomLayout layout)
        {
            if (_room != null) Destroy(_room.gameObject);

            CurrentRoom = layout;

            _room = new GameObject("Interior").transform;
            _room.SetParent(transform, worldPositionStays: false);
            _room.localPosition = Anchor;

            BuildFloor(layout);
            BuildWalls(layout);

            // El cuarto no hace sombra sobre sí mismo.
            //
            // El sol de la isla entra de lado, así que las paredes proyectaban una
            // cuña oscura que cortaba el suelo en diagonal y partía la habitación en
            // dos mitades de distinto color. Sin ellas la luz queda plana y la única
            // sombra que se ve es la de los muebles, que es la que dice dónde están
            // apoyados.
            foreach (var renderer in _room.GetComponentsInChildren<MeshRenderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Los muebles cuelgan de un hijo suyo y no del cuarto: amueblar cambia un
            // mueble cada pocos segundos, y rehacer la habitación entera cada vez
            // tiraría también el suelo y las paredes, que no se han movido.
            _furniture = new GameObject("Muebles").transform;
            _furniture.SetParent(_room, worldPositionStays: false);
            BuildFurniture(layout);
        }

        /// <summary>Vuelve a dibujar solo los muebles. La llama el modo amueblar.</summary>
        public void RefreshFurniture()
        {
            if (_room == null || CurrentRoom == null) return;

            if (_furniture != null) Destroy(_furniture.gameObject);
            _furniture = new GameObject("Muebles").transform;
            _furniture.SetParent(_room, worldPositionStays: false);

            BuildFurniture(CurrentRoom);
        }

        private void BuildFloor(RoomLayout layout)
        {
            // Una sola losa grande en vez de una por casilla: son hasta doscientas
            // cincuenta y seis casillas y el suelo se ve de un color liso de todos
            // modos. Las casillas sueltas solo harían falta si cada una pudiera
            // llevar un acabado distinto, y el editor pinta el suelo entero.
            var floor = MeshShapes.Box(new Vector3(layout.Width * Tile, 0.2f, layout.Height * Tile));

            AddMesh(_room, "suelo", floor, ToonPalette.Solid(new Color32(0xC8, 0xA6, 0x7C, 255)),
                    new Vector3(layout.Width * Tile * 0.5f, -0.1f, layout.Height * Tile * 0.5f),
                    solid: true);
        }

        private void BuildWalls(RoomLayout layout)
        {
            const float Height = 3.2f;
            var paper = ToonPalette.Solid(new Color32(0xF4, 0xE7, 0xD2, 255));

            float w = layout.Width * Tile;
            float h = layout.Height * Tile;

            // Las cuatro paredes son sólidas: sin ellas el jugador sale andando de su
            // propia casa por el lado y aparece en la nada de debajo del mundo.
            AddMesh(_room, "pared_n", WallMesh(w, Height), paper,
                    new Vector3(w * 0.5f, Height * 0.5f, h), solid: true);
            AddMesh(_room, "pared_s", WallMesh(w, Height), paper,
                    new Vector3(w * 0.5f, Height * 0.5f, 0f), solid: true);
            AddMesh(_room, "pared_o", WallMesh(h, Height), paper,
                    new Vector3(0f, Height * 0.5f, h * 0.5f),
                    Quaternion.Euler(0f, 90f, 0f), solid: true);
            AddMesh(_room, "pared_e", WallMesh(h, Height), paper,
                    new Vector3(w, Height * 0.5f, h * 0.5f),
                    Quaternion.Euler(0f, 90f, 0f), solid: true);

            // La puerta de salida, en la pared sur y del color de la madera: es lo
            // primero que se busca al querer irse.
            AddMesh(_room, "salida", MeshShapes.Box(new Vector3(1.2f, 2.1f, 0.2f)),
                    ToonPalette.Solid(ToonPalette.TrunkBrown),
                    new Vector3(w * 0.5f, 1.05f, 0.12f));

            // Y el felpudo delante. La cámara mira el cuarto desde arriba y desde el
            // sur, así que de la puerta se ve el canto y poco más: puesta en el suelo,
            // la salida se lee de un vistazo desde donde de verdad se mira.
            AddMesh(_room, "felpudo", MeshShapes.Box(new Vector3(1.8f, 0.06f, 1.1f)),
                    ToonPalette.Solid(new Color32(0xA8, 0x7B, 0x50, 255)),
                    new Vector3(w * 0.5f, 0.03f, 0.85f));
        }

        private void BuildFurniture(RoomLayout layout)
        {
            var objects = layout.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                var placed = objects[i];

                Footprint(placed.CatalogId, placed.Facing, out int cellsX, out int cellsY);
                float sizeX = cellsX * Tile * 0.9f;
                float sizeZ = cellsY * Tile * 0.9f;

                float height = HeightOf(placed.CatalogId);

                // Los de encima de la mesa se levantan hasta la altura de lo que
                // tienen debajo. Sin esto, un candelabro puesto sobre una mesa se
                // dibuja dentro de ella y desde arriba parece que no se colocó nada.
                float floor = placed.Layer == PlacementLayer.Surface ? SurfaceHeightAt(layout, placed) : 0f;

                AddMesh(_furniture, placed.CatalogId,
                        MeshShapes.Box(new Vector3(sizeX, height, sizeZ)),
                        ToonPalette.Solid(ColourOf(placed.CatalogId)),
                        CentreOf(placed.Origin.X, placed.Origin.Y, cellsX, cellsY)
                            + Vector3.up * (floor + height * 0.5f));
            }
        }

        /// <summary>
        /// Cuántas casillas ocupa mirando hacia ahí.
        /// </summary>
        /// <remarks>
        /// Girado el mueble, su huella gira con él: un sofá de tres por uno puesto de
        /// lado ocupa uno por tres, y sin esto atravesaría la pared.
        /// </remarks>
        public static void Footprint(string catalogId, Facing facing, out int cellsX, out int cellsY)
        {
            cellsX = cellsY = 1;
            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
            {
                var item = economy.GetItem(catalogId);
                if (item != null) { cellsX = item.FootprintX; cellsY = item.FootprintY; }
            }

            if (facing != Facing.East && facing != Facing.West) return;
            (cellsX, cellsY) = (cellsY, cellsX);
        }

        /// <summary>
        /// El centro de una huella, en coordenadas de la habitación.
        /// </summary>
        /// <remarks>
        /// Lo usan tanto quien dibuja el mueble como el fantasma del modo amueblar. Es
        /// la razón de que sea público: con dos cuentas distintas, el fantasma se
        /// pinta medio metro al lado de donde acaba cayendo la silla.
        /// </remarks>
        public static Vector3 CentreOf(int cellX, int cellY, int cellsX = 1, int cellsY = 1) =>
            new((cellX + cellsX * 0.5f) * Tile, 0f, (cellY + cellsY * 0.5f) * Tile);

        /// <summary>Sobre qué casilla cae ese punto de la habitación.</summary>
        public static void CellAt(Vector3 local, out int cellX, out int cellY)
        {
            cellX = Mathf.FloorToInt(local.x / Tile);
            cellY = Mathf.FloorToInt(local.z / Tile);
        }

        /// <summary>Lo alto que queda la mesa sobre la que se apoya algo.</summary>
        private static float SurfaceHeightAt(RoomLayout layout, PlacedObject placed)
        {
            float best = 0f;
            foreach (var other in layout.Objects)
            {
                if (other.Layer != PlacementLayer.Furniture) continue;

                Footprint(other.CatalogId, other.Facing, out int ox, out int oy);
                if (placed.Origin.X < other.Origin.X || placed.Origin.X >= other.Origin.X + ox) continue;
                if (placed.Origin.Y < other.Origin.Y || placed.Origin.Y >= other.Origin.Y + oy) continue;

                best = Mathf.Max(best, HeightOf(other.CatalogId));
            }
            return best;
        }

        /// <summary>
        /// Alto y color salen del identificador del mueble.
        /// </summary>
        /// <remarks>
        /// No hay una malla por mueble y no la va a haber: son doscientos cuarenta.
        /// Con el alto y el color sacados del nombre, dos muebles distintos se ven
        /// distintos y el mismo se ve igual en todas las partidas, que es lo que hace
        /// falta para reconocer tu propia casa.
        /// </remarks>
        public static float HeightOf(string catalogId)
        {
            uint hash = Hash(catalogId);
            return 0.45f + (hash % 100) / 100f * 1.4f;
        }

        private static Color ColourOf(string catalogId)
        {
            uint hash = Hash(catalogId);
            return Color.HSVToRGB((hash % 360) / 360f, 0.32f, 0.86f);
        }

        private static uint Hash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static Mesh WallMesh(float length, float height) =>
            MeshShapes.Box(new Vector3(length, height, 0.24f));

        private static void Teleport(GameObject body, Vector3 to)
        {
            // Hay que apagar el controlador: si no, se come el cambio de posición y
            // deja al jugador donde estaba, con la habitación montada y sin nadie.
            var controller = body.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            body.transform.position = to;

            if (controller != null) controller.enabled = true;
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material,
                                    Vector3 offset, Quaternion rotation = default,
                                    bool solid = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = offset;
            go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            if (solid) go.AddComponent<BoxCollider>();
        }

        private void OnDestroy()
        {
            foreach (var mesh in new[] { _slab, _wall })
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _slab = _wall = null;
        }
    }
}
