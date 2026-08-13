using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Decor
{
    /// <summary>
    /// Decorar la isla: elegir una zona, elegir un adorno y ponerlo donde quieras.
    /// </summary>
    /// <remarks>
    /// La colocación se hace sobre un plano de la zona dibujado en la propia
    /// interfaz —un círculo con las piezas como fichas— y no pinchando en el mundo
    /// en 3D. Es a propósito: pinchar en 3D con una cámara que orbita obliga a
    /// convertir de pantalla a suelo, a poner colisionadores en todo y a pelearse
    /// con la perspectiva para dejar una pieza exactamente donde uno quiere. El
    /// plano cenital enseña de un vistazo lo que hay puesto, lo que cabe y dónde
    /// queda hueco, que es justo lo que uno necesita saber al decorar.
    ///
    /// El plano trabaja en las mismas coordenadas que el servicio: el radio del
    /// círculo son los <c>DecorService.ZoneRadius</c> metros de verdad.
    /// </remarks>
    public sealed class DecorPanel
    {
        /// <summary>Debe coincidir con <c>DecorService.ZoneRadius</c>.</summary>
        private const float ZoneRadius = 12f;

        /// <summary>Por debajo de esto el plano no se puede usar: las fichas se tocan.</summary>
        private const float MinMapSize = 240f;

        /// <summary>
        /// El lado del plano, en píxeles. Ya no es una constante.
        /// </summary>
        /// <remarks>
        /// Era 300 fijos, y con el panel ocupando media pantalla dejaba el plano del
        /// tamaño de un posavasos en una esquina. Ahora lo pone el hueco que quede:
        /// el cuadrado más grande que entre. Toda la conversión entre metros de la
        /// zona y píxeles pasa por aquí, así que el plano puede medir lo que sea
        /// mientras las fichas se coloquen con este mismo número.
        /// </remarks>
        private float _mapSize = 300f;

        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly VisualElement _catalogList;
        private readonly VisualElement _zoneRow;
        private readonly VisualElement _map;
        private readonly VisualElement _mapHolder;
        private readonly Label _hint;
        private readonly Label _charm;

        private IDecorService _decor;
        private IIslandService _island;
        private IEconomyService _economy;

        private string _zoneId;
        private string _selectedCatalogId;
        private string _selectedPlacementId;
        private DecorKind? _filter;

        public DecorPanel()
        {
            Root = UiTheme.Card("adornos");
            Root.style.display = DisplayStyle.None;

            // Un modo se merece la pantalla. El resto se deja a propósito: decorando
            // se sigue viendo la isla por la izquierda, que es lo que estás decorando.
            Root.style.width = Length.Percent(62);
            Root.style.height = Length.Percent(90);

            Root.Add(UiTheme.Header("Decorar la isla", Hide));

            _zoneRow = new VisualElement();
            _zoneRow.style.flexDirection = FlexDirection.Row;
            _zoneRow.style.flexWrap = Wrap.Wrap;
            _zoneRow.style.marginBottom = 10;
            Root.Add(_zoneRow);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.minHeight = 0;

            var left = new VisualElement();
            left.style.width = Length.Percent(46);
            left.style.minWidth = 320;
            left.style.marginRight = UiTheme.SpaceL;

            left.Add(BuildFilters());

            var catalogScroll = new ScrollView();
            catalogScroll.style.flexGrow = 1;
            UiTheme.StyleScroll(catalogScroll);
            _catalogList = catalogScroll;
            left.Add(_catalogList);
            body.Add(left);

            var right = new VisualElement();
            right.style.flexGrow = 1;
            right.style.minWidth = 0;

            _mapHolder = new VisualElement();
            _mapHolder.style.flexGrow = 1;
            _mapHolder.style.alignItems = Align.Center;
            _mapHolder.style.justifyContent = Justify.Center;

            _map = BuildMap();
            _mapHolder.Add(_map);
            right.Add(_mapHolder);

            // El plano es redondo, así que tiene que ser cuadrado: se lleva el lado
            // mayor que quepa en el hueco, y al cambiar de tamaño se repintan las
            // fichas, que van en píxeles y no en tantos por ciento.
            _mapHolder.RegisterCallback<GeometryChangedEvent>(OnMapResized);

            _charm = UiTheme.Body("", soft: true);
            _charm.style.marginTop = UiTheme.SpaceS;
            _charm.style.unityTextAlign = TextAnchor.MiddleCenter;
            right.Add(_charm);
            body.Add(right);

            Root.Add(body);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 10;
            _hint.style.minHeight = 34;
            Root.Add(_hint);

            // Un botón fijo que se enseña y se esconde, en vez de crear uno nuevo
            // con cada selección: crearlos obligaba a barrer los viejos a mano
            // buscando botones por el árbol, y eso se rompe en cuanto el panel
            // tenga otro botón suelto.
            _remove = UiTheme.Secondary("Quitar", RemoveSelected);
            _remove.style.display = DisplayStyle.None;
            _remove.style.alignSelf = Align.FlexStart;
            Root.Add(_remove);
        }

        private readonly Button _remove;

        private void RemoveSelected()
        {
            if (_decor == null || string.IsNullOrEmpty(_selectedPlacementId)) return;
            if (!_decor.Remove(_selectedPlacementId)) return;

            _selectedPlacementId = null;
            RebuildMap();
            Say("Quitado. No devuelve nimbos: los adornos no se revenden.");
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _decor)) return;
            ServiceRegistry.TryGet(out _island);
            ServiceRegistry.TryGet(out _economy);

            Root.style.display = DisplayStyle.Flex;

            RebuildZones();
            RebuildCatalog();
            RebuildMap();
            Say("Elige una zona, luego un adorno, y pincha en el plano para ponerlo.");
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            _selectedPlacementId = null;
        }

        // ── Zonas ────────────────────────────────────────────────────────────

        private void RebuildZones()
        {
            _zoneRow.Clear();
            if (_island == null) return;

            foreach (var zoneId in _island.ZoneIds)
            {
                // Solo las abiertas: el servicio rechazaría las cerradas de todos
                // modos, y ofrecer un botón que siempre dice que no es una trampa.
                if (!_island.IsUnlocked(zoneId)) continue;

                string id = zoneId;
                var button = UiTheme.Secondary(PrettyZone(zoneId), () =>
                {
                    _zoneId = id;
                    _selectedPlacementId = null;
                    RebuildZones();
                    RebuildMap();
                });

                button.style.marginRight = 6;
                button.style.marginBottom = 6;
                button.style.paddingTop = button.style.paddingBottom = 5;
                button.style.fontSize = 13;
                if (zoneId == _zoneId) button.style.backgroundColor = UiTheme.Peach;

                _zoneRow.Add(button);
            }

            if (string.IsNullOrEmpty(_zoneId) && _zoneRow.childCount > 0)
            {
                foreach (var zoneId in _island.ZoneIds)
                    if (_island.IsUnlocked(zoneId)) { _zoneId = zoneId; break; }
                RebuildZones();
            }
        }

        /// <summary>«zona_plaza» no es un nombre. Esto lo convierte en «Plaza».</summary>
        private static string PrettyZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return "";
            string name = zoneId.StartsWith("zona_") ? zoneId[5..] : zoneId;
            name = name.Replace('_', ' ');
            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        // ── Catálogo ─────────────────────────────────────────────────────────

        private VisualElement BuildFilters()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 8;

            Add("Todo", null);
            foreach (DecorKind kind in System.Enum.GetValues(typeof(DecorKind)))
                Add(KindName(kind), kind);

            return row;

            void Add(string text, DecorKind? kind)
            {
                var chip = UiTheme.Secondary(text, () => { _filter = kind; RebuildCatalog(); });
                chip.style.fontSize = 12;
                chip.style.paddingTop = chip.style.paddingBottom = 3;
                chip.style.paddingLeft = chip.style.paddingRight = 10;
                chip.style.marginRight = 4;
                chip.style.marginBottom = 4;
                row.Add(chip);
            }
        }

        private static string KindName(DecorKind kind) => kind switch
        {
            DecorKind.Seat => "Asientos",
            DecorKind.Light => "Luces",
            DecorKind.Statue => "Estatuas",
            DecorKind.Plant => "Plantas",
            DecorKind.Sign => "Carteles",
            DecorKind.Fence => "Vallas",
            _ => "Agua",
        };

        private void RebuildCatalog()
        {
            _catalogList.Clear();
            if (_decor == null) return;

            int islandLevel = _island?.State?.Level ?? 1;
            int shown = 0;

            var catalog = _decor.Catalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                if (_filter.HasValue && item.Kind != _filter.Value) continue;

                _catalogList.Add(BuildCatalogRow(item, islandLevel, shown++ % 2 == 1));
            }

            if (shown == 0)
                _catalogList.Add(UiTheme.Body("Nada de esto todavía.", soft: true));
        }

        private VisualElement BuildCatalogRow(DecorDefinition item, int islandLevel, bool alternate)
        {
            var row = UiTheme.Row(alternate);
            row.style.justifyContent = Justify.SpaceBetween;
            UiTheme.SetRadius(row, UiTheme.Radius);
            if (item.CatalogId == _selectedCatalogId) row.style.backgroundColor = UiTheme.SkySoft;

            var text = new VisualElement();
            text.style.flexGrow = 1;
            text.style.marginRight = 8;

            var name = UiTheme.Body(item.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.Add(name);
            text.Add(UiTheme.Body(item.Description, soft: true));
            row.Add(text);

            bool locked = item.UnlockLevel > islandLevel;
            var tag = UiTheme.Pill(locked ? $"Nivel {item.UnlockLevel}" : $"{item.Price}",
                                   locked ? UiTheme.InkFaint : UiTheme.Butter);
            tag.style.marginRight = 0;
            row.Add(tag);

            if (!locked)
            {
                string id = item.CatalogId;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _selectedCatalogId = id;
                    _selectedPlacementId = null;
                    RebuildCatalog();
                    Say($"«{item.DisplayName}» elegido. Pincha en el plano para ponerlo.");
                });
            }

            return row;
        }

        // ── El plano de la zona ──────────────────────────────────────────────

        private VisualElement BuildMap()
        {
            var map = new VisualElement();
            map.style.width = map.style.height = _mapSize;
            map.style.backgroundColor = UiTheme.Sage;
            map.style.flexShrink = 0;
            UiTheme.SetRadius(map, _mapSize / 2f);
            map.style.overflow = Overflow.Hidden;

            map.RegisterCallback<ClickEvent>(OnMapClicked);
            return map;
        }

        /// <summary>
        /// El hueco del plano ha cambiado de tamaño: se recalcula el lado y se
        /// vuelven a colocar las fichas.
        /// </summary>
        /// <remarks>
        /// Repintar dentro de un aviso de geometría puede provocar otro aviso, así que
        /// se sale sin hacer nada si el lado no ha cambiado de verdad. Sin esa salida,
        /// el plano se repinta a sí mismo hasta que el editor se cae.
        /// </remarks>
        private void OnMapResized(GeometryChangedEvent evt)
        {
            float side = Mathf.Floor(Mathf.Min(evt.newRect.width, evt.newRect.height));
            if (side < MinMapSize) side = MinMapSize;
            if (Mathf.Abs(side - _mapSize) < 1f) return;

            _mapSize = side;
            _map.style.width = _map.style.height = side;
            UiTheme.SetRadius(_map, side / 2f);
            RebuildMap();
        }

        private void RebuildMap()
        {
            _map.Clear();
            if (_decor == null || string.IsNullOrEmpty(_zoneId)) return;

            int count = 0;
            foreach (var placement in _decor.InZone(_zoneId))
            {
                if (!_decor.TryGetDefinition(placement.CatalogId, out var definition)) continue;
                _map.Add(BuildToken(placement.PlacementId, definition,
                                    placement.X, placement.Z));
                count++;
            }

            float charm = _decor.CharmOf(_zoneId);
            _charm.text = $"{count} de {_decor.CapacityPerZone} adornos · encanto {charm:0.00}";
        }

        private VisualElement BuildToken(string placementId, DecorDefinition definition,
                                         float x, float z)
        {
            // El radio de la pieza se dibuja a escala real: si dos fichas se tocan
            // en el plano, el servicio va a rechazar la segunda. Ver el motivo es
            // mejor que leer un «ahí no cabe».
            float scale = _mapSize / (ZoneRadius * 2f);
            float diameter = Mathf.Max(10f, definition.Footprint * 2f * scale);

            var token = new VisualElement();
            token.style.position = Position.Absolute;
            token.style.width = token.style.height = diameter;
            token.style.left = _mapSize / 2f + x * scale - diameter / 2f;
            token.style.top = _mapSize / 2f + z * scale - diameter / 2f;
            token.style.backgroundColor = TokenColour(definition.Kind);
            UiTheme.SetRadius(token, UiTheme.RadiusPill);

            if (placementId == _selectedPlacementId)
            {
                token.style.borderTopWidth = token.style.borderBottomWidth =
                    token.style.borderLeftWidth = token.style.borderRightWidth = 3;
                token.style.borderTopColor = token.style.borderBottomColor =
                    token.style.borderLeftColor = token.style.borderRightColor = UiTheme.Ink;
            }

            token.tooltip = definition.DisplayName;
            token.RegisterCallback<ClickEvent>(evt =>
            {
                // Sin esto el clic sigue subiendo hasta el plano y además de
                // seleccionar la ficha coloca otra encima.
                evt.StopPropagation();

                _selectedPlacementId = placementId;
                RebuildMap();
                Say($"«{definition.DisplayName}» seleccionado. Pincha en otro sitio para " +
                    "moverlo, o pulsa Quitar.");
                _remove.text = $"Quitar {definition.DisplayName}";
            });

            return token;
        }

        private static Color TokenColour(DecorKind kind) => kind switch
        {
            DecorKind.Seat => UiTheme.Peach,
            DecorKind.Light => UiTheme.Butter,
            DecorKind.Statue => UiTheme.InkFaint,
            DecorKind.Plant => UiTheme.Mint,
            DecorKind.Sign => UiTheme.PeachDeep,
            DecorKind.Fence => UiTheme.Rose,
            _ => UiTheme.Sky,
        };

        private void OnMapClicked(ClickEvent evt)
        {
            if (_decor == null || string.IsNullOrEmpty(_zoneId)) return;

            var local = evt.localPosition;
            float scale = _mapSize / (ZoneRadius * 2f);
            float x = ((float)local.x - _mapSize / 2f) / scale;
            float z = ((float)local.y - _mapSize / 2f) / scale;
            var point = new Vector3(x, 0f, z);

            // Con una ficha seleccionada, el clic la mueve. Sin ella, coloca una
            // nueva del catálogo. Un solo gesto para las dos cosas, y el estado se
            // ve: la ficha seleccionada lleva borde.
            if (!string.IsNullOrEmpty(_selectedPlacementId))
            {
                if (_decor.Move(_selectedPlacementId, point, 0f))
                {
                    RebuildMap();
                    Say("Movido.");
                }
                else Say("Ahí no cabe: chocaría con otra cosa o se sale de la zona.");
                return;
            }

            if (string.IsNullOrEmpty(_selectedCatalogId))
            {
                Say("Elige antes un adorno de la lista.");
                return;
            }

            var reason = _decor.CanPlace(_selectedCatalogId, _zoneId, point);
            if (reason != DecorRejection.Ok)
            {
                Say(Excuse(reason));
                return;
            }

            if (!_decor.TryGetDefinition(_selectedCatalogId, out var definition)) return;

            // Se cobra antes de colocar, y solo si el sitio ya valía: cobrar y luego
            // descubrir que no cabía dejaría al jugador sin adorno y sin dinero.
            if (_economy != null &&
                !_economy.TrySpend(definition.Price, $"adorno {definition.DisplayName}"))
            {
                Say($"Te faltan nimbos: «{definition.DisplayName}» cuesta {definition.Price}.");
                return;
            }

            string id = _decor.Place(_selectedCatalogId, _zoneId, point, 0f);
            if (string.IsNullOrEmpty(id))
            {
                // Rarísimo, pero si el sitio se hubiera ocupado entre la comprobación
                // y ahora, el dinero se devuelve. No se cobra por nada.
                _economy?.AddCoins(definition.Price, "adorno no colocado");
                Say("No se pudo poner. Te he devuelto los nimbos.");
                return;
            }

            RebuildMap();
            Say($"«{definition.DisplayName}» colocado por {definition.Price} nimbos.");
        }

        private static string Excuse(DecorRejection reason) => reason switch
        {
            DecorRejection.UnknownItem => "Ese adorno no existe.",
            DecorRejection.ZoneLocked => "Esa zona todavía no está abierta.",
            DecorRejection.OutsideZone => "Eso queda fuera de la zona. Más hacia el centro.",
            DecorRejection.Overlaps => "Ahí ya hay algo. Los adornos no se pisan.",
            DecorRejection.TooMany => "Esta zona ya está llena de adornos.",
            _ => "Ahí no se puede.",
        };

        private void Say(string text)
        {
            _hint.text = text;

            // El botón de quitar solo existe mientras haya algo seleccionado, y se
            // decide aquí porque aquí pasa todo lo que cambia la selección.
            _remove.style.display = string.IsNullOrEmpty(_selectedPlacementId)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
