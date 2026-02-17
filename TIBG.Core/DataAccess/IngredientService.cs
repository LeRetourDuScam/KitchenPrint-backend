using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for ingredient business logic
    /// </summary>
    public class IngredientService : IIngredientService
    {
        private readonly IIngredientRepository _repository;
        private readonly IExternalIngredientService _externalService;
        private readonly ILogger<IngredientService> _logger;

        public IngredientService(
            IIngredientRepository repository,
            IExternalIngredientService externalService,
            ILogger<IngredientService> logger)
        {
            _repository = repository;
            _externalService = externalService;
            _logger = logger;
        }

        public async Task<IngredientDto?> GetByIdAsync(int id)
        {
            try
            {
                var ingredient = await _repository.GetByIdAsync(id);
                return ingredient != null ? MapToDto(ingredient) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ingredient by ID: {Id}", id);
                throw;
            }
        }

        public async Task<IngredientListResponse> SearchAsync(IngredientSearchRequest request)
        {
            try
            {
                var (ingredients, totalCount) = await _repository.SearchAsync(
                    request.Query,
                    request.Category,
                    request.Season,
                    request.Page,
                    request.PageSize);

                return new IngredientListResponse
                {
                    Ingredients = ingredients.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ingredients with query: {Query}", request.Query);
                throw;
            }
        }

        public async Task<IngredientDto> CreateAsync(Ingredient ingredient)
        {
            try
            {
                var created = await _repository.AddAsync(ingredient);
                _logger.LogInformation("Ingredient created: {Name}", created.Name);
                return MapToDto(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ingredient: {Name}", ingredient.Name);
                throw;
            }
        }

        public async Task<List<IngredientDto>> SyncFromExternalApiAsync(List<string> ingredientNames)
        {
            try
            {
                var syncedIngredients = new List<IngredientDto>();

                foreach (var name in ingredientNames)
                {
                    try
                    {
                        _logger.LogInformation("Syncing ingredient from external API: {Name}", name);

                        // Try to fetch from external API
                        var ingredient = await _externalService.SearchIngredientAsync(name);

                        if (ingredient != null)
                        {
                            // Check if already exists by external ID
                            var existing = await _repository.GetByExternalIdAsync(ingredient.ExternalId!);

                            if (existing == null)
                            {
                                var created = await _repository.AddAsync(ingredient);
                                syncedIngredients.Add(MapToDto(created));
                                _logger.LogInformation("Ingredient synced and created: {Name}", name);
                            }
                            else
                            {
                                // Update existing
                                existing.CarbonEmissionKgPerKg = ingredient.CarbonEmissionKgPerKg;
                                existing.WaterFootprintLitersPerKg = ingredient.WaterFootprintLitersPerKg;
                                existing.NutritionData = ingredient.NutritionData;
                                existing.UpdatedAt = DateTime.UtcNow;

                                var updated = await _repository.UpdateAsync(existing);
                                syncedIngredients.Add(MapToDto(updated));
                                _logger.LogInformation("Ingredient updated from external API: {Name}", name);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Ingredient not found in external APIs: {Name}", name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing individual ingredient: {Name}", name);
                        // Continue with other ingredients
                    }
                }

                return syncedIngredients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing ingredients from external API");
                throw;
            }
        }

        private IngredientDto MapToDto(Ingredient ingredient)
        {
            return new IngredientDto
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                Category = ingredient.Category,
                CarbonEmissionKgPerKg = ingredient.CarbonEmissionKgPerKg,
                WaterFootprintLitersPerKg = ingredient.WaterFootprintLitersPerKg,
                Season = ingredient.Season,
                Origin = ingredient.Origin,
                ApiSource = ingredient.ApiSource
            };
        }
    }
}
