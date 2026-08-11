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
        private const float Tile = 1.5f;

        private Transform _room;
        private IHousingService _housing;

        private static Mesh _slab;
        private static Mesh _wall;

        /// <summary>Cierto mientras el jugador está dentro de una casa.</summary>
        public bool Inside { get; private set; }

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

            return _housing.GetHomeOf(homeKey);
        }

        private void Rebuild(RoomLayout layout)
        {
            if (_room != null) Destroy(_room.gameObject);

            _room = new GameObject("Interior").transform;
            _room.SetParent(transform, worldPositionStays: false);
            _room.localPosition = Anchor;

            BuildFloor(layout);
            BuildWalls(layout);
            BuildFurniture(layout);
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
        }

        private void BuildFurniture(RoomLayout layout)
        {
            var objects = layout.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                var placed = objects[i];

                int footX = 1, footY = 1;
                if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
                {
                    var item = economy.GetItem(placed.CatalogId);
                    if (item != null) { footX = item.FootprintX; footY = item.FootprintY; }
                }

                // Girado el mueble, su huella gira con él: un sofá de tres por uno
                // puesto de lado ocupa uno por tres, y sin esto atravesaría la pared.
                bool turned = placed.Facing == Facing.East || placed.Facing == Facing.West;
                float sizeX = (turned ? footY : footX) * Tile * 0.9f;
                float sizeZ = (turned ? footX : footY) * Tile * 0.9f;

                float height = HeightOf(placed.CatalogId);

                AddMesh(_room, placed.CatalogId, MeshShapes.Box(new Vector3(sizeX, height, sizeZ)),
                        ToonPalette.Solid(ColourOf(placed.CatalogId)),
                        new Vector3((placed.Origin.X + sizeX / Tile * 0.5f) * Tile,
                                    height * 0.5f,
                                    (placed.Origin.Y + sizeZ / Tile * 0.5f) * Tile));
            }
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
        private static float HeightOf(string catalogId)
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
