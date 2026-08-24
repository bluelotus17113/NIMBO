using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El mapa: las dos islas, dónde estás tú y dónde anda cada vecino.
    /// </summary>
    /// <remarks>
    /// Se dibuja a escala real, con el archipiélago entero metido en un cuadrado. No es
    /// un mapa bonito con dibujos: son dos círculos, una línea para el puente y un
    /// punto por persona. Lo que uno necesita de un mapa aquí es saber a qué lado está
    /// la gente antes de cruzar, y para eso los puntos bastan.
    ///
    /// Se refresca mientras está abierto porque los vecinos andan: un mapa que enseña
    /// dónde estaban al abrirlo miente a los diez segundos.
    /// </remarks>
    public sealed class MapPanel
    {
        private const float Size = 460f;

        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly VisualElement _canvas;
        private readonly Label _where;

        private IIslanderRegistry _registry;

        public MapPanel()
        {
            Root = UiTheme.Card("mapa");
            Root.style.display = DisplayStyle.None;
            Root.style.width = Size + 32f;

            Root.Add(UiTheme.Header("Mapa", Hide));

            _where = UiTheme.Body("", soft: true);
            _where.style.marginBottom = 8;
            Root.Add(_where);

            _canvas = new VisualElement();
            _canvas.style.width = _canvas.style.height = Size;
            _canvas.style.backgroundColor = UiTheme.Sky;
            UiTheme.SetRadius(_canvas, UiTheme.RadiusCard);
            _canvas.style.overflow = Overflow.Hidden;
            Root.Add(_canvas);
        }

        public void Show()
        {
            ServiceRegistry.TryGet(out _registry);
            Root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Cuánto mide el mundo de lado a lado, para meterlo entero en el cuadrado.
        /// </summary>
        private static float WorldSpan =>
            Mathf.Abs(Archipelago.HomeCentre.z) + Archipelago.HomeRadius
            + Archipelago.VillageRadius + 30f;

        private Vector2 ToCanvas(Vector3 world)
        {
            float span = WorldSpan;
            float scale = Size / span;

            // El centro del cuadrado cae a medio camino entre las dos islas, no en el
            // origen: con el origen centrado, tu isla se salía por abajo.
            float midZ = (Archipelago.VillageCentre.z + Archipelago.HomeCentre.z) * 0.5f;

            return new Vector2(
                Size * 0.5f + world.x * scale,
                Size * 0.5f + (world.z - midZ) * scale);
        }

        public void Refresh()
        {
            _canvas.Clear();

            float scale = Size / WorldSpan;

            DrawIsland(Archipelago.VillageCentre, Archipelago.VillageRadius, scale,
                       new Color32(0x9E, 0xD4, 0x7A, 255));
            DrawIsland(Archipelago.HomeCentre, Archipelago.HomeRadius, scale,
                       new Color32(0xBF, 0xE0, 0x96, 255));
            DrawBridge(scale);

            // Los nodos antes que los vecinos y que tú: son el fondo del mapa, no la
            // información principal, y dibujados encima taparían a la gente.
            DrawReadyNodes();

            if (_registry != null)
                foreach (var islander in _registry.All)
                    DrawIslander(islander);

            DrawPlayer();
        }

        /// <summary>
        /// Lo que está listo para recoger, con Recolección 10.
        /// </summary>
        /// <remarks>
        /// Es el último desbloqueo de la vía y el más difícil de justificar hasta que se
        /// juega: con ciento veinte nodos repartidos y una semana de reposiciones, saber
        /// **dónde queda algo** es lo que convierte salir a por madera en una ruta en
        /// vez de un paseo dando vueltas.
        ///
        /// Puntos pequeños y sin nombre: son de dónde ir, no qué mirar.
        /// </remarks>
        private void DrawReadyNodes()
        {
            if (!Gates.Allows(Unlock.NodesOnMap)) return;
            if (!ServiceRegistry.TryGet<IGatheringService>(out var gathering)) return;

            var nodes = gathering.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.IsDepleted) continue;

                Dot(new Vector3(node.X, node.Y, node.Z), UiTheme.Sage, 5f, "");
            }
        }

        private void DrawIsland(Vector3 centre, float radius, float scale, Color colour)
        {
            float diameter = radius * 2f * scale;
            var point = ToCanvas(centre);

            var disc = new VisualElement();
            disc.style.position = Position.Absolute;
            disc.style.width = disc.style.height = diameter;
            disc.style.left = point.x - diameter * 0.5f;
            disc.style.top = point.y - diameter * 0.5f;
            disc.style.backgroundColor = colour;
            UiTheme.SetRadius(disc, diameter * 0.5f);
            _canvas.Add(disc);
        }

        private void DrawBridge(float scale)
        {
            var from = ToCanvas(Archipelago.BridgeFromVillage);
            var to = ToCanvas(Archipelago.BridgeToHome);

            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.width = Mathf.Max(3f, Archipelago.BridgeWidth * scale);
            line.style.height = Mathf.Abs(to.y - from.y);
            line.style.left = from.x - line.style.width.value.value * 0.5f;
            line.style.top = Mathf.Min(from.y, to.y);
            line.style.backgroundColor = (Color)new Color32(0xA9, 0x86, 0x63, 255);
            _canvas.Add(line);
        }

        private void DrawIslander(Data.Islanders.IslanderData islander)
        {
            // Los vecinos viven en la aldea, así que su punto sale de la zona en la que
            // están. Es aproximado —dentro de la zona andan— y para un mapa vale: lo
            // que se quiere saber es «está en la tienda», no en qué baldosa.
            if (!ServiceRegistry.TryGet<IIslandService>(out var island)) return;
            if (!island.TryGetSpawnPoint(islander.CurrentZoneId, out var position)) return;

            Dot(position, UiTheme.PeachDeep, 9f, islander.Identity.ShortName);
        }

        private void DrawPlayer()
        {
            if (!ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var player)) return;

            Dot(player.Position, UiTheme.Ink, 13f, "Tú");

            string side = Archipelago.OnBridge(player.Position)
                ? "cruzando el puente"
                : Archipelago.SideOf(player.Position) == IslandSide.Home
                    ? "en tu isla" : "en la aldea";
            _where.text = $"Estás {side}.";
        }

        private void Dot(Vector3 world, Color colour, float size, string tooltip)
        {
            var point = ToCanvas(world);

            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.width = dot.style.height = size;
            dot.style.left = point.x - size * 0.5f;
            dot.style.top = point.y - size * 0.5f;
            dot.style.backgroundColor = colour;
            UiTheme.SetRadius(dot, size * 0.5f);
            dot.tooltip = tooltip;
            _canvas.Add(dot);
        }
    }
}
