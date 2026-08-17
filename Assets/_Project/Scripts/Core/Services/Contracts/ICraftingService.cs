using System.Collections.Generic;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Dónde se puede hacer. Lo de banco pide estar junto a la mesa.</summary>
    public enum CraftStation
    {
        Hand = 0,   // en cualquier sitio, andando
        Bench = 1,  // en la mesa de trabajo de tu casa
        Kitchen = 2,
    }

    /// <summary>Un ingrediente de una receta.</summary>
    public readonly struct CraftIngredient
    {
        public readonly string CatalogId;
        public readonly int Quantity;
        public CraftIngredient(string catalogId, int quantity)
        {
            CatalogId = catalogId; Quantity = quantity;
        }
    }

    /// <summary>Una receta. Ficha muerta: no sabe si se puede hacer.</summary>
    public readonly struct Recipe
    {
        public readonly string RecipeId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly CraftStation Station;

        public readonly IReadOnlyList<CraftIngredient> Ingredients;

        public readonly string OutputId;
        public readonly int OutputQuantity;

        /// <summary>Nivel de isla que hace falta para que aparezca.</summary>
        public readonly int UnlockLevel;

        public Recipe(string recipeId, string displayName, string description,
                      CraftStation station, IReadOnlyList<CraftIngredient> ingredients,
                      string outputId, int outputQuantity, int unlockLevel)
        {
            RecipeId = recipeId; DisplayName = displayName; Description = description;
            Station = station; Ingredients = ingredients;
            OutputId = outputId; OutputQuantity = outputQuantity; UnlockLevel = unlockLevel;
        }
    }

    public enum CraftError
    {
        Ok = 0,
        UnknownRecipe,
        MissingIngredients,
        WrongStation,
        Locked,           // el nivel de isla no llega
        InventoryFull,
    }

    /// <summary>
    /// Convertir lo recogido en algo que sirva.
    /// </summary>
    /// <remarks>
    /// Es el puente entre lo nuevo y lo que ya había: lo que sale de aquí son muebles
    /// del catálogo de vivienda, adornos del de decoración, herramientas mejores y
    /// regalos. Nada de esto necesita objetos nuevos inventados, y por eso el crafteo
    /// da valor a la isla entera en vez de ser un sistema aparte.
    /// </remarks>
    public interface ICraftingService
    {
        IReadOnlyList<Recipe> Recipes { get; }

        bool TryGetRecipe(string recipeId, out Recipe recipe);

        /// <summary>Las que se pueden hacer ahí y con ese nivel de isla.</summary>
        IEnumerable<Recipe> AvailableAt(CraftStation station, int islandLevel);

        /// <summary>No cambia nada; sirve para pintar el botón y su motivo.</summary>
        CraftError CanCraft(string recipeId, CraftStation station);

        /// <summary>
        /// Gasta los ingredientes y mete el resultado en la mochila. Si no cabe, no
        /// gasta nada: quedarse sin materiales y sin objeto no puede pasar.
        /// </summary>
        /// <param name="outputOverride">
        /// Qué sale en lugar de lo que dice la receta. Lo usa la cocina: si el
        /// minijuego se tuerce, los ingredientes se gastan igual y de la olla sale
        /// engrudo. Los ingredientes se cobran en un solo sitio a propósito — con la
        /// cocina descontándolos por su cuenta, cualquier cambio en el crafteo dejaría
        /// de aplicarse justo ahí.
        /// </param>
        CraftError Craft(string recipeId, CraftStation station, string outputOverride = null);
    }
}
