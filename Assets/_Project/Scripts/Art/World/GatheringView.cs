using System.Collections;
using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Dibuja lo que hay por la isla para recoger: los árboles, las piedras, las
    /// matas y las flores.
    /// </summary>
    /// <remarks>
    /// Los ciento veinte nodos existían desde el principio —sembrados, guardados, con
    /// su tabla de golpes y su reposición— y no los dibujaba nadie. Se cruzaba el
    /// puente, se andaba por un prado vacío y cada tantos metros salía un cartel que
    /// decía «Talar roble anciano» encima de la nada. Esto es la vista que faltaba, y
    /// por eso <c>NodeGathered</c> y <c>NodeRespawned</c> se publicaban desde el
    /// primer día sin que los escuchara nadie: estaban esperando a esta clase.
    ///
    /// Es el mismo trato que <see cref="FarmView"/> tiene con el huerto: el servicio
    /// decide y avisa, y aquí solo se rehace lo que ha cambiado.
    ///
    /// **Sin colisionadores, a propósito.** Los vecinos no van por física: rodean una
    /// lista corta de bultos gordos con <see cref="WalkAround"/>. Hacer sólidos los
    /// árboles solo para el jugador significaría verle a un vecino cruzar por dentro
    /// del roble que a ti te frena, y eso se nota más que atravesarlo. Meter los ciento
    /// veinte en la lista de bultos arreglaría la asimetría y convertiría cada paso de
    /// cada vecino en un recorrido de ciento veinte comprobaciones.
    ///
    /// (El motivo de antes —que podían caer encima de un edificio— ya no vale: la
    /// siembra esquiva las parcelas y colocar un edificio aparta lo que pille debajo.)
    /// </remarks>
    public sealed class GatheringView : MonoBehaviour
    {
        /// <summary>Desde qué altura se busca el suelo al plantar un nodo.</summary>
        private const float ProbeHeight = 40f;

        private readonly Dictionary<string, GameObject> _bodies = new();

        private IGatheringService _gathering;
        private Transform _root;
        private bool _pending;

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<NodeHit>(OnNodeHit);
            EventBus.Subscribe<NodeGathered>(OnNodeGathered);
            EventBus.Subscribe<NodeRespawned>(OnNodeRespawned);
            EventBus.Subscribe<NodeMoved>(OnNodeMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<NodeHit>(OnNodeHit);
            EventBus.Unsubscribe<NodeGathered>(OnNodeGathered);
            EventBus.Unsubscribe<NodeRespawned>(OnNodeRespawned);
            EventBus.Unsubscribe<NodeMoved>(OnNodeMoved);
        }

        /// <summary>
        /// No se construye aquí: se apunta para el fotograma siguiente.
        /// </summary>
        /// <remarks>
        /// Cada nodo busca el suelo con un rayo, y el suelo es el colisionador que
        /// monta <see cref="WorldView"/> con este mismo aviso. Quién de los dos lo
        /// recibe antes depende del orden de los componentes en la escena, que es
        /// justo la clase de detalle que se rompe el día que alguien los reordene.
        /// Esperando un fotograma, el prado ya está puesto seguro.
        /// </remarks>
        private void OnGameLoaded(GameLoaded _) => _pending = true;

        private void Update()
        {
            if (!_pending) return;
            _pending = false;
            BuildAll();
        }

        private void BuildAll()
        {
            if (!ServiceRegistry.TryGet(out _gathering)) return;

            _root = new GameObject("Recursos").transform;
            _root.SetParent(transform, worldPositionStays: false);

            var nodes = _gathering.Nodes;
            for (int i = 0; i < nodes.Count; i++) Rebuild(nodes[i]);
        }

        private void OnNodeGathered(NodeGathered evt) => RebuildById(evt.InstanceId);
        private void OnNodeRespawned(NodeRespawned evt) => RebuildById(evt.InstanceId);

        /// <summary>Le ha caído un edificio encima y se ha apartado: se redibuja allí.</summary>
        private void OnNodeMoved(NodeMoved evt) => RebuildById(evt.InstanceId);

        /// <summary>
        /// Le ha dado pero aún aguanta: se sacude y sigue en pie.
        /// </summary>
        /// <remarks>
        /// Es la única señal de que el golpe ha contado. Sin ella, dar cuatro hachazos
        /// a un abedul y que no pase nada hasta el quinto se lee como que la
        /// herramienta no funciona — que es literalmente el fallo que ya tuvo este
        /// juego una vez (ver el commit «Las herramientas no funcionaban: ninguna»).
        /// </remarks>
        private void OnNodeHit(NodeHit evt)
        {
            if (!_bodies.TryGetValue(evt.InstanceId, out var body) || body == null) return;
            StartCoroutine(Shake(body.transform));
        }

        private static IEnumerator Shake(Transform body)
        {
            var origin = body.localPosition;

            // Corta y amortiguada: lo justo para que el golpe se sienta. Alargarla
            // convierte un hachazo en un terremoto.
            const float Duration = 0.22f;
            for (float t = 0f; t < Duration; t += Time.deltaTime)
            {
                float decay = 1f - t / Duration;
                float wobble = Mathf.Sin(t * 62f) * 0.12f * decay;
                body.localPosition = origin + new Vector3(wobble, 0f, wobble * 0.4f);
                yield return null;
            }

            body.localPosition = origin;
        }

        private void RebuildById(string instanceId)
        {
            if (_gathering == null) return;
            if (!_gathering.TryGetNode(instanceId, out var node)) return;
            Rebuild(node);
        }

        private void Rebuild(ResourceNode node)
        {
            if (_root == null || node == null) return;
            if (!_gathering.TryGetDefinition(node.NodeId, out var definition)) return;

            if (_bodies.TryGetValue(node.InstanceId, out var old) && old != null) Destroy(old);

            var go = new GameObject(node.InstanceId);
            go.transform.SetParent(_root, worldPositionStays: false);
            go.transform.SetPositionAndRotation(
                Grounded(new Vector3(node.X, node.Y, node.Z)),
                Quaternion.Euler(0f, node.Yaw, 0f));

            if (node.IsDepleted) AddStump(go.transform, definition);
            else AddBody(go.transform, definition);

            _bodies[node.InstanceId] = go;
        }

        /// <summary>
        /// El punto puesto sobre el prado. Los nodos se siembran a altura cero porque
        /// quien los siembra no sabe cómo ondula el terreno; quien los dibuja sí.
        /// </summary>
        private static Vector3 Grounded(Vector3 position)
        {
            var from = new Vector3(position.x, position.y + ProbeHeight, position.z);
            return Physics.Raycast(from, Vector3.down, out var hit, ProbeHeight * 2f,
                                   ~0, QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
        }

        // ── cuerpos ──────────────────────────────────────────────────────────

        private static void AddBody(Transform parent, in NodeDefinition definition)
        {
            var tint = TintOf(definition);

            switch (definition.Kind)
            {
                case NodeKind.Tree:
                    AddPart(parent, "tronco", TreeTrunk(), ToonPalette.TrunkBrown);
                    AddPart(parent, "copa", TreeCrown(), tint,
                            material: ToonPalette.Foliage(tint), receivesShadows: false);
                    break;

                case NodeKind.Rock:
                    // La piedra no se mueve con el viento ni se ve por dentro: va con
                    // el material de rampa dura, que es el que le dibuja las aristas.
                    AddPart(parent, "bloque", Boulder(), tint,
                            new Vector3(0f, Rest(Boulder(), 1.6f), 0f), Vector3.one * 1.6f,
                            material: ToonPalette.Stone(tint));
                    AddPart(parent, "esquirla", Chip(), tint,
                            new Vector3(0.8f, Rest(Chip(), 0.75f), 0.35f), Vector3.one * 0.75f,
                            material: ToonPalette.Stone(tint));
                    break;

                case NodeKind.Bush:
                    // Una mata es una copa de árbol pequeña y sin tronco: el mismo
                    // truco de las normales, así que se lee como un bulto y no como
                    // dos bolas pegadas.
                    // Sin levantarla: los lóbulos ya vienen tejidos apoyados en el
                    // cero, y la mata se hunde un palmo a propósito para que no se le
                    // vea la costura de abajo.
                    AddPart(parent, "mata", BushClump(), tint,
                            material: ToonPalette.Foliage(tint, windStrength: 0.16f),
                            receivesShadows: false);
                    break;

                case NodeKind.Herb:
                    // El mismo manojo que el prado, pero más alto: una hierba de
                    // recoger tiene que leerse como hierba, y ahora hay hierba de
                    // verdad con la que parecerse. Los conos de antes eran un manojo
                    // de púas al lado de un césped.
                    AddPart(parent, "manojo", Herbs(), tint,
                            material: ToonPalette.Foliage(tint, windStrength: 0.30f));
                    break;

                case NodeKind.Flower:
                    AddPart(parent, "tallo", Stem(), ToonPalette.LeafGreen,
                            new Vector3(0f, Rest(Stem(), 1f), 0f));
                    AddPart(parent, "corola", Blob(), tint,
                            new Vector3(0f, Rest(Stem(), 1f) * 2f + Rest(Blob(), 0.38f), 0f),
                            new Vector3(0.62f, 0.38f, 0.62f));
                    break;

                default:
                    AddPart(parent, "jirón", Blob(), tint,
                            new Vector3(0f, Rest(Blob(), 0.62f), 0f),
                            new Vector3(1.2f, 0.62f, 1f));
                    break;
            }
        }

        /// <summary>
        /// Lo que queda cuando se ha recogido, mientras espera a reponerse.
        /// </summary>
        /// <remarks>
        /// Un tocón y unos cascotes en vez de nada. Que un árbol desaparezca entero
        /// deja al jugador sin saber si ahí había algo o se lo ha imaginado, y lo que
        /// tiene que saber es «esto vuelve dentro de unos días»: el sitio se acuerda.
        /// Lo pequeño —hierbas, flores— sí se va del todo, porque un tocón de flor no
        /// existe.
        /// </remarks>
        private static void AddStump(Transform parent, in NodeDefinition definition)
        {
            switch (definition.Kind)
            {
                case NodeKind.Tree:
                    AddPart(parent, "tocón", Stump(), ToonPalette.TrunkBrown,
                            new Vector3(0f, Rest(Stump(), 1f), 0f));
                    break;

                case NodeKind.Rock:
                    AddPart(parent, "cascote", Boulder(), ToonPalette.RockDeep,
                            new Vector3(0f, Rest(Boulder(), 0.35f), 0f),
                            new Vector3(0.9f, 0.35f, 0.9f),
                            material: ToonPalette.Stone(ToonPalette.RockDeep));
                    break;
            }
        }

        /// <summary>
        /// A qué altura hay que poner esa pieza para que se apoye en el suelo.
        /// </summary>
        /// <remarks>
        /// Sale de la propia malla, no de un número escrito a mano. Escritos a mano ya
        /// estaban mal la primera vez: los arbustos salieron medio enterrados y los
        /// restos de nube flotando siete centímetros, porque hay que acordarse de que
        /// la esfera viene achatada y de multiplicar por la escala. Preguntándoselo a
        /// la malla no hay nada que recordar.
        /// </remarks>
        private static float Rest(Mesh mesh, float scaleY) => mesh.bounds.extents.y * scaleY;

        /// <summary>
        /// Una pieza. Con <paramref name="receivesShadows"/> a falso, no le entra la
        /// sombra proyectada de nadie.
        /// </summary>
        /// <remarks>
        /// **Las copas y las matas no reciben sombras, a propósito.** Una copa es un
        /// bulto cerrado y se hace sombra a sí misma: el envés cae dentro de su propio
        /// mapa de sombras, y el mapa de sombras tiene resolución. En las capturas eso
        /// se veía como una línea de dientes de sierra cruzando la copa por la mitad,
        /// una escalera de píxeles gordos donde debería haber un envés. Lo que la
        /// oscurece ahora es la oclusión escrita en el vértice, que no tiene bordes
        /// porque se interpola: es también lo que hace la referencia, que resuelve el
        /// envés con un nodo de oclusión ambiental y no con sombras.
        ///
        /// El prado sí las recibe. La sombra del árbol sobre la hierba es de las cosas
        /// que más dicen «esto es un sitio», y ahí el borde cae sobre una superficie
        /// grande y suave donde el dentado no se ve.
        /// </remarks>
        private static void AddPart(Transform parent, string name, Mesh mesh, Color colour,
                                    Vector3 offset = default, Vector3 scale = default,
                                    Quaternion rotation = default, Material material = null,
                                    bool receivesShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = offset;
            go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            go.transform.localScale = scale == default ? Vector3.one : scale;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material ?? ToonPalette.Solid(colour);
            renderer.receiveShadows = receivesShadows;
        }

        /// <summary>
        /// El color de una especie. Sale de su identificador, así que un pino de nube
        /// es del mismo verde en todas las partidas y distinto del de un roble.
        /// </summary>
        /// <remarks>
        /// Mismo truco que en <see cref="DecorMeshBuilder.ColorOf"/>: un desvío corto
        /// de tono y claridad sobre el color base del tipo. Lo justo para distinguir
        /// cuatro árboles de un vistazo sin que ninguno se salga de la paleta.
        /// </remarks>
        private static Color TintOf(in NodeDefinition definition)
        {
            Color.RGBToHSV(BaseTint(definition.Kind), out float h, out float s, out float v);

            uint hash = 2166136261u;
            for (int i = 0; i < definition.NodeId.Length; i++)
            {
                hash ^= definition.NodeId[i];
                hash *= 16777619u;
            }

            h = Mathf.Repeat(h + ((hash & 0xFF) / 255f - 0.5f) * 0.13f, 1f);
            v = Mathf.Clamp(v + (((hash >> 8) & 0xFF) / 255f - 0.5f) * 0.22f, 0.3f, 0.97f);
            return Color.HSVToRGB(h, s, v);
        }

        private static Color BaseTint(NodeKind kind) => kind switch
        {
            NodeKind.Tree => ToonPalette.LeafGreen,
            NodeKind.Rock => ToonPalette.Rock,
            NodeKind.Bush => ToonPalette.GrassDark,
            NodeKind.Herb => new Color32(0x9F, 0xD8, 0x8A, 255),
            NodeKind.Flower => new Color32(0xF0, 0x8F, 0xB4, 255),
            _ => ToonPalette.CloudWhite,
        };

        // ── mallas ───────────────────────────────────────────────────────────
        //
        // Una por forma y compartida entre los ciento veinte nodos. Generar una malla
        // por árbol serían ciento veinte copias idénticas ocupando memoria para nada.

        private static Mesh _trunk, _crown, _stump, _boulder, _chip, _bush, _herbs, _blob, _stem;

        private static Mesh TreeTrunk() => _trunk ??= IslandMeshBuilder.BuildTree(4.6f, 1.5f).trunk;
        private static Mesh TreeCrown() => _crown ??= IslandMeshBuilder.BuildTree(4.6f, 1.5f).crown;
        private static Mesh Stump() => _stump ??= MeshShapes.Cylinder(8, 0.32f, 0.4f, 0.4f);
        private static Mesh Blob() => _blob ??= MeshShapes.Sphere(10, 7, new Vector3(1f, 0.82f, 1f));
        private static Mesh Stem() => _stem ??= MeshShapes.Cylinder(6, 0.035f, 0.05f, 0.72f);

        // Dos pedruscos con semillas distintas: el bloque y su esquirla tienen que
        // ser dos piedras, no la misma piedra dos veces de tamaños distintos.
        private static Mesh Boulder() => _boulder ??= RockMeshBuilder.Boulder(11u);
        private static Mesh Chip() => _chip ??= RockMeshBuilder.Boulder(29u, segments: 6, rings: 4,
                                                                       squash: 0.82f, roughness: 0.34f);

        /// <summary>Una mata: tres lóbulos con las normales de un solo bulto.</summary>
        private static Mesh BushClump()
        {
            if (_bush != null) return _bush;

            var lobes = new List<FoliageMeshBuilder.Lobe>
            {
                new(new Vector3(0f, 0.62f, 0f), new Vector3(0.95f, 0.72f, 0.95f)),
                new(new Vector3(0.52f, 0.42f, 0.18f), new Vector3(0.60f, 0.50f, 0.60f)),
                new(new Vector3(-0.38f, 0.40f, -0.34f), new Vector3(0.55f, 0.46f, 0.55f)),
            };

            return _bush = FoliageMeshBuilder.Weave(lobes, new Vector3(0f, 0.30f, 0f), 23u,
                                                    "arbusto", segments: 12, rings: 8);
        }

        /// <summary>Un manojo de hierba alta, tejido con el mismo tejedor que el prado.</summary>
        private static Mesh Herbs()
        {
            if (_herbs != null) return _herbs;

            var tufts = new List<Meadow.Tuft>(5);
            var rng = new Nimbo.Core.Util.Rng(97u);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * (Mathf.PI * 2f / 5f);
                float distance = rng.Range(0f, 0.26f);
                tufts.Add(new Meadow.Tuft(
                    new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance),
                    Vector3.up, rng.Range(0f, 360f), rng.Range(1.25f, 1.7f), rng.NextFloat()));
            }

            return _herbs = Meadow.Weave(tufts, 0, tufts.Count, "manojo_hierba");
        }

        private void OnDestroy()
        {
            foreach (var mesh in new[] { _trunk, _crown, _stump, _boulder, _chip, _bush, _herbs,
                                         _blob, _stem })
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _trunk = _crown = _stump = _boulder = _chip = _bush = _herbs = _blob = _stem = null;
        }
    }
}
