using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El menú del modo construcción: qué edificio estás colocando.
    /// </summary>
    /// <remarks>
    /// Se queda pegado a un lado y estrecho a propósito: mientras construyes, lo que
    /// hay que ver es la isla, no el menú. Solo lista los edificios abiertos — ofrecer
    /// los que no lo están sería un catálogo de cosas que el clic va a rechazar.
    /// </remarks>
    public sealed class BuildPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        /// <summary>Lo que hay elegido ahora. Vacío para no colocar nada.</summary>
        public string Selected { get; private set; } = "";

        private readonly ScrollView _list;
        private readonly Label _hint;

        private IBuildService _build;
        private IIslandService _island;

        public BuildPanel()
        {
            Root = UiTheme.Card("construir");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 260;
            Root.style.maxHeight = Length.Percent(88);

            Root.Add(UiTheme.Title("Construir"));
            Root.Add(UiTheme.Body("Elige un edificio y pincha en la rejilla. " +
                                  "Botón derecho arrastrando mueve la vista.", soft: true));

            _list = new ScrollView();
            UiTheme.StyleScroll(_list);
            _list.style.flexGrow = 1;
            _list.style.marginTop = 8;
            Root.Add(_list);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 6;
            _hint.style.minHeight = 18;
            Root.Add(_hint);

            var done = UiTheme.Action("Terminar", () => EventBus.Publish(new BuildModeChanged(false)));
            done.style.marginTop = 8;
            Root.Add(done);
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _build)) return;
            ServiceRegistry.TryGet(out _island);

            Selected = "";
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            Selected = "";
        }

        private void Rebuild()
        {
            _list.Clear();
            if (_build == null) return;

            int shown = 0;
            foreach (var zoneId in _build.Movable)
            {
                if (_island != null && !_island.IsUnlocked(zoneId)) continue;
                _list.Add(BuildRow(zoneId, shown++ % 2 == 1));
            }

            if (shown == 0)
                _list.Add(UiTheme.Body("Todavía no has abierto ningún edificio.", soft: true));
        }

        private VisualElement BuildRow(string zoneId, bool alternate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 10;
            row.style.paddingTop = row.style.paddingBottom = 8;
            row.style.marginBottom = 4;
            if (alternate) row.style.backgroundColor = UiTheme.CreamDeep;
            if (zoneId == Selected) row.style.backgroundColor = UiTheme.Peach;
            UiTheme.SetRadius(row, UiTheme.Radius);

            var name = UiTheme.Body(Pretty(zoneId));
            name.style.flexGrow = 1;
            row.Add(name);

            if (_build.TryGetPlacement(zoneId, out _))
            {
                var pill = UiTheme.Pill("movido", UiTheme.Mint);
                pill.style.marginRight = 0;
                row.Add(pill);
            }

            row.RegisterCallback<ClickEvent>(_ =>
            {
                // Volver a pulsar el mismo lo suelta: sin eso no hay forma de dejar de
                // colocar salvo saliendo del modo entero.
                Selected = Selected == zoneId ? "" : zoneId;
                _hint.text = string.IsNullOrEmpty(Selected)
                    ? "" : $"Colocando {Pretty(zoneId)}.";
                Rebuild();
            });

            return row;
        }

        /// <summary>«zona_tienda_comida» no es un nombre. Esto lo deja legible.</summary>
        private static string Pretty(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return "";
            string name = zoneId.StartsWith("zona_") ? zoneId[5..] : zoneId;
            name = name.Replace('_', ' ');
            return char.ToUpperInvariant(name[0]) + name[1..];
        }
    }
}
