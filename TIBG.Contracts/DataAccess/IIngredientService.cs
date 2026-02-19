using KitchenPrint.Core.Models;

namespace KitchenPrint.Contracts.DataAccess
{
    /// <summary>
    /// Service interface for ingredient business logic
    /// </summary>
    public interface IIngredientService
    {
        Task<IngredientDto?> GetByIdAsync(int id);
        Task<IngredientListResponse> SearchAsync(IngredientSearchRequest request);
        Task<IngredientDto> CreateAsync(Ingredient ingredient);
        Task<List<IngredientDto>> SyncFromExternalApiAsync(List<string> ingredientNames);
    }
}
