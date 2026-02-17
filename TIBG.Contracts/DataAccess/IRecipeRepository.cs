using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Repository interface for recipe data access
    /// </summary>
    public interface IRecipeRepository
    {
        Task<Recipe> AddAsync(Recipe recipe);
        Task<Recipe?> GetByIdAsync(int id);
        Task<Recipe?> GetByIdWithIngredientsAsync(int id);
        Task<List<Recipe>> GetByUserIdAsync(int userId);
        Task<(List<Recipe> recipes, int totalCount)> GetPagedByUserIdAsync(int userId, int page, int pageSize);
        Task<Recipe> UpdateAsync(Recipe recipe);
        Task<bool> DeleteAsync(int id);
    }
}
