using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Repository interface for ingredient data access
    /// </summary>
    public interface IIngredientRepository
    {
        Task<Ingredient> AddAsync(Ingredient ingredient);
        Task<Ingredient?> GetByIdAsync(int id);
        Task<Ingredient?> GetByExternalIdAsync(string externalId);
        Task<List<Ingredient>> GetAllAsync();
        Task<(List<Ingredient> ingredients, int totalCount)> GetPagedAsync(int page, int pageSize);
        Task<(List<Ingredient> ingredients, int totalCount)> SearchAsync(string? query, string? category, string? season, int page, int pageSize);
        Task<List<Ingredient>> GetByCategoryAsync(string category);
        Task<Ingredient> UpdateAsync(Ingredient ingredient);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
