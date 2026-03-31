using KitchenPrint.API.Core.DataAccess;
using KitchenPrint.Core.Models;
using KitchenPrint.ENTITIES;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KitchenPrint.API.Core.DataAccess
{
    /// <summary>
    /// Seeds the database with ingredient data from the ADEME AGRIBALYSE REST API.
    /// CO2 values in kg CO2eq per kg of product (Changement_climatique).
    /// Water values in m³ deprivation per kg of product (Épuisement_des_ressources_eau), converted to liters (×1000).
    /// Source: AGRIBALYSE 3.1 Synthèse — ADEME data-fair API.
    /// </summary>
    public static class IngredientDataSeeder
    {
        private const string ApiBaseUrl = "https://data.ademe.fr/data-fair/api/v1/datasets/agribalyse-31-synthese/lines";
        private const int PageSize = 100;

        private static readonly HashSet<string> ExcludedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "entrées et plats composés",
            "aliments infantiles"
        };

        public static async Task SeedAsync(kitchenPrintDbContext context, ILogger logger)
        {
            if (await context.Ingredients.AnyAsync())
            {
                logger.LogInformation("Ingredients already seeded. Skipping.");
                return;
            }

            List<Ingredient> ingredients;

            try
            {
                logger.LogInformation("Seeding ingredient database from ADEME AGRIBALYSE API...");
                ingredients = await FetchFromAdemeApiAsync(logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch from ADEME API. Using fallback hardcoded data.");
                ingredients = GetFallbackIngredients();
            }

            if (ingredients.Count == 0)
            {
                logger.LogWarning("ADEME API returned no ingredients. Using fallback hardcoded data.");
                ingredients = GetFallbackIngredients();
            }

            foreach (var ingredient in ingredients)
            {
                if (string.IsNullOrEmpty(ingredient.NormalizedName))
                {
                    ingredient.NormalizedName = IngredientRepository.RemoveDiacritics(ingredient.Name).ToLower();
                }
            }

            await context.Ingredients.AddRangeAsync(ingredients);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} ingredients successfully.", ingredients.Count);
        }

        private static async Task<List<Ingredient>> FetchFromAdemeApiAsync(ILogger logger)
        {
            var ingredients = new List<Ingredient>();
            var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "KitchenPrint/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var select = Uri.EscapeDataString(
                "Code_AGB,Nom_du_Produit_en_Français,Groupe_d'aliment,Sous-groupe_d'aliment," +
                "code_saison,code_avion,Changement_climatique,Épuisement_des_ressources_eau");

            string? afterCursor = null;
            int totalFetched = 0;

            while (true)
            {
                var url = $"{ApiBaseUrl}?size={PageSize}&select={select}";
                if (!string.IsNullOrEmpty(afterCursor))
                    url += $"&after={Uri.EscapeDataString(afterCursor)}";

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<AdemeApiResponse>();
                if (data?.Results == null || data.Results.Count == 0)
                    break;

                foreach (var product in data.Results)
                {
                    var name = product.NomDuProduitEnFrancais?.Trim();
                    var code = product.CodeAGB?.Trim();
                    var category = product.GroupeAliment?.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                        continue;

                    if (!string.IsNullOrEmpty(category) && ExcludedCategories.Contains(category))
                        continue;

                    var externalId = "agrib_" + code;
                    if (externalId.Length > 100)
                        externalId = externalId[..100];

                    if (!seenExternalIds.Add(externalId))
                        continue;

                    var carbon = Math.Max(0, product.ChangementClimatique ?? 0m);
                    var water = Math.Max(0, (product.EpuisementRessourcesEau ?? 0m) * 1000m);

                    var season = product.CodeSaison switch
                    {
                        0 => "hors-saison",
                        1 => "seasonal",
                        _ => "all-year"
                    };

                    var origin = product.CodeAvion == true ? "imported" : "national";

                    var categoryFormatted = string.IsNullOrEmpty(category)
                        ? "Autres"
                        : char.ToUpper(category[0]) + category[1..];

                    ingredients.Add(new Ingredient
                    {
                        Name = name.Length > 200 ? name[..200] : name,
                        NormalizedName = IngredientRepository.RemoveDiacritics(name.Length > 200 ? name[..200] : name).ToLower(),
                        Category = categoryFormatted.Length > 100 ? categoryFormatted[..100] : categoryFormatted,
                        CarbonEmissionKgPerKg = carbon,
                        WaterFootprintLitersPerKg = water,
                        Season = season,
                        Origin = origin,
                        ApiSource = "Agribalyse 3.1",
                        ExternalId = externalId,
                        IsActive = true
                    });
                }

                totalFetched += data.Results.Count;
                logger.LogInformation("Fetched {Count}/{Total} products from ADEME API...", totalFetched, data.Total);

                if (data.Results.Count < PageSize)
                    break;

                afterCursor = data.Next?.Split("after=").LastOrDefault();
                if (string.IsNullOrEmpty(afterCursor))
                    break;
            }

            logger.LogInformation("Fetched {Count} ingredients from ADEME AGRIBALYSE API (excluded {Excluded}).",
                ingredients.Count, string.Join(", ", ExcludedCategories));
            return ingredients;
        }

        /// <summary>
        /// Fallback data used when the ADEME API is not available.
        /// Values sourced from Agribalyse 3.1.1 / ADEME Base Carbone.
        /// </summary>
        private static List<Ingredient> GetFallbackIngredients()
        {
            return new List<Ingredient>
            {
                // ===== VIANDES =====
                new() { Name = "Bœuf (steak)", Category = "Viandes", CarbonEmissionKgPerKg = 27.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beef_steak" },
                new() { Name = "Bœuf (haché)", Category = "Viandes", CarbonEmissionKgPerKg = 25.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beef_ground" },
                new() { Name = "Agneau", Category = "Viandes", CarbonEmissionKgPerKg = 39.0m, WaterFootprintLitersPerKg = 10400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lamb" },
                new() { Name = "Porc (côtelette)", Category = "Viandes", CarbonEmissionKgPerKg = 7.0m, WaterFootprintLitersPerKg = 6000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pork_chop" },
                new() { Name = "Poulet", Category = "Viandes", CarbonEmissionKgPerKg = 5.1m, WaterFootprintLitersPerKg = 4300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_chicken" },
                new() { Name = "Dinde", Category = "Viandes", CarbonEmissionKgPerKg = 5.0m, WaterFootprintLitersPerKg = 4500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_turkey" },
                new() { Name = "Jambon", Category = "Viandes", CarbonEmissionKgPerKg = 6.0m, WaterFootprintLitersPerKg = 5900m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_ham" },

                // ===== POISSONS =====
                new() { Name = "Saumon", Category = "Poissons", CarbonEmissionKgPerKg = 11.9m, WaterFootprintLitersPerKg = 2000m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_salmon" },
                new() { Name = "Thon", Category = "Poissons", CarbonEmissionKgPerKg = 6.1m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_tuna" },
                new() { Name = "Sardine", Category = "Poissons", CarbonEmissionKgPerKg = 1.8m, WaterFootprintLitersPerKg = 800m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sardine" },
                new() { Name = "Crevettes", Category = "Poissons", CarbonEmissionKgPerKg = 20.0m, WaterFootprintLitersPerKg = 3500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_shrimp" },
                new() { Name = "Moules", Category = "Poissons", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 200m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_mussels" },

                // ===== PRODUITS LAITIERS =====
                new() { Name = "Lait entier", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.4m, WaterFootprintLitersPerKg = 1020m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_whole_milk" },
                new() { Name = "Beurre", Category = "Produits laitiers", CarbonEmissionKgPerKg = 9.0m, WaterFootprintLitersPerKg = 5550m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_butter" },
                new() { Name = "Fromage (emmental)", Category = "Produits laitiers", CarbonEmissionKgPerKg = 8.5m, WaterFootprintLitersPerKg = 5000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_emmental" },
                new() { Name = "Yaourt nature", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.7m, WaterFootprintLitersPerKg = 1100m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_yogurt" },
                new() { Name = "Crème fraîche", Category = "Produits laitiers", CarbonEmissionKgPerKg = 3.5m, WaterFootprintLitersPerKg = 2500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cream" },

                // ===== ŒUFS =====
                new() { Name = "Œufs", Category = "Œufs", CarbonEmissionKgPerKg = 4.5m, WaterFootprintLitersPerKg = 3300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_eggs" },

                // ===== CÉRÉALES =====
                new() { Name = "Riz blanc", Category = "Céréales", CarbonEmissionKgPerKg = 3.6m, WaterFootprintLitersPerKg = 2500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_white_rice" },
                new() { Name = "Pâtes", Category = "Céréales", CarbonEmissionKgPerKg = 1.3m, WaterFootprintLitersPerKg = 1850m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pasta" },
                new() { Name = "Pain", Category = "Céréales", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 1600m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_bread" },
                new() { Name = "Farine de blé", Category = "Céréales", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_wheat_flour" },
                new() { Name = "Quinoa", Category = "Céréales", CarbonEmissionKgPerKg = 1.2m, WaterFootprintLitersPerKg = 1500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_quinoa" },
                new() { Name = "Avoine", Category = "Céréales", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_oats" },

                // ===== LÉGUMES =====
                new() { Name = "Tomate", Category = "Légumes", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 214m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_tomato" },
                new() { Name = "Carotte", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 195m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_carrot" },
                new() { Name = "Pomme de terre", Category = "Légumes", CarbonEmissionKgPerKg = 0.2m, WaterFootprintLitersPerKg = 290m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_potato" },
                new() { Name = "Oignon", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 272m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_onion" },
                new() { Name = "Brocoli", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 284m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_broccoli" },
                new() { Name = "Épinards", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 292m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_spinach" },
                new() { Name = "Courgette", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 353m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_zucchini" },
                new() { Name = "Champignons", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 200m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_mushrooms" },
                new() { Name = "Avocat", Category = "Légumes", CarbonEmissionKgPerKg = 2.0m, WaterFootprintLitersPerKg = 1981m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_avocado" },

                // ===== LÉGUMINEUSES =====
                new() { Name = "Lentilles", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 5874m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lentils" },
                new() { Name = "Pois chiches", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 4177m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_chickpeas" },
                new() { Name = "Tofu", Category = "Légumineuses", CarbonEmissionKgPerKg = 2.0m, WaterFootprintLitersPerKg = 2523m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_tofu" },
                new() { Name = "Haricots blancs", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 5053m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_white_beans" },

                // ===== FRUITS =====
                new() { Name = "Pomme", Category = "Fruits", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 822m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_apple" },
                new() { Name = "Banane", Category = "Fruits", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 790m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_banana" },
                new() { Name = "Orange", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 560m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_orange" },
                new() { Name = "Fraise", Category = "Fruits", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 347m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_strawberry" },

                // ===== HUILES =====
                new() { Name = "Huile d'olive", Category = "Huiles", CarbonEmissionKgPerKg = 3.2m, WaterFootprintLitersPerKg = 14500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_olive_oil" },
                new() { Name = "Huile de tournesol", Category = "Huiles", CarbonEmissionKgPerKg = 2.5m, WaterFootprintLitersPerKg = 6800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sunflower_oil" },

                // ===== CONDIMENTS =====
                new() { Name = "Sel", Category = "Condiments", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 50m, Season = "all-year", Origin = "national", ApiSource = "ADEME", ExternalId = "ademe_salt" },
                new() { Name = "Sucre", Category = "Condiments", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 1782m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sugar" },

                // ===== AUTRES =====
                new() { Name = "Chocolat noir", Category = "Autres", CarbonEmissionKgPerKg = 4.2m, WaterFootprintLitersPerKg = 17196m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_dark_chocolate" },
                new() { Name = "Café", Category = "Autres", CarbonEmissionKgPerKg = 5.7m, WaterFootprintLitersPerKg = 15897m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_coffee" },
                new() { Name = "Noix", Category = "Fruits à coque", CarbonEmissionKgPerKg = 1.2m, WaterFootprintLitersPerKg = 9063m, Season = "seasonal", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_walnuts" },
                new() { Name = "Amandes", Category = "Fruits à coque", CarbonEmissionKgPerKg = 1.4m, WaterFootprintLitersPerKg = 16194m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_almonds" },
                new() { Name = "Lait d'avoine", Category = "Boissons végétales", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 480m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_oat_milk" },
                new() { Name = "Lait de soja", Category = "Boissons végétales", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 270m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_soy_milk" },
            };
        }
    }

    internal class AdemeApiResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("results")]
        public List<AdemeApiProduct>? Results { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    internal class AdemeApiProduct
    {
        [JsonPropertyName("Code_AGB")]
        public string? CodeAGB { get; set; }

        [JsonPropertyName("Nom_du_Produit_en_Français")]
        public string? NomDuProduitEnFrancais { get; set; }

        [JsonPropertyName("Groupe_d'aliment")]
        public string? GroupeAliment { get; set; }

        [JsonPropertyName("Sous-groupe_d'aliment")]
        public string? SousGroupeAliment { get; set; }

        [JsonPropertyName("code_saison")]
        public int? CodeSaison { get; set; }

        [JsonPropertyName("code_avion")]
        public bool? CodeAvion { get; set; }

        [JsonPropertyName("Changement_climatique")]
        public decimal? ChangementClimatique { get; set; }

        [JsonPropertyName("Épuisement_des_ressources_eau")]
        public decimal? EpuisementRessourcesEau { get; set; }
    }
}
