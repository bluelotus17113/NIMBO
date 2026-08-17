using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;

namespace Nimbo.Crafting
{
    /// <summary>
    /// Convierte lo recogido en algo que sirva. El puente entre la recolección y los
    /// catálogos de vivienda, decoración y comida.
    /// </summary>
    public class CraftingService : ICraftingService
    {
        readonly RecipeCatalog _catalog;
        readonly IInventoryService _inventory;
        readonly IIslandService _island;

        public CraftingService(RecipeCatalog catalog, IInventoryService inventory, IIslandService island)
        {
            _catalog = catalog;
            _inventory = inventory;
            _island = island;
        }

        public IReadOnlyList<Recipe> Recipes => _catalog.All;

        public bool TryGetRecipe(string recipeId, out Recipe recipe)
        {
            return _catalog.TryGet(recipeId, out recipe);
        }

        /// <summary>
        /// Recetas de esa estación con nivel suficiente, aunque falten materiales.
        /// Ver lo que podrías hacer es la mitad del interés de una lista de recetas.
        /// </summary>
        public IEnumerable<Recipe> AvailableAt(CraftStation station, int islandLevel)
        {
            return _catalog.All.Where(r => r.Station == station && r.UnlockLevel <= islandLevel);
        }

        /// <summary>
        /// No cambia nada. Sirve para pintar el botón y decir por qué no se puede.
        /// El orden importa: está pensado para que el primer error que se encuentra
        /// sea el más útil para el jugador.
        /// </summary>
        public CraftError CanCraft(string recipeId, CraftStation station)
        {
            // 1. La receta ni siquiera existe
            if (!_catalog.TryGet(recipeId, out var recipe))
                return CraftError.UnknownRecipe;

            // 2. Se está intentando en el sitio equivocado
            if (recipe.Station != station)
                return CraftError.WrongStation;

            // 3. El nivel de isla no llega para desbloquearla
            if (recipe.UnlockLevel > _island.State.Level)
                return CraftError.Locked;

            // 4. Faltan ingredientes
            foreach (var ing in recipe.Ingredients)
            {
                if (_inventory.CountOf(ing.CatalogId) < ing.Quantity)
                    return CraftError.MissingIngredients;
            }

            // 5. No hay hueco en la mochila para el resultado
            if (!WouldFit(recipe.OutputId, recipe.OutputQuantity))
                return CraftError.InventoryFull;

            return CraftError.Ok;
        }

        /// <summary>
        /// Gasta los ingredientes y mete el resultado.
        /// Se comprueba TODO primero —incluido que quepa en la mochila— antes de
        /// gastar un solo material. Si se gastara primero y luego fallara el hueco,
        /// el jugador perdería materiales sin recibir nada, y eso no se puede deshacer.
        /// </summary>
        public CraftError Craft(string recipeId, CraftStation station, string outputOverride = null)
        {
            // ── comprobar todo antes de tocar nada ──────────────────────────
            if (!_catalog.TryGet(recipeId, out var recipe))
                return CraftError.UnknownRecipe;

            if (recipe.Station != station)
                return CraftError.WrongStation;

            if (recipe.UnlockLevel > _island.State.Level)
                return CraftError.Locked;

            foreach (var ing in recipe.Ingredients)
            {
                if (_inventory.CountOf(ing.CatalogId) < ing.Quantity)
                    return CraftError.MissingIngredients;
            }

            // Lo que va a salir de verdad. La cocina manda engrudo cuando el minijuego
            // se tuerce, y entonces sale una unidad: un desastre no rinde tres platos.
            string outputId = string.IsNullOrEmpty(outputOverride) ? recipe.OutputId : outputOverride;
            int outputQuantity = string.IsNullOrEmpty(outputOverride) ? recipe.OutputQuantity : 1;

            // se comprueba que quepa antes de gastar, o te quedas sin materiales y sin objeto
            if (!WouldFit(outputId, outputQuantity))
                return CraftError.InventoryFull;

            // ── todo bien: gastar ingredientes ──────────────────────────────
            foreach (var ing in recipe.Ingredients)
            {
                _inventory.TryTake(ing.CatalogId, ing.Quantity);
            }

            // ── meter el resultado en la mochila ────────────────────────────
            _inventory.TryStore(outputId, outputQuantity, out _);

            // ── avisar al resto del juego, una sola vez y solo si salió bien ──
            EventBus.Publish(new ItemCrafted(recipe.RecipeId, outputId, outputQuantity));

            return CraftError.Ok;
        }

        /// <summary>
        /// ¿Cabrían <paramref name="quantity"/> unidades de
        /// <paramref name="catalogId"/> en la mochila sin tocar nada?
        /// Cuenta huecos existentes del mismo tipo y huecos vacíos.
        /// </summary>
        bool WouldFit(string catalogId, int quantity)
        {
            int stackLimit = _inventory.StackLimitOf(catalogId);
            int remaining = quantity;

            // primero rellenar pilas que ya existan de ese objeto
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                var stack = _inventory.At(i);
                if (stack.CatalogId == catalogId && stack.Quantity < stackLimit)
                {
                    remaining -= stackLimit - stack.Quantity;
                    if (remaining <= 0) return true;
                }
            }

            // luego huecos completamente vacíos
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                var stack = _inventory.At(i);
                if (string.IsNullOrEmpty(stack.CatalogId) || stack.Quantity <= 0)
                {
                    remaining -= stackLimit;
                    if (remaining <= 0) return true;
                }
            }

            return remaining <= 0;
        }
    }
}
