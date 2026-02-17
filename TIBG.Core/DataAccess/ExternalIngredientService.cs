using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for fetching ingredient data from external APIs
    /// Integrates with Agribalyse and Open Food Facts
    /// </summary>
    public class ExternalIngredientService : IExternalIngredientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExternalIngredientService> _logger;

        // Cache duration for external API results
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

        // Default water footprint values (liters/kg) when not available from API
        private static readonly Dictionary<string, decimal> DefaultWaterFootprints = new()
        {
            { "beef", 15400m },
            { "pork", 6000m },
            { "chicken", 4300m },
            { "rice", 2500m },
            { "wheat", 1800m },
            { "potato", 290m },
            { "tomato", 214m },
            { "apple", 822m },
            { "cheese", 5000m },
            { "milk", 1000m }
        };

        public ExternalIngredientService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<ExternalIngredientService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Ingredient?> GetFromAgribalyseAsync(string ingredientName)
        {
            try
            {
                var cacheKey = $"agribalyse_{ingredientName.ToLower()}";

                if (_cache.TryGetValue<Ingredient>(cacheKey, out var cachedIngredient))
                {
                    _logger.LogInformation("Returning cached Agribalyse data for: {Name}", ingredientName);
                    return cachedIngredient;
                }

                _logger.LogInformation("Fetching from Agribalyse API: {Name}", ingredientName);

                // Note: This is a placeholder for actual Agribalyse API integration
                // Agribalyse data is typically accessed via their CSV files or through ADEME's API
                // You'll need to implement the actual API call or CSV parsing here

                var httpClient = _httpClientFactory.CreateClient();

                // TODO: Implement actual Agribalyse API call
                // For now, return null to fallback to Open Food Facts
                _logger.LogWarning("Agribalyse API integration not yet implemented. Falling back to Open Food Facts.");

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from Agribalyse API: {Name}", ingredientName);
                return null;
            }
        }

        public async Task<Ingredient?> GetFromOpenFoodFactsAsync(string ingredientName)
        {
            try
            {
                var cacheKey = $"openfoodfacts_{ingredientName.ToLower()}";

                if (_cache.TryGetValue<Ingredient>(cacheKey, out var cachedIngredient))
                {
                    _logger.LogInformation("Returning cached Open Food Facts data for: {Name}", ingredientName);
                    return cachedIngredient;
                }

                _logger.LogInformation("Fetching from Open Food Facts API: {Name}", ingredientName);

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CarbonFootprint/1.0");

                // Search for products matching the ingredient name
                var searchUrl = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={Uri.EscapeDataString(ingredientName)}&search_simple=1&json=1&page_size=1";

                var response = await httpClient.GetAsync(searchUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Open Food Facts API returned status: {Status}", response.StatusCode);
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenFoodFactsResponse>(jsonResponse);

                if (data?.products == null || data.products.Count == 0)
                {
                    _logger.LogWarning("No products found in Open Food Facts for: {Name}", ingredientName);
                    return null;
                }

                var product = data.products[0];

                // Extract carbon footprint (g CO2/100g) and convert to kg CO2/kg
                decimal carbonEmission = 0m;
                if (product.ecoscore_data?.agribalyse?.co2_total != null)
                {
                    carbonEmission = (decimal)product.ecoscore_data.agribalyse.co2_total / 1000m;
                }
                else if (product.environment_impact_level_tags?.Contains("en:low") == true)
                {
                    carbonEmission = 0.5m; // Low impact estimate
                }
                else if (product.environment_impact_level_tags?.Contains("en:high") == true)
                {
                    carbonEmission = 5.0m; // High impact estimate
                }

                // Water footprint - use default values or estimate
                decimal waterFootprint = EstimateWaterFootprint(ingredientName, product.categories_tags);

                var ingredient = new Ingredient
                {
                    Name = ingredientName,
                    Category = product.categories_tags?.FirstOrDefault()?.Replace("en:", "") ?? "unknown",
                    CarbonEmissionKgPerKg = carbonEmission,
                    WaterFootprintLitersPerKg = waterFootprint,
                    Season = "all-year", // Default, could be enhanced
                    Origin = "imported", // Default, could be enhanced
                    ApiSource = "OpenFoodFacts",
                    ExternalId = $"off_{product.code}",
                    NutritionData = JsonSerializer.Serialize(new
                    {
                        product.nutriments?.energy_100g,
                        product.nutriments?.proteins_100g,
                        product.nutriments?.carbohydrates_100g,
                        product.nutriments?.fat_100g,
                        product.nutriments?.fiber_100g,
                        product.nutriments?.salt_100g
                    })
                };

                // Cache the result
                _cache.Set(cacheKey, ingredient, CacheDuration);

                _logger.LogInformation("Successfully fetched ingredient from Open Food Facts: {Name}", ingredientName);

                return ingredient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching from Open Food Facts API: {Name}", ingredientName);
                return null;
            }
        }

        public async Task<Ingredient?> SearchIngredientAsync(string ingredientName)
        {
            try
            {
                _logger.LogInformation("Searching for ingredient: {Name}", ingredientName);

                // Try Agribalyse first (more accurate for environmental data)
                var ingredient = await GetFromAgribalyseAsync(ingredientName);

                // Fallback to Open Food Facts
                if (ingredient == null)
                {
                    ingredient = await GetFromOpenFoodFactsAsync(ingredientName);
                }

                if (ingredient != null)
                {
                    _logger.LogInformation("Found ingredient: {Name} from {Source}",
                        ingredientName, ingredient.ApiSource);
                }
                else
                {
                    _logger.LogWarning("Ingredient not found in any external API: {Name}", ingredientName);
                }

                return ingredient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ingredient: {Name}", ingredientName);
                throw;
            }
        }

        private decimal EstimateWaterFootprint(string ingredientName, List<string>? categories)
        {
            var nameLower = ingredientName.ToLower();

            // Check if we have a default value for this ingredient
            foreach (var kvp in DefaultWaterFootprints)
            {
                if (nameLower.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            // Estimate based on category
            if (categories != null)
            {
                if (categories.Any(c => c.Contains("meat") || c.Contains("beef") || c.Contains("pork")))
                    return 10000m;
                if (categories.Any(c => c.Contains("dairy") || c.Contains("cheese")))
                    return 3000m;
                if (categories.Any(c => c.Contains("vegetables")))
                    return 300m;
                if (categories.Any(c => c.Contains("fruits")))
                    return 500m;
                if (categories.Any(c => c.Contains("cereals") || c.Contains("grains")))
                    return 2000m;
            }

            // Default fallback
            return 1000m;
        }
    }

    // DTOs for Open Food Facts API response
    internal class OpenFoodFactsResponse
    {
        public List<OpenFoodFactsProduct>? products { get; set; }
    }

    internal class OpenFoodFactsProduct
    {
        public string? code { get; set; }
        public string? product_name { get; set; }
        public List<string>? categories_tags { get; set; }
        public List<string>? environment_impact_level_tags { get; set; }
        public OpenFoodFactsEcoscoreData? ecoscore_data { get; set; }
        public OpenFoodFactsNutriments? nutriments { get; set; }
    }

    internal class OpenFoodFactsEcoscoreData
    {
        public OpenFoodFactsAgribalyse? agribalyse { get; set; }
    }

    internal class OpenFoodFactsAgribalyse
    {
        public double? co2_total { get; set; }
    }

    internal class OpenFoodFactsNutriments
    {
        public double? energy_100g { get; set; }
        public double? proteins_100g { get; set; }
        public double? carbohydrates_100g { get; set; }
        public double? fat_100g { get; set; }
        public double? fiber_100g { get; set; }
        public double? salt_100g { get; set; }
    }
}
