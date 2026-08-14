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
                    AddPart(parent, "copa", TreeCrown(), tint);
                    break;

                case NodeKind.Rock:
                    AddPart(parent, "bloque", Boulder(), tint,
                            new Vector3(0f, Rest(Boulder(), 1.6f), 0f), Vector3.one * 1.6f);
                    AddPart(parent, "esquirla", Boulder(), tint,
                            new Vector3(0.8f, Rest(Boulder(), 0.75f), 0.35f), Vector3.one * 0.75f);
                    break;

                case NodeKind.Bush:
                    AddPart(parent, "mata", Blob(), tint,
                            new Vector3(0f, Rest(Blob(), 1.45f), 0f), Vector3.one * 1.45f);
                    AddPart(parent, "brote", Blob(), tint,
                            new Vector3(0.62f, Rest(Blob(), 0.62f), -0.34f), Vector3.one * 0.62f);
                    break;

                case NodeKind.Herb:
                    // Un manojo abierto. Cinco briznas gordas y no cinco láminas: la
                    // primera versión eran cajas de dos centímetros de canto y desde
                    // la cámara del juego no se veía absolutamente nada, solo un
                    // rasguño en el prado.
                    for (int i = 0; i < 7; i++)
                    {
                        float angle = i * 51f;
                        var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.13f, 0f, 0f);
                        offset.y = Rest(Blade(), 1f) * 0.9f;

                        AddPart(parent, "brizna", Blade(), tint, offset,
                                Vector3.one, Quaternion.Euler(0f, angle, 26f));
                    }
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
                            new Vector3(0.9f, 0.35f, 0.9f));
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

        private static void AddPart(Transform parent, string name, Mesh mesh, Color colour,
                                    Vector3 offset = default, Vector3 scale = default,
                                    Quaternion rotation = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = offset;
            go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            go.transform.localScale = scale == default ? Vector3.one : scale;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = ToonPalette.Solid(colour);
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

        private static Mesh _trunk, _crown, _stump, _boulder, _blob, _blade, _stem;

        private static Mesh TreeTrunk() => _trunk ??= IslandMeshBuilder.BuildTree(4.6f, 1.5f).trunk;
        private static Mesh TreeCrown() => _crown ??= IslandMeshBuilder.BuildTree(4.6f, 1.5f).crown;
        private static Mesh Stump() => _stump ??= MeshShapes.Cylinder(8, 0.32f, 0.4f, 0.4f);
        private static Mesh Boulder() => _boulder ??= MeshShapes.Sphere(8, 6, new Vector3(1f, 0.7f, 0.85f));
        private static Mesh Blob() => _blob ??= MeshShapes.Sphere(10, 7, new Vector3(1f, 0.82f, 1f));
        // Un cono, no una aguja: con el remate en punta fina el manojo se leía como un
        // andamio de alambre. Ancho abajo y corto es lo que parece una mata de hierba.
        private static Mesh Blade() => _blade ??= MeshShapes.Cylinder(5, 0.008f, 0.11f, 0.62f);
        private static Mesh Stem() => _stem ??= MeshShapes.Cylinder(6, 0.035f, 0.05f, 0.72f);

        private void OnDestroy()
        {
            foreach (var mesh in new[] { _trunk, _crown, _stump, _boulder, _blob, _blade, _stem })
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _trunk = _crown = _stump = _boulder = _blob = _blade = _stem = null;
        }
    }
}
