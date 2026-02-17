using TIBG.Models;

namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Service interface for ingredient alternative suggestions using AI
    /// </summary>
    public interface IAlternativesService
    {
        /// <summary>
        /// Get alternative ingredient suggestions with lower environmental impact
        /// </summary>
        Task<AlternativeSuggestionsResponse> GetAlternativesAsync(int ingredientId);

        /// <summary>
        /// Get AI-powered alternative suggestions for an ingredient by name
        /// </summary>
        Task<AlternativeSuggestionsResponse> GetAlternativesByNameAsync(string ingredientName, string? category = null);
    }
}
