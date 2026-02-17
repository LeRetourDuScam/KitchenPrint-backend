using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Service interface for nutritional data enrichment
    /// </summary>
    public interface INutritionService
    {
        /// <summary>
        /// Get nutritional information for an ingredient by ID
        /// </summary>
        Task<NutritionInfo?> GetNutritionByIngredientIdAsync(int ingredientId);

        /// <summary>
        /// Get nutritional information for a full recipe
        /// </summary>
        Task<RecipeNutritionResponse> GetRecipeNutritionAsync(List<RecipeIngredientRequest> ingredients);
    }
}
