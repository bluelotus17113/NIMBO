using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.PlayerView
{
    /// <summary>Con qué está a punto de interactuar el jugador.</summary>
    public enum TargetKind { None = 0, Islander = 1, Node = 2, FarmTile = 3 }

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
        }

        private void FindTarget()
        {
            Kind = TargetKind.None;
            TargetId = "";
            Prompt = "";

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
                    Prompt = $"Hablar con {islander.Identity.ShortName}";
                }
            }

            if (Kind == TargetKind.Islander) return;

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
            var world = GetComponentInParent<World.WorldView>();
            if (world == null) return false;
            if (!world.TryGetIslander(islanderId, out var body)) return false;

            position = body.position;
            return true;
        }

        /// <summary>Hace lo que diga el objetivo. Es la única tecla de acción del juego.</summary>
        private void Act()
        {
            switch (Kind)
            {
                case TargetKind.Islander:
                    // Abrir su ficha es lo que ya hacía clicar su nombre: se reutiliza
                    // el mismo aviso para no tener dos caminos que hagan lo mismo.
                    EventBus.Publish(new IslanderFocused(TargetId));
                    break;

                case TargetKind.Node:
                    GatherTarget();
                    break;
            }
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
