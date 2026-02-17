using Microsoft.Extensions.Logging;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for recipe calculation and management with carbon/water footprint
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ILogger<RecipeService> _logger;

        // Equivalence factors for environmental impact visualization
        private const decimal CO2_PER_KM_CAR = 0.12m; // kg CO2 per km
        private const decimal CO2_PER_SMARTPHONE_CHARGE = 0.008m; // kg CO2 per charge
        private const decimal WATER_PER_SHOWER = 50m; // liters per 5-min shower
        private const decimal WATER_PER_DAY_DRINKING = 2m; // liters per day

        public RecipeService(
            IRecipeRepository recipeRepository,
            IIngredientRepository ingredientRepository,
            ILogger<RecipeService> logger)
        {
            _recipeRepository = recipeRepository;
            _ingredientRepository = ingredientRepository;
            _logger = logger;
        }

        public async Task<RecipeCalculationResponse> CalculateImpactAsync(
            RecipeCalculationRequest request,
            int? userId = null,
            bool saveRecipe = false)
        {
            try
            {
                _logger.LogInformation("Calculating impact for recipe: {Name}", request.Name);

                var ingredientIds = request.Ingredients.Select(i => i.IngredientId).ToList();
                var ingredients = new Dictionary<int, Ingredient>();

                // Fetch all ingredients
                foreach (var id in ingredientIds)
                {
                    var ingredient = await _ingredientRepository.GetByIdAsync(id);
                    if (ingredient == null)
                    {
                        throw new ArgumentException($"Ingredient with ID {id} not found");
                    }
                    ingredients[id] = ingredient;
                }

                // Calculate impact for each ingredient
                var recipeIngredients = new List<RecipeIngredientDto>();
                decimal totalCarbon = 0m;
                decimal totalWater = 0m;

                foreach (var reqIngredient in request.Ingredients)
                {
                    var ingredient = ingredients[reqIngredient.IngredientId];
                    var quantityKg = reqIngredient.QuantityGrams / 1000m;

                    var carbonContribution = quantityKg * ingredient.CarbonEmissionKgPerKg;
                    var waterContribution = quantityKg * ingredient.WaterFootprintLitersPerKg;

                    totalCarbon += carbonContribution;
                    totalWater += waterContribution;

                    recipeIngredients.Add(new RecipeIngredientDto
                    {
                        IngredientId = ingredient.Id,
                        IngredientName = ingredient.Name,
                        QuantityGrams = reqIngredient.QuantityGrams,
                        CarbonContributionKg = carbonContribution,
                        WaterContributionLiters = waterContribution,
                        CarbonPercentage = 0, // Will be calculated after
                        WaterPercentage = 0    // Will be calculated after
                    });
                }

                // Calculate percentages
                if (totalCarbon > 0)
                {
                    foreach (var ri in recipeIngredients)
                    {
                        ri.CarbonPercentage = (ri.CarbonContributionKg / totalCarbon) * 100m;
                        ri.WaterPercentage = (ri.WaterContributionLiters / totalWater) * 100m;
                    }
                }

                // Calculate eco score (A to E based on carbon per serving)
                var carbonPerServing = totalCarbon / request.Servings;
                var ecoScore = CalculateEcoScore(carbonPerServing);

                // Calculate equivalents
                var equivalents = new EnvironmentalEquivalents
                {
                    CarKilometers = Math.Round(totalCarbon / CO2_PER_KM_CAR, 1),
                    SmartphoneCharges = (int)Math.Round(totalCarbon / CO2_PER_SMARTPHONE_CHARGE),
                    Showers = (int)Math.Round(totalWater / WATER_PER_SHOWER),
                    DaysOfDrinkingWater = Math.Round(totalWater / WATER_PER_DAY_DRINKING, 1)
                };

                var response = new RecipeCalculationResponse
                {
                    Name = request.Name,
                    Description = request.Description,
                    Servings = request.Servings,
                    TotalCarbonKg = Math.Round(totalCarbon, 4),
                    TotalWaterLiters = Math.Round(totalWater, 2),
                    EcoScore = ecoScore,
                    Ingredients = recipeIngredients,
                    Equivalents = equivalents
                };

                // Save recipe if requested and user is authenticated
                if (saveRecipe && userId.HasValue)
                {
                    var recipe = new Recipe
                    {
                        UserId = userId.Value,
                        Name = request.Name,
                        Description = request.Description,
                        Servings = request.Servings,
                        TotalCarbonKg = response.TotalCarbonKg,
                        TotalWaterLiters = response.TotalWaterLiters,
                        EcoScore = ecoScore,
                        RecipeIngredients = request.Ingredients.Select(ri => new RecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            QuantityGrams = ri.QuantityGrams,
                            CarbonContributionKg = recipeIngredients.First(i => i.IngredientId == ri.IngredientId).CarbonContributionKg,
                            WaterContributionLiters = recipeIngredients.First(i => i.IngredientId == ri.IngredientId).WaterContributionLiters
                        }).ToList()
                    };

                    var savedRecipe = await _recipeRepository.AddAsync(recipe);
                    response.RecipeId = savedRecipe.Id;

                    _logger.LogInformation("Recipe saved with ID: {RecipeId}", savedRecipe.Id);
                }

                _logger.LogInformation("Recipe calculation completed: Carbon={Carbon}kg, Water={Water}L, Score={Score}",
                    response.TotalCarbonKg, response.TotalWaterLiters, ecoScore);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recipe impact");
                throw;
            }
        }

        public async Task<RecipeDto?> GetByIdAsync(int id, int userId)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdWithIngredientsAsync(id);

                if (recipe == null || recipe.UserId != userId)
                {
                    return null;
                }

                return MapToDto(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe: {RecipeId}", id);
                throw;
            }
        }

        public async Task<(List<RecipeDto> recipes, int totalCount)> GetUserRecipesAsync(int userId, int page, int pageSize)
        {
            try
            {
                var (recipes, totalCount) = await _recipeRepository.GetPagedByUserIdAsync(userId, page, pageSize);

                var dtos = recipes.Select(MapToDto).ToList();

                return (dtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user recipes: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            try
            {
                var recipe = await _recipeRepository.GetByIdAsync(id);

                if (recipe == null || recipe.UserId != userId)
                {
                    return false;
                }

                return await _recipeRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe: {RecipeId}", id);
                throw;
            }
        }

        private string CalculateEcoScore(decimal carbonPerServing)
        {
            // Eco score thresholds (kg CO2 per serving)
            // A: < 0.5, B: 0.5-1.0, C: 1.0-2.0, D: 2.0-3.5, E: > 3.5
            if (carbonPerServing < 0.5m) return "A";
            if (carbonPerServing < 1.0m) return "B";
            if (carbonPerServing < 2.0m) return "C";
            if (carbonPerServing < 3.5m) return "D";
            return "E";
        }

        private RecipeDto MapToDto(Recipe recipe)
        {
            var totalCarbon = recipe.TotalCarbonKg;
            var totalWater = recipe.TotalWaterLiters;

            return new RecipeDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Description = recipe.Description,
                Servings = recipe.Servings,
                TotalCarbonKg = recipe.TotalCarbonKg,
                TotalWaterLiters = recipe.TotalWaterLiters,
                EcoScore = recipe.EcoScore,
                CreatedAt = recipe.CreatedAt,
                Ingredients = recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto
                {
                    IngredientId = ri.IngredientId,
                    IngredientName = ri.Ingredient.Name,
                    QuantityGrams = ri.QuantityGrams,
                    CarbonContributionKg = ri.CarbonContributionKg,
                    WaterContributionLiters = ri.WaterContributionLiters,
                    CarbonPercentage = totalCarbon > 0 ? (ri.CarbonContributionKg / totalCarbon) * 100m : 0,
                    WaterPercentage = totalWater > 0 ? (ri.WaterContributionLiters / totalWater) * 100m : 0
                }).ToList()
            };
        }
    }
}
