using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Service interface for fetching ingredient data from external APIs
    /// (Agribalyse, Open Food Facts)
    /// </summary>
    public interface IExternalIngredientService
    {
        /// <summary>
        /// Fetch ingredient data from Agribalyse API
        /// </summary>
        Task<Ingredient?> GetFromAgribalyseAsync(string ingredientName);

        /// <summary>
        /// Fetch ingredient data from Open Food Facts API
        /// </summary>
        Task<Ingredient?> GetFromOpenFoodFactsAsync(string ingredientName);

        /// <summary>
        /// Search for ingredient with fallback strategy (Agribalyse -> OpenFoodFacts)
        /// </summary>
        Task<Ingredient?> SearchIngredientAsync(string ingredientName);
    }
}
