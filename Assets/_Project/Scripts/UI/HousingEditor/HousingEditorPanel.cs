using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Housing;
using Nimbo.Data.Islanders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.HousingEditor
{
    /// <summary>
    /// El editor de interiores: el jugador arrastra muebles del inventario a la
    /// rejilla de un apartamento. La cámara 3D dibuja la rejilla; este panel pone
    /// los controles, el catálogo y la lista de lo colocado.
    /// </summary>
    public sealed class HousingEditorPanel : System.IDisposable
    {
        private readonly Label _title;
        private readonly VisualElement _catalogList;
        private readonly VisualElement _placedList;
        private readonly VisualElement _controls;
        private readonly Label _feedback;
        private readonly Label _selectedLabel;

        private string _islanderId;
        private RoomLayout _room;
        private string _selectedCatalogId;
        private Facing _selectedFacing = Facing.South;

        // Controles de coordenadas — se recrean al refrescar porque el tamaño
        // de la rejilla puede cambiar.
        private IntegerField _fieldX;
        private IntegerField _fieldY;
        private Button _btnNorth, _btnEast, _btnSouth, _btnWest;
        private Button _btnPlace;

        public VisualElement Root { get; }

        public HousingEditorPanel()
        {
            Root = new VisualElement { name = "editor-casa" };
            Root.style.width = 420;
            Root.style.display = DisplayStyle.None;

            // ── Cabecera ──────────────────────────────────────────────
            var headerCard = UiTheme.Card();
            _title = UiTheme.Title("Casa");
            headerCard.Add(_title);
            Root.Add(headerCard);

            // ── Objeto seleccionado ───────────────────────────────────
            var selCard = UiTheme.Card();
            _selectedLabel = UiTheme.Body("Elige un mueble del inventario.", soft: true);
            selCard.Add(_selectedLabel);
            Root.Add(selCard);

            // ── Catálogo ──────────────────────────────────────────────
            var catCard = UiTheme.Card();
            catCard.Add(UiTheme.Title("Tu inventario"));
            _catalogList = new VisualElement();
            catCard.Add(_catalogList);
            Root.Add(catCard);

            // ── Lo colocado ───────────────────────────────────────────
            var placedCard = UiTheme.Card();
            placedCard.Add(UiTheme.Title("Colocado"));
            _placedList = new VisualElement();
            placedCard.Add(_placedList);
            Root.Add(placedCard);

            // ── Controles ─────────────────────────────────────────────
            var ctrlCard = UiTheme.Card();
            ctrlCard.Add(UiTheme.Title("Colocar"));

            _controls = new VisualElement();
            ctrlCard.Add(_controls);
            Root.Add(ctrlCard);

            // ── Aviso ─────────────────────────────────────────────────
            _feedback = UiTheme.Body("", soft: true);
            _feedback.style.marginTop = 6;
            Root.Add(_feedback);

            // ── Cerrar ────────────────────────────────────────────────
            Root.Add(UiTheme.Secondary("Cerrar", Hide));
        }

        public void Dispose()
        {
            // nada que desuscribir de momento
        }

        public void Show(string islanderId)
        {
            _islanderId = islanderId;
            _selectedCatalogId = null;
            _selectedFacing = Facing.South;
            Root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            _islanderId = null;
            _room = null;
            Root.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            if (_islanderId == null) return;

            // ── La habitación ─────────────────────────────────────────
            if (!ServiceRegistry.TryGet<IHousingService>(out var housing)) return;
            _room = housing.GetHomeOf(_islanderId);
            if (_room == null)
            {
                _title.text = "Sin casa";
                _catalogList.Clear();
                _placedList.Clear();
                _controls.Clear();
                _feedback.text = "Este isleño aún no tiene casa asignada.";
                return;
            }

            // ── Nombre del habitante ──────────────────────────────────
            string name = _islanderId;
            if (ServiceRegistry.TryGet<IIslanderRegistry>(out var registry) &&
                registry.TryGet(_islanderId, out var islander))
            {
                name = islander.Identity.DisplayName;
            }
            _title.text = $"Casa de {name}";

            BuildCatalog();
            BuildPlaced();
            BuildControls();
            UpdateSelectedLabel();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Catálogo: lo que el jugador tiene en el inventario
        // ═══════════════════════════════════════════════════════════════

        private void BuildCatalog()
        {
            _catalogList.Clear();
            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy)) return;

            var inventory = economy.Inventory;
            if (inventory.Stacks.Count == 0)
            {
                _catalogList.Add(UiTheme.Body("No tienes nada todavía. Pásate por la tienda.", soft: true));
                return;
            }

            // Agrupar por categoría las cosas que se pueden colocar
            var placeable = new System.Collections.Generic.Dictionary<ItemCategory, System.Collections.Generic.List<IItemDefinition>>();
            foreach (var stack in inventory.Stacks)
            {
                var def = economy.GetItem(stack.CatalogId);
                if (def == null) continue;
                if (!IsPlaceable(def.Category)) continue;
                if (!placeable.ContainsKey(def.Category)) placeable[def.Category] = new System.Collections.Generic.List<IItemDefinition>();
                placeable[def.Category].Add(def);
            }

            if (placeable.Count == 0)
            {
                _catalogList.Add(UiTheme.Body("Aquí no hay nada que colocar. Compra muebles en la tienda.", soft: true));
                return;
            }

            bool alternate = false;
            foreach (var kv in placeable)
            {
                var catLabel = UiTheme.Body(CategoryName(kv.Key));
                catLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                catLabel.style.marginTop = 8;
                _catalogList.Add(catLabel);

                foreach (var def in kv.Value)
                {
                    var row = UiTheme.Row(alternate);
                    alternate = !alternate;
                    row.style.justifyContent = Justify.SpaceBetween;

                    var left = new VisualElement();
                    left.style.flexDirection = FlexDirection.Row;
                    left.style.alignItems = Align.Center;

                    var name = UiTheme.Body(def.DisplayName);
                    name.style.marginRight = 8;
                    left.Add(name);
                    left.Add(UiTheme.Pill($"×{inventory.CountOf(def.CatalogId)}", InkFaintForPill));

                    row.Add(left);

                    bool isSelected = def.CatalogId == _selectedCatalogId;
                    if (isSelected)
                    {
                        row.Add(UiTheme.Pill("Seleccionado", UiTheme.Mint));
                    }
                    else
                    {
                        var id = def.CatalogId; // capturar para el lambda
                        row.Add(UiTheme.Secondary("Elegir", () => SelectCatalogItem(id)));
                    }
                    _catalogList.Add(row);
                }
            }
        }

        private static Color InkFaintForPill => UiTheme.InkFaint;

        private static bool IsPlaceable(ItemCategory category) => category switch
        {
            ItemCategory.Furniture => true,
            ItemCategory.Decoration => true,
            ItemCategory.Wallpaper => true,
            ItemCategory.Flooring => true,
            _ => false,
        };

        private static string CategoryName(ItemCategory category) => category switch
        {
            ItemCategory.Furniture => "Muebles",
            ItemCategory.Decoration => "Decoración",
            ItemCategory.Wallpaper => "Paredes",
            ItemCategory.Flooring => "Suelos",
            _ => "Otros",
        };

        private void SelectCatalogItem(string catalogId)
        {
            _selectedCatalogId = catalogId;
            BuildCatalog();
            UpdateSelectedLabel();
        }

        private void UpdateSelectedLabel()
        {
            if (_selectedCatalogId == null)
            {
                _selectedLabel.text = "Elige un mueble del inventario.";
                _selectedLabel.style.color = UiTheme.InkSoft;
                return;
            }

            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
            {
                var def = economy.GetItem(_selectedCatalogId);
                if (def != null)
                {
                    _selectedLabel.text = $"Vas a colocar: {def.DisplayName}";
                    _selectedLabel.style.color = UiTheme.Ink;
                    return;
                }
            }
            _selectedLabel.text = $"Vas a colocar: {_selectedCatalogId}";
            _selectedLabel.style.color = UiTheme.Ink;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Lo colocado
        // ═══════════════════════════════════════════════════════════════

        private void BuildPlaced()
        {
            _placedList.Clear();
            if (_room == null) return;

            if (_room.Objects.Count == 0)
            {
                _placedList.Add(UiTheme.Body("Aquí no hay nada todavía.", soft: true));
                return;
            }

            if (!ServiceRegistry.TryGet<IEconomyService>(out var economy))
            {
                // Sin economía mostramos los catalogId en crudo
                for (int i = 0; i < _room.Objects.Count; i++)
                {
                    var obj = _room.Objects[i];
                    var row = UiTheme.Row(i % 2 != 0);
                    row.style.justifyContent = Justify.SpaceBetween;

                    var info = UiTheme.Body($"{obj.CatalogId}  ({obj.Origin.X}, {obj.Origin.Y})  {FacingName(obj.Facing)}");
                    row.Add(info);

                    var instanceId = obj.InstanceId;
                    row.Add(UiTheme.Secondary("Quitar", () => RemoveObject(instanceId)));
                    _placedList.Add(row);
                }
                return;
            }

            for (int i = 0; i < _room.Objects.Count; i++)
            {
                var obj = _room.Objects[i];
                var row = UiTheme.Row(i % 2 != 0);
                row.style.justifyContent = Justify.SpaceBetween;

                var def = economy.GetItem(obj.CatalogId);
                string display = def != null ? def.DisplayName : obj.CatalogId;
                var info = UiTheme.Body($"{display}  ({obj.Origin.X}, {obj.Origin.Y})  {FacingName(obj.Facing)}");
                row.Add(info);

                var instanceId = obj.InstanceId;
                row.Add(UiTheme.Secondary("Quitar", () => RemoveObject(instanceId)));
                _placedList.Add(row);
            }
        }

        private void RemoveObject(string instanceId)
        {
            if (_room == null) return;
            if (!ServiceRegistry.TryGet<IHousingService>(out var housing)) return;
            housing.Remove(_room, instanceId);
            Refresh();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Controles de colocación
        // ═══════════════════════════════════════════════════════════════

        private void BuildControls()
        {
            _controls.Clear();
            if (_room == null) return;

            // ── Coordenadas ───────────────────────────────────────────
            var coordsRow = new VisualElement();
            coordsRow.style.flexDirection = FlexDirection.Row;
            coordsRow.style.alignItems = Align.Center;
            coordsRow.style.marginBottom = 10;

            coordsRow.Add(UiTheme.Body("X:", soft: true));
            _fieldX = new IntegerField { value = 0, style = { width = 60, marginRight = 12 } };
            coordsRow.Add(_fieldX);

            coordsRow.Add(UiTheme.Body("Y:", soft: true));
            _fieldY = new IntegerField { value = 0, style = { width = 60 } };
            coordsRow.Add(_fieldY);

            _controls.Add(coordsRow);

            // ── Orientación ───────────────────────────────────────────
            var facingRow = new VisualElement();
            facingRow.style.flexDirection = FlexDirection.Row;
            facingRow.style.alignItems = Align.Center;
            facingRow.style.marginBottom = 10;

            facingRow.Add(UiTheme.Body("Mira hacia:", soft: true));
            _btnNorth = BuildFacingButton("Norte", Facing.North);
            _btnEast  = BuildFacingButton("Este",  Facing.East);
            _btnSouth = BuildFacingButton("Sur",   Facing.South);
            _btnWest  = BuildFacingButton("Oeste", Facing.West);

            facingRow.Add(_btnNorth);
            facingRow.Add(_btnEast);
            facingRow.Add(_btnSouth);
            facingRow.Add(_btnWest);

            _controls.Add(facingRow);
            UpdateFacingButtons();

            // ── Colocar ───────────────────────────────────────────────
            _btnPlace = UiTheme.Action("Colocar", DoPlace);
            _controls.Add(_btnPlace);
        }

        private Button BuildFacingButton(string label, Facing facing)
        {
            var btn = UiTheme.Secondary(label, () =>
            {
                _selectedFacing = facing;
                UpdateFacingButtons();
            });
            btn.style.marginRight = 4;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            return btn;
        }

        private void UpdateFacingButtons()
        {
            if (_btnNorth == null) return;
            _btnNorth.style.backgroundColor  = _selectedFacing == Facing.North  ? UiTheme.Peach : UiTheme.CreamDeep;
            _btnEast.style.backgroundColor   = _selectedFacing == Facing.East   ? UiTheme.Peach : UiTheme.CreamDeep;
            _btnSouth.style.backgroundColor  = _selectedFacing == Facing.South  ? UiTheme.Peach : UiTheme.CreamDeep;
            _btnWest.style.backgroundColor   = _selectedFacing == Facing.West   ? UiTheme.Peach : UiTheme.CreamDeep;
        }

        private void DoPlace()
        {
            _feedback.text = "";

            if (_selectedCatalogId == null)
            {
                _feedback.text = "Elige primero qué quieres colocar.";
                _feedback.style.color = UiTheme.InkSoft;
                return;
            }

            if (_room == null) return;
            if (!ServiceRegistry.TryGet<IHousingService>(out var housing)) return;

            int x = _fieldX?.value ?? 0;
            int y = _fieldY?.value ?? 0;
            var origin = new GridCoord(x, y);

            var error = housing.CanPlace(_room, _selectedCatalogId, origin, _selectedFacing);
            if (error != PlacementError.None)
            {
                _feedback.text = PlacementErrorMessage(error);
                _feedback.style.color = UiTheme.Rose;
                return;
            }

            var result = housing.Place(_room, _selectedCatalogId, origin, _selectedFacing);
            if (result != PlacementError.None)
            {
                _feedback.text = PlacementErrorMessage(result);
                _feedback.style.color = UiTheme.Rose;
                return;
            }

            _feedback.text = "¡Colocado!";
            _feedback.style.color = UiTheme.Mint;
            Refresh();
        }

        private static string PlacementErrorMessage(PlacementError error) => error switch
        {
            PlacementError.Occupied        => "Ahí ya hay algo.",
            PlacementError.NeedsWall       => "Esto va en la pared.",
            PlacementError.NeedsSurface    => "Esto va encima de un mueble.",
            PlacementError.OutOfBounds     => "Eso queda fuera.",
            PlacementError.NotOwned        => "No lo tienes.",
            PlacementError.UnknownCatalogId => "Ese mueble no existe.",
            PlacementError.NeedsFloor      => "Esto va en el suelo.",
            _ => "",
        };

        private static string FacingName(Facing f) => f switch
        {
            Facing.North => "Norte",
            Facing.East  => "Este",
            Facing.South => "Sur",
            Facing.West  => "Oeste",
            _ => "?",
        };
    }
}
