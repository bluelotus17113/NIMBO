using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Levanta la isla y sus habitantes, y los mantiene en su sitio.
    /// </summary>
    /// <remarks>
    /// Es la única pieza de arte que habla con la simulación, y solo por servicios.
    /// Todo lo que dibuja se genera aquí en el arranque: el proyecto no tiene ni un
    /// modelo importado, y esa fue la condición para poder tener el juego entero
    /// funcionando antes de ponerle cara.
    /// </remarks>
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField] private float _islandRadius = 100f;
        [SerializeField] private float _islandDepth = 62f;

        private readonly Dictionary<string, IslanderView> _views = new();
        private readonly Dictionary<string, Vector3> _zoneCentres = new();
        private readonly Dictionary<string, GameObject> _decorViews = new();
        private readonly HashSet<string> _built = new();
        private readonly List<Obstacle> _obstacles = new();

        private Transform _islanders;
        private Transform _decorRoot;
        private IIslanderRegistry _registry;
        private IIslandService _island;
        private IPersonalityService _personalities;
        private IDecorService _decor;

        private Rng _rng = Rng.FromTime();
        private float _sinceCheck;

        private void OnEnable() => EventBus.Subscribe<GameLoaded>(OnGameLoaded);

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Unsubscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Unsubscribe<EmotionShown>(OnEmotionShown);
            EventBus.Unsubscribe<BuildingUnlocked>(OnBuildingUnlocked);
            EventBus.Unsubscribe<DecorPlaced>(OnDecorPlaced);
            EventBus.Unsubscribe<DecorRemoved>(OnDecorRemoved);
            EventBus.Unsubscribe<DecorMoved>(OnDecorMoved);
        }

        /// <summary>Se acaba de abrir una zona: se levanta ahí mismo, sin recargar.</summary>
        private void OnBuildingUnlocked(BuildingUnlocked evt)
        {
            var zones = transform.Find("Zonas");
            if (zones == null) return;
            if (_island.TryGetSpawnPoint(evt.BuildingId, out var centre))
                BuildZone(zones, evt.BuildingId, centre);
        }

        private void OnGameLoaded(GameLoaded _)
        {
            if (!ServiceRegistry.TryGet(out _registry)) return;
            if (!ServiceRegistry.TryGet(out _island)) return;
            ServiceRegistry.TryGet(out _personalities);

            ServiceRegistry.TryGet(out _decor);

            BuildIsland();
            BuildZones();
            BuildDecor();

            _islanders = new GameObject("Habitantes").transform;
            _islanders.SetParent(transform, worldPositionStays: false);

            foreach (var islander in _registry.All) Spawn(islander);

            SpawnPlayer();

            EventBus.Subscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Subscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Subscribe<EmotionShown>(OnEmotionShown);
            EventBus.Subscribe<BuildingUnlocked>(OnBuildingUnlocked);
            EventBus.Subscribe<DecorPlaced>(OnDecorPlaced);
            EventBus.Subscribe<DecorRemoved>(OnDecorRemoved);
            EventBus.Subscribe<DecorMoved>(OnDecorMoved);
        }

        private void BuildIsland()
        {
            var island = new GameObject("Isla").transform;
            island.SetParent(transform, worldPositionStays: false);

            AddMesh(island, "prado", IslandMeshBuilder.BuildSurface(_islandRadius),
                    ToonPalette.Solid(ToonPalette.Grass), solid: true);
            AddMesh(island, "roca", IslandMeshBuilder.BuildUnderside(_islandRadius, _islandDepth),
                    ToonPalette.Solid(ToonPalette.Rock));

            BuildHomeIsland();
            BuildBridge();

            // El Árbol Nimbo: el corazón de la isla y su referencia visual. Va en el
            // centro porque es donde cae la plaza y donde mira la cámara al empezar.
            // El tronco del Árbol es lo primero que hay que rodear: está en mitad de
            // la plaza, que es por donde pasa todo el mundo.
            _obstacles.Add(new Obstacle(Vector3.zero, 4.5f));

            var (trunk, crown) = IslandMeshBuilder.BuildTree(26f, 9f);
            var tree = new GameObject("Árbol Nimbo").transform;
            tree.SetParent(island, worldPositionStays: false);
            AddMesh(tree, "tronco", trunk, ToonPalette.Solid(ToonPalette.TrunkBrown), solid: true);
            AddMesh(tree, "copa", crown, ToonPalette.Solid(ToonPalette.LeafGreen));

            BuildClouds(island);
        }

        /// <summary>
        /// Tu isla: más pequeña y al sur, con tu casa y tu huerto y nadie más.
        /// </summary>
        /// <remarks>
        /// Se genera con el mismo constructor que la de la aldea pero con otra semilla,
        /// para que el borde irregular no salga calcado. Con la misma, las dos islas se
        /// verían como la misma pieza repetida desde el aire.
        /// </remarks>
        private void BuildHomeIsland()
        {
            var home = new GameObject("Isla del jugador").transform;
            home.SetParent(transform, worldPositionStays: false);
            home.localPosition = Data.World.Archipelago.HomeCentre;

            float radius = Data.World.Archipelago.HomeRadius;

            AddMesh(home, "prado", IslandMeshBuilder.BuildSurface(radius, seed: 31u),
                    ToonPalette.Solid(ToonPalette.Grass), solid: true);
            AddMesh(home, "roca", IslandMeshBuilder.BuildUnderside(radius, _islandDepth * 0.6f),
                    ToonPalette.Solid(ToonPalette.Rock));
        }

        /// <summary>
        /// El puente de madera que une las dos islas.
        /// </summary>
        /// <remarks>
        /// Con barandilla, y no es adorno: es lo único que impide caerse a media
        /// travesía. Sin ella, el paso más ancho de la cuenta te tira al vacío y la red
        /// de seguridad te devuelve al principio, que es peor que no poder cruzar.
        /// </remarks>
        private void BuildBridge()
        {
            var bridge = new GameObject("Puente").transform;
            bridge.SetParent(transform, worldPositionStays: false);

            var from = Data.World.Archipelago.BridgeFromVillage;
            var to = Data.World.Archipelago.BridgeToHome;
            float length = Mathf.Abs(from.z - to.z);
            float middle = (from.z + to.z) * 0.5f;
            float width = Data.World.Archipelago.BridgeWidth;

            var plank = ToonPalette.Solid(new Color32(0xA9, 0x86, 0x63, 255));
            var rail = ToonPalette.Solid(ToonPalette.TrunkBrown);

            AddMesh(bridge, "tablero", MeshShapes.Box(new Vector3(width, 0.3f, length)),
                    plank, new Vector3(from.x, -0.15f, middle), solid: true);

            // Barandillas a los dos lados, sólidas: son las que te mantienen encima.
            for (int side = -1; side <= 1; side += 2)
            {
                AddMesh(bridge, "baranda", MeshShapes.Box(new Vector3(0.22f, 1.1f, length)),
                        rail, new Vector3(from.x + side * width * 0.5f, 0.55f, middle),
                        solid: true);
            }

            // Postes cada pocos metros: sin ellos el puente parece una tabla flotando.
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 6f));
            for (int i = 0; i <= posts; i++)
            {
                float z = Mathf.Lerp(from.z, to.z, i / (float)posts);
                for (int side = -1; side <= 1; side += 2)
                    AddMesh(bridge, "poste", MeshShapes.Box(new Vector3(0.3f, 1.5f, 0.3f)),
                            rail, new Vector3(from.x + side * width * 0.5f, 0.4f, z));
            }
        }

        /// <summary>Nubes alrededor y por debajo: son las que venden que la isla flota.</summary>
        private void BuildClouds(Transform parent)
        {
            var clouds = new GameObject("Nubes").transform;
            clouds.SetParent(parent, worldPositionStays: false);

            var material = ToonPalette.Solid(ToonPalette.CloudWhite, smoothness: 0.02f);
            var blob = MeshShapes.Sphere(12, 9, new Vector3(1f, 0.55f, 1f));

            for (int i = 0; i < 26; i++)
            {
                float angle = _rng.Range(0f, Mathf.PI * 2f);
                float distance = _rng.Range(_islandRadius * 0.95f, _islandRadius * 1.9f);
                float y = _rng.Range(-_islandDepth * 0.85f, -4f);

                var cloud = new GameObject($"nube_{i}").transform;
                cloud.SetParent(clouds, worldPositionStays: false);
                cloud.localPosition = new Vector3(Mathf.Cos(angle) * distance, y,
                                                  Mathf.Sin(angle) * distance);
                cloud.localScale = Vector3.one * _rng.Range(14f, 38f);

                AddMesh(cloud, "masa", blob, material);
            }
        }

        private void BuildZones()
        {
            var zones = new GameObject("Zonas").transform;
            zones.SetParent(transform, worldPositionStays: false);

            foreach (var zoneId in _island.ZoneIds)
            {
                if (!_island.TryGetSpawnPoint(zoneId, out var centre)) continue;
                _zoneCentres[zoneId] = centre;

                // Solo se levanta lo que está abierto. Una zona cerrada es un claro
                // de hierba, y ver ese hueco es lo que hace querer desbloquearla.
                if (_island.IsUnlocked(zoneId)) BuildZone(zones, zoneId, centre);
            }
        }

        private void BuildZone(Transform parent, string zoneId, Vector3 centre)
        {
            if (_built.Contains(zoneId)) return;
            _built.Add(zoneId);

            var purpose = _island.PurposeOf(zoneId);
            var meshes = BuildingMeshBuilder.Build(purpose);

            var zone = new GameObject(zoneId).transform;
            zone.SetParent(parent, worldPositionStays: false);
            zone.localPosition = centre;

            // Cada zona mira al centro de la isla: así las fachadas dan a la plaza y
            // no a la nada, sin tener que anotar una rotación por zona a mano.
            var toCentre = new Vector3(-centre.x, 0f, -centre.z);
            if (toCentre.sqrMagnitude > 0.01f)
                zone.localRotation = Quaternion.LookRotation(toCentre.normalized);

            if (meshes.Walls != null)
            {
                AddMesh(zone, "muros", meshes.Walls, ToonPalette.Solid(ToonPalette.WallCream),
                        solid: true);

                // El bulto sale de la malla que se acaba de levantar, no de un número
                // escrito a mano: el día que un edificio cambie de tamaño, el rodeo
                // cambia con él en vez de quedarse desfasado en silencio.
                var size = meshes.Walls.bounds.size;
                _obstacles.Add(new Obstacle(centre, Mathf.Max(size.x, size.z) * 0.5f));
            }
            if (meshes.Roof != null)
                AddMesh(zone, "tejado", meshes.Roof,
                        ToonPalette.Solid(BuildingMeshBuilder.RoofColor(purpose)));
            if (meshes.Trim != null)
                AddMesh(zone, "detalle", meshes.Trim,
                        ToonPalette.Solid(purpose == ZonePurpose.Nature
                            ? ToonPalette.Water : ToonPalette.TrunkBrown));
        }

        // ── Los adornos que ha puesto el jugador ─────────────────────────────
        //
        // Se levantan después de las zonas porque van colocados en coordenadas de
        // zona: sin el centro de la zona ya calculado, todos aterrizarían apilados
        // en mitad de la plaza.

        private void BuildDecor()
        {
            _decorRoot = new GameObject("Adornos").transform;
            _decorRoot.SetParent(transform, worldPositionStays: false);

            if (_decor == null) return;

            var placed = _decor.Placed;
            for (int i = 0; i < placed.Count; i++) SpawnDecor(placed[i]);
        }

        private void SpawnDecor(Data.World.DecorPlacement placement)
        {
            if (placement == null) return;
            if (_decorViews.ContainsKey(placement.PlacementId)) return;
            if (!_decor.TryGetDefinition(placement.CatalogId, out var definition)) return;
            if (!_zoneCentres.TryGetValue(placement.ZoneId, out var centre)) return;

            var go = new GameObject(placement.PlacementId);
            go.transform.SetParent(_decorRoot, worldPositionStays: false);
            go.transform.localPosition = centre + new Vector3(placement.X, placement.Y, placement.Z);
            go.transform.localRotation = Quaternion.Euler(0f, placement.Yaw, 0f);

            AddMesh(go.transform, "cuerpo",
                    DecorMeshBuilder.For(definition.Kind),
                    ToonPalette.Solid(DecorMeshBuilder.ColorOf(placement.CatalogId, definition.Kind)));

            _decorViews[placement.PlacementId] = go;
        }

        private void OnDecorPlaced(DecorPlaced evt)
        {
            if (_decor == null) return;

            // Se busca en la lista del servicio en vez de reconstruirlo del evento:
            // el evento lleva lo justo para saber qué ha pasado, y la posición y el
            // giro los tiene quien manda, que es el servicio.
            foreach (var placement in _decor.Placed)
                if (placement.PlacementId == evt.PlacementId) { SpawnDecor(placement); return; }
        }

        private void OnDecorRemoved(DecorRemoved evt)
        {
            if (!_decorViews.TryGetValue(evt.PlacementId, out var go)) return;
            Destroy(go);
            _decorViews.Remove(evt.PlacementId);
        }

        private void OnDecorMoved(DecorMoved evt)
        {
            if (_decor == null) return;
            if (!_decorViews.TryGetValue(evt.PlacementId, out var go)) return;

            foreach (var placement in _decor.Placed)
            {
                if (placement.PlacementId != evt.PlacementId) continue;
                if (!_zoneCentres.TryGetValue(placement.ZoneId, out var centre)) return;

                go.transform.localPosition = centre + new Vector3(placement.X, placement.Y, placement.Z);
                go.transform.localRotation = Quaternion.Euler(0f, placement.Yaw, 0f);
                return;
            }
        }

        /// <summary>
        /// Pone al protagonista en el mundo y le da la cámara.
        /// </summary>
        /// <remarks>
        /// Va aquí y no en el arranque porque el cuerpo tiene que nacer después de la
        /// isla: sin el prado puesto —y con él su colisionador— el protagonista
        /// aparece en el aire y se cae antes del primer fotograma.
        ///
        /// Si todavía no está creado no pasa nada: el creador de personajes avisará y
        /// se le pondrá entonces.
        /// </remarks>
        private void SpawnPlayer()
        {
            if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player)) return;
            if (!player.Exists) return;

            var body = PlayerView.PlayerBody.Create(
                player.State.Appearance, player.Position, player.State.Yaw, transform);
            body.Bind(player);
            body.gameObject.AddComponent<PlayerView.PlayerInteractor>();
            EventBus.Publish(new PlayerSpawned());

            var camera = Camera.main != null
                ? Camera.main.GetComponent<CameraWork.IslandCamera>()
                : null;
            camera?.Follow(body.transform);
        }

        private void OnDestroy() => DecorMeshBuilder.Clear();

        // ── Lo que la cámara necesita saber del mundo ────────────────────────
        //
        // Es la única puerta de entrada: quien quiera enfocar algo pregunta aquí y
        // no rebusca por la jerarquía con Find. Si mañana los habitantes cuelgan de
        // otro sitio, esto sigue valiendo y no se entera nadie.

        public float IslandRadius => _islandRadius;

        /// <summary>El cuerpo de un habitante, para poder mirarlo. Nulo si no está.</summary>
        public bool TryGetIslander(string islanderId, out Transform body)
        {
            body = null;
            if (string.IsNullOrEmpty(islanderId)) return false;
            if (!_views.TryGetValue(islanderId, out var view) || view == null) return false;

            body = view.transform;
            return true;
        }

        /// <summary>El centro de una zona, esté abierta o cerrada.</summary>
        public bool TryGetZoneCentre(string zoneId, out Vector3 centre) =>
            _zoneCentres.TryGetValue(zoneId ?? "", out centre);

        /// <param name="solid">
        /// Si es cierto, se le pone colisionador. Desde que el protagonista anda por
        /// la isla, lo que no lo lleve es aire: sin esto se cae por el prado en el
        /// primer fotograma. Se pone solo a lo que hay que pisar o rodear —el suelo,
        /// los muros— y nunca a las nubes ni a los adornos pequeños, que multiplicaría
        /// por cien los colisionadores para que el jugador se quede atascado en un
        /// arbusto.
        /// </param>
        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material,
                                    bool solid = false)
            => AddMesh(parent, name, mesh, material, Vector3.zero, solid: solid);

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

            // Caja para lo que es una caja, malla para lo que no: un MeshCollider por
            // cada tabla del puente son cuarenta mallas de colisión para algo que son
            // cuatro cajas.
            if (!solid) return;
            if (name is "tablero" or "baranda" or "muros")
                go.AddComponent<BoxCollider>();
            else
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void OnIslanderCreated(IslanderCreated evt)
        {
            if (_registry.TryGet(evt.IslanderId, out var islander)) Spawn(islander);
        }

        private void OnIslanderLeft(IslanderLeft evt)
        {
            if (!_views.TryGetValue(evt.IslanderId, out var view)) return;
            Destroy(view.gameObject);
            _views.Remove(evt.IslanderId);
        }

        private void OnEmotionShown(EmotionShown evt)
        {
            if (_views.TryGetValue(evt.IslanderId, out var view)) view.SetEmotion(evt.Emotion);
        }

        private void Spawn(IslanderData islander)
        {
            if (_views.ContainsKey(islander.Id)) return;

            float speed = 3.2f;
            if (_personalities != null)
                speed *= _personalities.For(islander.Personality).WalkSpeedMultiplier;

            var view = IslanderView.Create(islander, speed, _islanders);
            view.Obstacles = _obstacles;
            view.PlaceAt(PointIn(islander.CurrentZoneId));
            view.SetEmotion(islander.Mood.Emotion);
            _views[islander.Id] = view;
        }

        private Vector3 PointIn(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && _island.TryGetSpawnPoint(zoneId, out var point))
                return point;

            return _zoneCentres.TryGetValue("zona_plaza", out var plaza) ? plaza : Vector3.zero;
        }

        private void Update()
        {
            if (_registry == null) return;

            // Los destinos se comprueban dos veces por segundo: la simulación decide
            // por horas de juego, así que mirar cada fotograma no aportaría nada.
            _sinceCheck += Time.deltaTime;
            if (_sinceCheck < 0.5f) return;
            _sinceCheck = 0f;

            foreach (var islander in _registry.All)
            {
                if (!_views.TryGetValue(islander.Id, out var view)) continue;
                if (view.IsWalking) continue;

                var destination = PointIn(islander.CurrentZoneId);
                if ((destination - view.transform.position).sqrMagnitude > 9f)
                    view.WalkTo(destination);
            }
        }
    }
}
