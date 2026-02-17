using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Service interface for recipe calculation and management
    /// </summary>
    public interface IRecipeService
    {
        Task<RecipeCalculationResponse> CalculateImpactAsync(RecipeCalculationRequest request, int? userId = null, bool saveRecipe = false);
        Task<RecipeDto?> GetByIdAsync(int id, int userId);
        Task<(List<RecipeDto> recipes, int totalCount)> GetUserRecipesAsync(int userId, int page, int pageSize);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
