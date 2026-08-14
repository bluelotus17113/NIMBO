using System.Text;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Art.PlayerView
{
    /// <summary>
    /// El panel de pruebas: teletransporte, herramientas y saltar el día.
    /// </summary>
    /// <remarks>
    /// Existe porque probar este juego a pie no es viable. Para ver si un roble se
    /// sacude al hachazo hay que salir de casa, cruzar el puente, encontrar un roble y
    /// llevar el hacha encima: cinco minutos reales por prueba, y si lo que quieres
    /// mirar es lo de la otra punta, otros cinco. Con el puente roto ni eso — la aldea
    /// era inalcanzable y no había forma de saberlo desde aquí.
    ///
    /// Es <c>OnGUI</c> a propósito, y es la única cosa del juego que lo usa. La
    /// interfaz de verdad es UI Toolkit con su hoja de estilo y su tema, y meter esto
    /// ahí significaría documentos, estilos y un sitio en la jerarquía para algo que no
    /// sale en el juego. <c>OnGUI</c> no necesita nada: se pinta y se va.
    ///
    /// Se abre con **F1** y arranca cerrado. Mientras está abierto no le quita el mando
    /// al protagonista salvo que le des a algo, para poder mirar el efecto sin cerrar.
    /// </remarks>
    public sealed class DebugPanel : MonoBehaviour
    {
        private const float Margin = 12f;
        private const float Width = 260f;

        private bool _open;
        private string _message = "";
        private float _messageUntil;

        private PlayerBody _body;
        private GUIStyle _box;

        private void Awake() => _body = GetComponent<PlayerBody>();

        /// <summary>
        /// Cierto mientras el panel esté abierto. Lo mira la cámara para soltar el
        /// ratón, porque esto se maneja clicando.
        /// </summary>
        /// <remarks>
        /// Estático y no por aviso, aunque los paneles de la interfaz sí avisen. Es que
        /// aquellos viven en <c>Nimbo.UI</c>, que la cámara no ve, y este vive en el
        /// mismo ensamblado que ella. Y sobre todo: con dos emisores del mismo aviso se
        /// pisan — cerrar la mochila mandaría «ya no hace falta el puntero» con este
        /// panel abierto delante, y sus botones dejarían de poder pulsarse.
        /// </remarks>
        public static bool AnyOpen { get; private set; }

        private void Update()
        {
            // Sistema de entrada antiguo: nada de Keyboard.current.
            if (Input.GetKeyDown(KeyCode.F1)) SetOpen(!_open);
        }

        private void OnDisable() => SetOpen(false);

        private void SetOpen(bool open)
        {
            _open = open;
            AnyOpen = open;
        }

        private void OnGUI()
        {
            if (!_open)
            {
                GUI.Label(new Rect(Margin, Margin, 220f, 20f), "F1 — pruebas");
                return;
            }

            _box ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };

            float height = 350f;
            GUILayout.BeginArea(new Rect(Margin, Margin, Width, height), _box);

            GUILayout.Label($"<b>Pruebas</b>  ·  {Where()}", Rich());
            GUILayout.Space(6f);

            GUILayout.Label("Ir a", Rich());
            if (GUILayout.Button("La plaza (Árbol Nimbo)")) TeleportTo(new Vector3(0f, 0f, -14f));
            if (GUILayout.Button("La arboleda más cercana")) TeleportToNode();
            if (GUILayout.Button("Un vecino")) TeleportToIslander();
            if (GUILayout.Button("Mi casa")) TeleportTo(Data.Player.PlayerHome.Spawn);

            GUILayout.Space(8f);
            GUILayout.Label("Dar", Rich());
            if (GUILayout.Button("Las cuatro herramientas")) GiveTools();
            if (GUILayout.Button("Llenar el vigor")) FillVigor();

            GUILayout.Space(8f);
            GUILayout.Label("Tiempo", Rich());
            if (GUILayout.Button("Saltar al día siguiente")) SkipDay();

            if (Time.unscaledTime < _messageUntil)
            {
                GUILayout.Space(8f);
                GUILayout.Label(_message, Rich());
            }

            GUILayout.EndArea();
        }

        private static GUIStyle _rich;
        private static GUIStyle Rich() =>
            _rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };

        private string Where()
        {
            var p = transform.position;
            string isla = Archipelago.SideOf(p) == IslandSide.Home ? "tu isla" : "la aldea";
            if (Archipelago.OnBridge(p)) isla = "el puente";
            return $"{isla}  ({p.x:0}, {p.y:0.0}, {p.z:0})";
        }

        // ── ir a ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Te pone ahí, buscando el suelo primero.
        /// </summary>
        /// <remarks>
        /// Hay que apagar el <c>CharacterController</c> para moverlo: si no, se come el
        /// cambio de posición y te deja donde estabas. Es el mismo detalle que hay en
        /// la red anticaída de <see cref="PlayerBody"/>.
        /// </remarks>
        private void TeleportTo(Vector3 target)
        {
            var above = target + Vector3.up * 60f;
            if (Physics.Raycast(above, Vector3.down, out var hit, 120f,
                                ~0, QueryTriggerInteraction.Ignore))
                target = hit.point;

            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = target + Vector3.up * 0.6f;
            if (controller != null) controller.enabled = true;

            Say($"Ahí estás: {target.x:0}, {target.z:0}");
        }

        private void TeleportToNode()
        {
            if (!ServiceRegistry.TryGet<IGatheringService>(out var gathering))
            {
                Say("No hay recolección montada.");
                return;
            }

            var me = transform.position;
            ResourceNode best = null;
            float bestDistance = float.MaxValue;

            foreach (var node in gathering.Nodes)
            {
                if (node.IsDepleted) continue;
                if (!gathering.TryGetDefinition(node.NodeId, out var definition)) continue;

                // Un árbol, que es lo que se quiere probar: los hachazos, la sacudida y
                // el tocón. Una flor se coge de un toque y no enseña nada.
                if (definition.Kind != NodeKind.Tree) continue;

                float dx = node.X - me.x, dz = node.Z - me.z;
                float distance = dx * dx + dz * dz;
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = node;
            }

            if (best == null) { Say("No queda ningún árbol en pie."); return; }

            // A dos metros del tronco, que es donde alcanza el brazo.
            var at = new Vector3(best.X, 0f, best.Z);
            TeleportTo(at + (me - at).normalized * 2f);
        }

        private void TeleportToIslander()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry) || registry.Count == 0)
            {
                Say("No hay vecinos.");
                return;
            }

            var world = GetComponentInParent<World.WorldView>();
            if (world == null) { Say("No encuentro el mundo."); return; }

            foreach (var islander in registry.All)
            {
                if (!world.TryGetIslander(islander.Id, out var body)) continue;

                var at = body.position;
                TeleportTo(at + (transform.position - at).normalized * 1.6f);
                Say($"Al lado de {islander.Identity.ShortName}.");
                return;
            }

            Say("Ningún vecino tiene cuerpo todavía.");
        }

        // ── dar ──────────────────────────────────────────────────────────────

        private void GiveTools()
        {
            if (!ServiceRegistry.TryGet<IInventoryService>(out var inventory))
            {
                Say("No hay mochila.");
                return;
            }

            var given = new StringBuilder();
            foreach (var tool in new[] { "tool_hacha", "tool_pico", "tool_azada", "tool_regadera" })
            {
                if (inventory.CountOf(tool) > 0) continue;
                if (inventory.TryStore(tool, 1, out _) == StoreResult.Full) continue;
                given.Append(given.Length > 0 ? ", " : "").Append(tool.Replace("tool_", ""));
            }

            Say(given.Length > 0 ? $"Dentro: {given}." : "Ya las llevabas todas.");
        }

        private void FillVigor()
        {
            if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player))
            {
                Say("No hay protagonista.");
                return;
            }

            player.RestoreVigor(Data.Player.PlayerState.MaxVigor);
            _body?.SetEmotion(Data.Islanders.Emotion.Happy);
            Say("Vigor al máximo.");
        }

        private void SkipDay()
        {
            if (!ServiceRegistry.TryGet<Core.Time.GameClock>(out var clock))
            {
                Say("No hay reloj.");
                return;
            }

            // Por el reloj y no saltando el día a pelo, por lo mismo que al dormir: así
            // el huerto, los nodos y las agendas se enteran de cada hora que pasa.
            int toMorning = (24 - clock.Hour + 8) % 24 * 60 - clock.Minute;
            if (toMorning <= 0) toMorning += 24 * 60;

            clock.Advance(toMorning);
            Say($"Día {clock.Day}, {clock.Hour:00}:{clock.Minute:00}.");
        }

        private void Say(string text)
        {
            _message = text;
            _messageUntil = Time.unscaledTime + 4f;
            Debug.Log($"[pruebas] {text}");
        }
    }
}
