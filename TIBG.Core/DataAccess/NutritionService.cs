using Microsoft.Extensions.Logging;
using System.Text.Json;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for nutritional data enrichment.
    /// Uses stored nutrition data from Open Food Facts + default values.
    /// </summary>
    public class NutritionService : INutritionService
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ILogger<NutritionService> _logger;

        // Default nutritional values per 100g by category (approximate)
        private static readonly Dictionary<string, NutritionDefaults> CategoryDefaults = new()
        {
            { "Viandes", new(200, 20, 0, 12, 0) },
            { "Poissons", new(120, 20, 0, 4, 0) },
            { "Produits laitiers", new(100, 5, 5, 6, 0) },
            { "Œufs", new(155, 13, 1.1m, 11, 0) },
            { "Céréales", new(350, 10, 70, 2, 3) },
            { "Légumes", new(30, 2, 5, 0.3m, 2) },
            { "Légumineuses", new(130, 9, 20, 0.5m, 6) },
            { "Fruits", new(50, 0.8m, 12, 0.3m, 2) },
            { "Huiles", new(884, 0, 0, 100, 0) },
            { "Condiments", new(50, 1, 10, 0.5m, 0.5m) },
            { "Fruits à coque", new(600, 15, 15, 50, 6) },
            { "Boissons végétales", new(40, 1, 5, 1.5m, 0.5m) },
            { "Autres", new(200, 5, 30, 8, 2) },
        };

        public NutritionService(
            IIngredientRepository ingredientRepository,
            ILogger<NutritionService> logger)
        {
            _ingredientRepository = ingredientRepository;
            _logger = logger;
        }

        public async Task<NutritionInfo?> GetNutritionByIngredientIdAsync(int ingredientId)
        {
            try
            {
                var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
                if (ingredient == null) return null;

                return ExtractNutrition(ingredient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nutrition for ingredient {Id}", ingredientId);
                throw;
            }
        }

        public async Task<RecipeNutritionResponse> GetRecipeNutritionAsync(List<RecipeIngredientRequest> ingredients)
        {
            try
            {
                var response = new RecipeNutritionResponse();

                foreach (var reqIngredient in ingredients)
                {
                    var ingredient = await _ingredientRepository.GetByIdAsync(reqIngredient.IngredientId);
                    if (ingredient == null) continue;

                    var nutrition = ExtractNutrition(ingredient);
                    if (nutrition == null) continue;

                    var factor = reqIngredient.QuantityGrams / 100m; // nutrition is per 100g

                    var detail = new IngredientNutritionDetail
                    {
                        IngredientId = ingredient.Id,
                        IngredientName = ingredient.Name,
                        QuantityGrams = reqIngredient.QuantityGrams,
                        CaloriesKcal = Math.Round(nutrition.CaloriesKcal * factor, 1),
                        ProteinG = Math.Round(nutrition.ProteinG * factor, 1),
                        CarbohydratesG = Math.Round(nutrition.CarbohydratesG * factor, 1),
                        FatG = Math.Round(nutrition.FatG * factor, 1),
                        FiberG = Math.Round(nutrition.FiberG * factor, 1)
                    };

                    response.IngredientDetails.Add(detail);
                    response.TotalCalories += detail.CaloriesKcal;
                    response.TotalProteinG += detail.ProteinG;
                    response.TotalCarbohydratesG += detail.CarbohydratesG;
                    response.TotalFatG += detail.FatG;
                    response.TotalFiberG += detail.FiberG;
                }

                // Round totals
                response.TotalCalories = Math.Round(response.TotalCalories, 1);
                response.TotalProteinG = Math.Round(response.TotalProteinG, 1);
                response.TotalCarbohydratesG = Math.Round(response.TotalCarbohydratesG, 1);
                response.TotalFatG = Math.Round(response.TotalFatG, 1);
                response.TotalFiberG = Math.Round(response.TotalFiberG, 1);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recipe nutrition");
                throw;
            }
        }

        private NutritionInfo ExtractNutrition(Ingredient ingredient)
        {
            var nutrition = new NutritionInfo
            {
                IngredientId = ingredient.Id,
                IngredientName = ingredient.Name
            };

            // Try to parse stored nutrition data from Open Food Facts
            if (!string.IsNullOrWhiteSpace(ingredient.NutritionData))
            {
                try
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(ingredient.NutritionData);

                    if (data.TryGetProperty("energy_100g", out var energy) && energy.ValueKind == JsonValueKind.Number)
                        nutrition.CaloriesKcal = (decimal)(energy.GetDouble() / 4.184); // kJ to kcal
                    if (data.TryGetProperty("proteins_100g", out var protein) && protein.ValueKind == JsonValueKind.Number)
                        nutrition.ProteinG = (decimal)protein.GetDouble();
                    if (data.TryGetProperty("carbohydrates_100g", out var carbs) && carbs.ValueKind == JsonValueKind.Number)
                        nutrition.CarbohydratesG = (decimal)carbs.GetDouble();
                    if (data.TryGetProperty("fat_100g", out var fat) && fat.ValueKind == JsonValueKind.Number)
                        nutrition.FatG = (decimal)fat.GetDouble();
                    if (data.TryGetProperty("fiber_100g", out var fiber) && fiber.ValueKind == JsonValueKind.Number)
                        nutrition.FiberG = (decimal)fiber.GetDouble();
                    if (data.TryGetProperty("salt_100g", out var salt) && salt.ValueKind == JsonValueKind.Number)
                        nutrition.SaltG = (decimal)salt.GetDouble();

                    nutrition.Source = "Open Food Facts";
                    return nutrition;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse nutrition data for ingredient {Id}", ingredient.Id);
                }
            }

            // Fallback to category defaults
            var category = ingredient.Category ?? "Autres";
            if (CategoryDefaults.TryGetValue(category, out var defaults))
            {
                nutrition.CaloriesKcal = defaults.Calories;
                nutrition.ProteinG = defaults.Protein;
                nutrition.CarbohydratesG = defaults.Carbs;
                nutrition.FatG = defaults.Fat;
                nutrition.FiberG = defaults.Fiber;
                nutrition.Source = "Estimation par catégorie";
            }
            else
            {
                var fallback = CategoryDefaults["Autres"];
                nutrition.CaloriesKcal = fallback.Calories;
                nutrition.ProteinG = fallback.Protein;
                nutrition.CarbohydratesG = fallback.Carbs;
                nutrition.FatG = fallback.Fat;
                nutrition.FiberG = fallback.Fiber;
                nutrition.Source = "Estimation générique";
            }

            return nutrition;
        }

        private record NutritionDefaults(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal Fiber);
    }
}
