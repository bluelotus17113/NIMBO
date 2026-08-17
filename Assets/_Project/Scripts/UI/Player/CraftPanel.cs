using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// El crafteo: qué puedes hacer con lo que llevas encima.
    /// </summary>
    /// <remarks>
    /// Se enseñan también las recetas para las que te faltan materiales, con lo que
    /// falta en rojo. Esconderlas dejaría la lista casi vacía el primer día y el
    /// jugador no sabría qué buscar por la isla: media gracia del crafteo es ver lo
    /// que podrías hacer si trajeras dos piedras más.
    /// </remarks>
    public sealed class CraftPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _hint;
        private readonly VisualElement _stationRow;

        private ICraftingService _crafting;
        private IInventoryService _inventory;
        private IEconomyService _economy;
        private IIslandService _island;

        private CraftStation _station = CraftStation.Hand;

        public CraftPanel()
        {
            Root = UiTheme.Card("crafteo");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 600;
            Root.style.maxHeight = Length.Percent(92);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Hacer cosas"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _stationRow = new VisualElement();
            _stationRow.style.flexDirection = FlexDirection.Row;
            _stationRow.style.marginBottom = 8;
            Root.Add(_stationRow);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            Root.Add(_list);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 8;
            _hint.style.minHeight = 20;
            Root.Add(_hint);
        }

        /// <summary>
        /// Lo que hay que hacer cuando el jugador le da a una receta de cocina.
        /// </summary>
        /// <remarks>
        /// La cocina no se craftea de un clic: se juega. El panel no abre ventanas —eso
        /// lo hace quien las tiene todas— así que avisa y se aparta. Si nadie ha puesto
        /// nada aquí, cocinar vuelve a ser instantáneo, que es lo que había antes y no
        /// deja el menú roto.
        /// </remarks>
        public System.Action<Recipe> OnCook;

        /// <summary>Abre el menú ya en esa pestaña. Lo usa el fogón.</summary>
        public void Show(CraftStation station)
        {
            _station = station;
            Show();
        }

        public void Show()
        {
            if (!ServiceRegistry.TryGet(out _crafting)) return;
            ServiceRegistry.TryGet(out _inventory);
            ServiceRegistry.TryGet(out _economy);
            ServiceRegistry.TryGet(out _island);

            Root.style.display = DisplayStyle.Flex;
            BuildStations();
            Rebuild();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        private void BuildStations()
        {
            _stationRow.Clear();
            Add(CraftStation.Hand, "A mano");
            Add(CraftStation.Bench, "Mesa de trabajo");
            Add(CraftStation.Kitchen, "Cocina");

            void Add(CraftStation station, string text)
            {
                var button = UiTheme.Secondary(text, () => { _station = station; BuildStations(); Rebuild(); });
                button.style.marginRight = 6;
                button.style.fontSize = 13;
                if (station == _station) button.style.backgroundColor = UiTheme.Peach;
                _stationRow.Add(button);
            }
        }

        private void Rebuild()
        {
            _list.Clear();
            if (_crafting == null) return;

            int level = _island?.State?.Level ?? 1;
            int shown = 0;

            foreach (var recipe in _crafting.AvailableAt(_station, level))
                _list.Add(BuildRow(recipe, shown++ % 2 == 1));

            if (shown == 0)
                _list.Add(UiTheme.Body("Aquí todavía no puedes hacer nada.", soft: true));
        }

        private VisualElement BuildRow(Recipe recipe, bool alternate)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = row.style.paddingRight = 12;
            row.style.paddingTop = row.style.paddingBottom = 10;
            row.style.marginBottom = 4;
            if (alternate) row.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(row, UiTheme.Radius);

            var text = new VisualElement();
            text.style.flexGrow = 1;
            text.style.marginRight = 8;

            var name = UiTheme.Body(recipe.DisplayName);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.Add(name);
            text.Add(UiTheme.Body(recipe.Description, soft: true));
            text.Add(BuildIngredients(recipe));
            row.Add(text);

            // Lo que pide más Oficio del que hay se enseña igual, con lo que falta
            // escrito. Un catálogo que se rellena solo con los niveles es de las pocas
            // formas de que subir de vía se vea sin abrir una pantalla de números.
            var rank = RankOf(recipe) ?? ToolRankOf(recipe);
            if (rank.HasValue && !Gates.Allows(rank.Value, out string falta))
            {
                var locked = UiTheme.Chip(Gates.Short(rank.Value), UiTheme.InkFaint);
                locked.tooltip = falta;
                row.Add(locked);
                return row;
            }

            var error = _crafting.CanCraft(recipe.RecipeId, _station);
            if (error != CraftError.Ok)
            {
                var disabled = UiTheme.Disabled(ShortExcuse(error));
                disabled.style.fontSize = 12;
                row.Add(disabled);
                return row;
            }

            var make = UiTheme.Action("Hacer", () => Craft(recipe));
            make.style.marginRight = 6;
            row.Add(make);

            // Hacer cinco de una vez (Oficio 4). Solo donde tiene sentido: cocinar en
            // lote abriría cinco minijuegos seguidos, que es exactamente lo contrario
            // de una comodidad.
            if (recipe.Station != CraftStation.Kitchen && Gates.Allows(Unlock.BatchCrafting))
                row.Add(UiTheme.Secondary("×5", () => CraftMany(recipe, 5)));

            return row;
        }

        /// <summary>
        /// El escalón de Oficio que pide una receta, o nada si es de las de siempre.
        /// </summary>
        /// <remarks>
        /// Sale del nivel de desbloqueo que la receta ya traía, así que no hay una
        /// segunda tabla que mantener al día: lo caro de conseguir es también lo que
        /// pide más oficio, y eso ya estaba escrito en el catálogo.
        ///
        /// Las básicas no piden nada, y eso importa: las cinco herramientas y la caña
        /// salen de aquí, así que ponerles cualquier puerta dejaría al jugador nuevo sin
        /// azada y sin forma de subir Oficio para conseguirla.
        /// </remarks>
        /// <summary>
        /// Las herramientas del segundo escalón piden Oficio 6, salga lo que salga de
        /// su nivel de desbloqueo.
        /// </summary>
        /// <remarks>
        /// Se mira lo que fabrican y no cómo se llama la receta: el escalón está escrito
        /// en el catálogo del objeto, así que una herramienta nueva queda detrás de la
        /// misma puerta sin que haya que acordarse de apuntarla en ningún sitio.
        /// </remarks>
        private Unlock? ToolRankOf(Recipe recipe)
        {
            var item = _economy?.GetItem(recipe.OutputId);
            return item != null && item.Category == ItemCategory.Tool && item.ToolTier >= 2
                ? Unlock.BetterTools
                : (Unlock?)null;
        }

        private static Unlock? RankOf(Recipe recipe) =>
            recipe.UnlockLevel >= 8 ? Unlock.FineRecipes :
            recipe.UnlockLevel >= 4 ? Unlock.MiddlingRecipes :
                                      (Unlock?)null;

        /// <summary>
        /// Hace varias seguidas, y para en cuanto una falla.
        /// </summary>
        /// <remarks>
        /// Parar al primer fallo y no seguir intentándolo: si se acaban los materiales
        /// a la tercera, lo que el jugador quiere ver es «has hecho tres», no cinco
        /// mensajes de error encima del mismo botón.
        /// </remarks>
        private void CraftMany(Recipe recipe, int times)
        {
            int done = 0;
            for (int i = 0; i < times; i++)
            {
                if (_crafting.Craft(recipe.RecipeId, _station) != CraftError.Ok) break;
                done++;
            }

            _hint.text = done == 0
                ? Excuse(_crafting.CanCraft(recipe.RecipeId, _station))
                : $"Hechos: {done} × {recipe.DisplayName}.";

            Rebuild();
        }

        /// <summary>
        /// Los ingredientes, con lo que te falta marcado. Es la línea que decide si el
        /// jugador entiende por qué no puede hacer algo o si solo ve un botón apagado.
        /// </summary>
        private VisualElement BuildIngredients(Recipe recipe)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginTop = 5;

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i];
                int have = _inventory?.CountOf(ingredient.CatalogId) ?? 0;
                bool enough = have >= ingredient.Quantity;

                var pill = UiTheme.Pill($"{NameOf(ingredient.CatalogId)} {have}/{ingredient.Quantity}",
                                        enough ? UiTheme.Mint : UiTheme.Rose);
                row.Add(pill);
            }

            return row;
        }

        private void Craft(Recipe recipe)
        {
            // Cocinar es jugar. Se comprueba antes de arrancar el minijuego para no
            // hacerle pasar cinco pasos a alguien al que le falta un huevo: los
            // ingredientes se gastan al final, gane o pierda.
            if (recipe.Station == CraftStation.Kitchen && OnCook != null)
            {
                var check = _crafting.CanCraft(recipe.RecipeId, _station);
                if (check != CraftError.Ok) { _hint.text = Excuse(check); Rebuild(); return; }

                OnCook(recipe);
                return;
            }

            var error = _crafting.Craft(recipe.RecipeId, _station);
            _hint.text = error == CraftError.Ok
                ? $"Hecho: {recipe.DisplayName}."
                : Excuse(error);

            Rebuild();
        }

        private static string ShortExcuse(CraftError error) => error switch
        {
            CraftError.MissingIngredients => "faltan cosas",
            CraftError.InventoryFull => "sin sitio",
            CraftError.Locked => "bloqueado",
            _ => "no",
        };

        private static string Excuse(CraftError error) => error switch
        {
            CraftError.MissingIngredients => "Te faltan materiales.",
            CraftError.InventoryFull => "No te cabe. Suelta algo antes.",
            CraftError.Locked => "La isla todavía no da para tanto.",
            CraftError.WrongStation => "Aquí no se puede hacer eso.",
            _ => "No se pudo.",
        };

        private string NameOf(string catalogId)
        {
            var item = _economy?.GetItem(catalogId);
            if (item != null) return item.DisplayName;

            int underscore = catalogId.IndexOf('_');
            return underscore >= 0 ? catalogId[(underscore + 1)..].Replace('_', ' ') : catalogId;
        }
    }
}
