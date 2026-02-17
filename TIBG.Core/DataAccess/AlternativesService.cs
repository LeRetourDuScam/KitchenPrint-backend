using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TIBG.API.Core.Configuration;
using TIBG.Contracts.DataAccess;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Service for suggesting ingredient alternatives using Groq AI + database lookup.
    /// Provides lower-carbon and lower-water alternatives for recipe ingredients.
    /// </summary>
    public class AlternativesService : IAlternativesService
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly AiSettings _aiSettings;
        private readonly ILogger<AlternativesService> _logger;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        public AlternativesService(
            IIngredientRepository ingredientRepository,
            HttpClient httpClient,
            IMemoryCache cache,
            IOptions<AiSettings> aiSettings,
            ILogger<AlternativesService> logger)
        {
            _ingredientRepository = ingredientRepository;
            _httpClient = httpClient;
            _cache = cache;
            _aiSettings = aiSettings.Value;
            _logger = logger;
        }

        public async Task<AlternativeSuggestionsResponse> GetAlternativesAsync(int ingredientId)
        {
            var cacheKey = $"alternatives_{ingredientId}";
            if (_cache.TryGetValue<AlternativeSuggestionsResponse>(cacheKey, out var cached))
            {
                return cached!;
            }

            try
            {
                var ingredient = await _ingredientRepository.GetByIdAsync(ingredientId);
                if (ingredient == null)
                {
                    throw new ArgumentException($"Ingredient with ID {ingredientId} not found. The database may be empty or not seeded.");
                }

                var response = await BuildAlternatives(ingredient);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheDuration)
                    .SetSize(1);
                _cache.Set(cacheKey, response, cacheOptions);
                return response;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw ArgumentException to be handled as 404
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while getting ingredient {Id}. Ensure database is connected and seeded.", ingredientId);
                throw new InvalidOperationException($"Failed to retrieve ingredient {ingredientId} from database. Database may not be seeded or connection failed.", ex);
            }
        }

        public async Task<AlternativeSuggestionsResponse> GetAlternativesByNameAsync(string ingredientName, string? category = null)
        {
            var cacheKey = $"alternatives_name_{ingredientName.ToLower()}";
            if (_cache.TryGetValue<AlternativeSuggestionsResponse>(cacheKey, out var cached))
            {
                return cached!;
            }

            // Search in database first
            var (ingredients, _) = await _ingredientRepository.SearchAsync(ingredientName, category, null, 1, 1);
            var ingredient = ingredients.FirstOrDefault();

            if (ingredient == null)
            {
                // Create a temporary ingredient for AI suggestions
                ingredient = new Ingredient
                {
                    Name = ingredientName,
                    Category = category ?? "unknown",
                    CarbonEmissionKgPerKg = 0,
                    WaterFootprintLitersPerKg = 0
                };
            }

            var response = await BuildAlternatives(ingredient);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetSize(1);
            _cache.Set(cacheKey, response, cacheOptions);
            return response;
        }

        private async Task<AlternativeSuggestionsResponse> BuildAlternatives(Ingredient ingredient)
        {
            var response = new AlternativeSuggestionsResponse
            {
                OriginalIngredientId = ingredient.Id,
                OriginalIngredientName = ingredient.Name,
                OriginalCarbonKgPerKg = ingredient.CarbonEmissionKgPerKg,
                OriginalWaterLitersPerKg = ingredient.WaterFootprintLitersPerKg
            };

            // Step 1: Get database alternatives (same category, lower impact)
            var dbAlternatives = await GetDatabaseAlternatives(ingredient);
            response.Alternatives.AddRange(dbAlternatives);

            // Step 2: Get AI-powered suggestions
            try
            {
                var aiInsight = await GetAiAlternativeSuggestions(ingredient);
                response.AiInsight = aiInsight;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get AI suggestions for {Name}, proceeding with DB alternatives only", ingredient.Name);
                response.AiInsight = "Suggestions IA non disponibles pour le moment.";
            }

            return response;
        }

        private async Task<List<AlternativeIngredient>> GetDatabaseAlternatives(Ingredient original)
        {
            var alternatives = new List<AlternativeIngredient>();

            try
            {
                // Get all ingredients to find lower-impact alternatives
                var allIngredients = await _ingredientRepository.GetAllAsync();

                // Find alternatives with lower carbon impact
                var lowerCarbonAlternatives = allIngredients
                    .Where(i => i.Id != original.Id
                        && i.CarbonEmissionKgPerKg < original.CarbonEmissionKgPerKg * 0.7m // At least 30% reduction
                        && i.IsActive)
                    .OrderBy(i => i.CarbonEmissionKgPerKg)
                    .Take(5)
                    .ToList();

                // Prefer same category alternatives first
                var sameCategoryAlts = lowerCarbonAlternatives
                    .Where(i => i.Category == original.Category)
                    .Take(3)
                    .ToList();

                var crossCategoryAlts = lowerCarbonAlternatives
                    .Where(i => i.Category != original.Category)
                    .Take(2)
                    .ToList();

                foreach (var alt in sameCategoryAlts.Concat(crossCategoryAlts))
                {
                    alternatives.Add(new AlternativeIngredient
                    {
                        IngredientId = alt.Id,
                        Name = alt.Name,
                        Category = alt.Category,
                        CarbonEmissionKgPerKg = alt.CarbonEmissionKgPerKg,
                        WaterFootprintLitersPerKg = alt.WaterFootprintLitersPerKg,
                        CarbonReductionPercent = original.CarbonEmissionKgPerKg > 0
                            ? Math.Round((1 - alt.CarbonEmissionKgPerKg / original.CarbonEmissionKgPerKg) * 100, 1)
                            : 0,
                        WaterReductionPercent = original.WaterFootprintLitersPerKg > 0
                            ? Math.Round((1 - alt.WaterFootprintLitersPerKg / original.WaterFootprintLitersPerKg) * 100, 1)
                            : 0,
                        Season = alt.Season,
                        Origin = alt.Origin,
                        Reason = $"Impact carbone {Math.Round((1 - alt.CarbonEmissionKgPerKg / Math.Max(original.CarbonEmissionKgPerKg, 0.01m)) * 100)}% plus faible"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database alternatives for {Name}", original.Name);
            }

            return alternatives;
        }

        private async Task<string> GetAiAlternativeSuggestions(Ingredient ingredient)
        {
            var systemPrompt = @"Tu es un expert en nutrition durable et en empreinte environnementale des aliments.
On te donne un ingrédient avec son impact carbone et hydrique.
Donne 2-3 phrases de conseils pratiques pour remplacer cet ingrédient par des alternatives plus durables.
Sois concis, pratique et encourageant. Réponds en français.";

            var userPrompt = $@"Ingrédient: {ingredient.Name}
Catégorie: {ingredient.Category ?? "non définie"}
Empreinte carbone: {ingredient.CarbonEmissionKgPerKg} kg CO₂/kg
Empreinte eau: {ingredient.WaterFootprintLitersPerKg} L/kg

Quelles alternatives plus durables peux-tu suggérer ?";

            var requestBody = new
            {
                model = _aiSettings.ModelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 300,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiSettings.ApiKey}");

            var response = await _httpClient.PostAsync(_aiSettings.BaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI API returned {Status} for alternative suggestions", response.StatusCode);
                return "Suggestions IA indisponibles. Consultez les alternatives de la base de données ci-dessus.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var message = aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return message ?? "Aucune suggestion disponible.";
        }
    }
}
