using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Housing;
using Nimbo.Player;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El menú de amueblar: qué mueble estás colocando dentro de la casa.
    /// </summary>
    /// <remarks>
    /// Solo enseña lo que de verdad se puede colocar. El editor viejo listaba todo lo
    /// que el inventario llamaba «colocable» —papeles pintados y suelos incluidos—, y
    /// elegir uno solo servía para leer «ese mueble no existe» después de haber
    /// apuntado a una casilla: son acabados, no objetos, y no están en el catálogo de
    /// muebles.
    ///
    /// Los acabados vuelven a la lista pero aparte, y al revés que los muebles:
    /// pincharlos los aplica y los gasta en el momento, porque un bote de pintura no
    /// se «coloca» en una casilla.
    /// </remarks>
    public sealed class FurnishPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        /// <summary>Lo que hay elegido. Vacío para no colocar nada.</summary>
        public string Selected { get; private set; } = "";

        private readonly ScrollView _list;
        private readonly Label _hint;

        private IEconomyService _economy;
        private IHousingService _housing;

        public FurnishPanel()
        {
            Root = UiTheme.Card("amueblar");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 280;
            Root.style.maxHeight = Length.Percent(88);

            Root.Add(UiTheme.Title("Amueblar"));
            Root.Add(UiTheme.Body("Elige un mueble y pincha en la casilla. " +
                                  "Botón derecho lo recoge, R lo gira.", soft: true));

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            _list.style.marginTop = 8;
            Root.Add(_list);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 6;
            _hint.style.minHeight = 18;
            Root.Add(_hint);

            var done = UiTheme.Action("Terminar", () => EventBus.Publish(new FurnishModeChanged(false)));
            done.style.marginTop = 8;
            Root.Add(done);
        }

        /// <summary>
        /// Se entera de cuándo el modo suelta lo que llevabas.
        /// </summary>
        /// <remarks>
        /// Al colocar el último de una pila, quien coloca lo suelta. Sin escuchar eso,
        /// el menú seguiría enseñando esa fila marcada como elegida cuando ya no queda
        /// ninguno, que es la clase de mentira pequeña que hace dudar de todo lo demás.
        /// </remarks>
        public void Subscribe() => EventBus.Subscribe<FurnishSelectionChanged>(OnSelectionChanged);

        public void Unsubscribe() => EventBus.Unsubscribe<FurnishSelectionChanged>(OnSelectionChanged);

        private void OnSelectionChanged(FurnishSelectionChanged evt)
        {
            string incoming = evt.CatalogId ?? "";
            if (incoming == Selected) return;

            Selected = incoming;
            if (IsShowing) Rebuild();
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _economy)) return;
            ServiceRegistry.TryGet(out _housing);

            Select("");
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            Select("");
        }

        public void Rebuild()
        {
            _list.Clear();
            if (_economy == null) return;

            int shown = 0;
            List<ItemStack> finishes = null;

            foreach (var stack in _economy.Inventory.Stacks)
            {
                if (stack.Quantity <= 0) continue;

                // Los acabados van a su propia sección: no se colocan casilla a
                // casilla, así que ofrecerlos como si fueran una silla era lo que
                // acababa en «ese mueble no existe».
                if (IsFinish(stack.CatalogId))
                {
                    finishes ??= new List<ItemStack>();
                    finishes.Add(stack);
                    continue;
                }

                if (_housing != null && !_housing.IsPlaceable(stack.CatalogId)) continue;

                _list.Add(Row(stack.CatalogId, stack.Quantity, shown++ % 2 == 1));
            }

            if (finishes != null)
            {
                var header = UiTheme.Body("Acabados — pínchalos para aplicarlos", soft: true);
                header.style.marginTop = 8;
                _list.Add(header);

                foreach (var stack in finishes)
                    _list.Add(FinishRow(stack.CatalogId, stack.Quantity, shown++ % 2 == 1));
            }

            if (shown == 0)
                _list.Add(UiTheme.Body("No tienes muebles. Los venden en la tienda de muebles.",
                                       soft: true));
        }

        /// <summary>¿Es un acabado de pared o suelo y no un objeto que se coloca?</summary>
        private bool IsFinish(string catalogId)
        {
            var item = _economy.GetItem(catalogId);
            return item != null && (item.Category == ItemCategory.Wallpaper ||
                                    item.Category == ItemCategory.Flooring);
        }

        private VisualElement Row(string catalogId, int quantity, bool alternate)
        {
            var row = UiTheme.Row(alternate);
            row.style.justifyContent = Justify.SpaceBetween;
            if (catalogId == Selected) row.style.backgroundColor = UiTheme.Peach;

            var item = _economy.GetItem(catalogId);
            var name = UiTheme.Body(item != null ? item.DisplayName : catalogId);
            name.style.flexGrow = 1;
            row.Add(name);
            row.Add(UiTheme.Pill($"×{quantity}", UiTheme.Mint));

            row.RegisterCallback<ClickEvent>(_ =>
            {
                // Volver a pulsar el mismo lo suelta: si no, no hay forma de dejar de
                // colocar salvo saliendo del modo entero.
                Select(Selected == catalogId ? "" : catalogId);
                Rebuild();
            });

            return row;
        }

        private void Select(string catalogId)
        {
            if (Selected == catalogId) return;

            Selected = catalogId;
            _hint.text = string.IsNullOrEmpty(catalogId)
                ? ""
                : $"Colocando {_economy?.GetItem(catalogId)?.DisplayName ?? catalogId}.";

            EventBus.Publish(new FurnishSelectionChanged(Selected));
        }

        /// <summary>
        /// Una fila de acabado. Pincharla aplica el acabado y gasta una unidad.
        /// </summary>
        /// <remarks>
        /// No pasa por la selección ni por el fantasma del modo amueblar: no hay
        /// casilla que señalar. Si no se pudo aplicar —no llevas unidades, o ya está
        /// puesto— se dice en el rótulo en vez de quedarse callado.
        /// </remarks>
        private VisualElement FinishRow(string catalogId, int quantity, bool alternate)
        {
            var row = UiTheme.Row(alternate);
            row.style.justifyContent = Justify.SpaceBetween;

            var item = _economy.GetItem(catalogId);
            var name = UiTheme.Body(item != null ? item.DisplayName : catalogId);
            name.style.flexGrow = 1;
            row.Add(name);

            string surface = item != null && item.Category == ItemCategory.Wallpaper
                ? "pared" : "suelo";
            row.Add(UiTheme.Pill($"{surface} ×{quantity}", UiTheme.Mint));

            row.RegisterCallback<ClickEvent>(_ =>
            {
                var displayName = item != null ? item.DisplayName : catalogId;
                _hint.text = TryApplyFinish(catalogId)
                    ? $"{displayName} aplicado."
                    : $"No se pudo aplicar {displayName}: ya está puesto o no llevas ninguna unidad.";
                Rebuild();
            });

            return row;
        }

        /// <summary>
        /// Aplica un acabado de la mochila a tu cabaña, lo gasta y avisa de la reforma.
        /// </summary>
        /// <remarks>
        /// Un bote de pintura pinta la pared entera, así que elegir es aplicar: no hay
        /// fase de colocación. Gasta una unidad porque si no, comprar un papel pintado
        /// una vez decoraría gratis todas las casas que vengan después.
        ///
        /// Publica <see cref="RoomEdited"/> con clave vacía: tu cabaña no cuelga de
        /// ningún edificio (ver <c>PlayerState.Home</c>) y quien hoy escucha el aviso
        /// —los logros de decoración— no mira el identificador. La vista del interior
        /// también lo escucha, y es lo que hace que el acabado se vea sin salir de casa.
        ///
        /// Devuelve falso sin gastar nada si no llevas el acabado, no es un acabado,
        /// o ya cubre toda la superficie: repintar lo igual no puede costar pintura.
        /// </remarks>
        public bool TryApplyFinish(string catalogId)
        {
            if (_economy == null || _housing == null || string.IsNullOrEmpty(catalogId)) return false;
            if (!IsFinish(catalogId)) return false;
            if (_economy.Inventory.CountOf(catalogId) <= 0) return false;
            if (!ServiceRegistry.TryGet(out PlayerService player) || player?.State?.Home == null)
                return false;

            var home = player.State.Home;
            bool wallpaper = _economy.GetItem(catalogId).Category == ItemCategory.Wallpaper;

            if (wallpaper)
            {
                if (home.WallpaperNorth == catalogId && home.WallpaperWest == catalogId) return false;

                _housing.SetWallpaper(home, north: true, catalogId);
                _housing.SetWallpaper(home, north: false, catalogId);

                // El servicio valida contra su catálogo y rechaza en silencio los ids
                // que no conoce; si no se aplicó de verdad, no se gasta la unidad.
                if (home.WallpaperNorth != catalogId) return false;
            }
            else
            {
                bool alreadyOn = true;
                for (int y = 0; y < home.Height && alreadyOn; y++)
                    for (int x = 0; x < home.Width && alreadyOn; x++)
                        alreadyOn = home.FloorAt(new GridCoord(x, y)) == catalogId;
                if (alreadyOn) return false;

                for (int y = 0; y < home.Height; y++)
                    for (int x = 0; x < home.Width; x++)
                        _housing.SetFloor(home, new GridCoord(x, y), catalogId);

                if (home.FloorAt(new GridCoord(0, 0)) != catalogId) return false;
            }

            _economy.Inventory.Remove(catalogId, 1);
            EventBus.Publish(new RoomEdited("", 0));
            return true;
        }
    }
}
