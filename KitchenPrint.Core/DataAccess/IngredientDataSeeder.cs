using ExcelDataReader;
using KitchenPrint.Core.Models;
using KitchenPrint.ENTITIES;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KitchenPrint.API.Core.DataAccess
{
    /// <summary>
    /// Seeds the database with ingredient data from the AGRIBALYSE 3.2 Excel file.
    /// CO2 values in kg CO2eq per kg of product (column 14).
    /// Water values in m³ deprivation per kg of product (column 27), converted to liters (×1000).
    /// Source: AGRIBALYSE 3.2 - ADEME, August 2025.
    /// </summary>
    public static class IngredientDataSeeder
    {
        public static async Task SeedAsync(kitchenPrintDbContext context, ILogger logger, string? excelFilePath = null)
        {
            if (await context.Ingredients.AnyAsync())
            {
                logger.LogInformation("Ingredients already seeded. Skipping.");
                return;
            }

            List<Ingredient> ingredients;

            if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
            {
                logger.LogInformation("Seeding ingredient database from AGRIBALYSE Excel: {Path}", excelFilePath);
                ingredients = ParseAgribalyseExcel(excelFilePath, logger);
            }
            else
            {
                logger.LogWarning("AGRIBALYSE Excel file not found at '{Path}'. Using fallback hardcoded data.", excelFilePath);
                ingredients = GetFallbackIngredients();
            }

            await context.Ingredients.AddRangeAsync(ingredients);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} ingredients successfully.", ingredients.Count);
        }

        private static List<Ingredient> ParseAgribalyseExcel(string filePath, ILogger logger)
        {
            // Required for ExcelDataReader on .NET Core
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var ingredients = new List<Ingredient>();
            var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Navigate to the "Synthese" sheet
            do
            {
                if (reader.Name == "Synthese") break;
            } while (reader.NextResult());

            if (reader.Name != "Synthese")
            {
                logger.LogError("Sheet 'Synthese' not found in AGRIBALYSE Excel file.");
                return ingredients;
            }

            // Skip 3 header rows (notices, category headers, column headers)
            for (int i = 0; i < 3; i++) reader.Read();

            int rowNumber = 3;
            while (reader.Read())
            {
                rowNumber++;
                try
                {
                    // Col index 0: Code AGB
                    var code = reader.GetValue(0)?.ToString()?.Trim();
                    // Col index 4: Nom du Produit en Français
                    var name = reader.GetValue(4)?.ToString()?.Trim();
                    // Col index 2: Groupe d'aliment
                    var category = reader.GetValue(2)?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                        continue;

                    var externalId = "agrib_" + code;
                    if (externalId.Length > 100)
                        externalId = externalId[..100];

                    if (!seenExternalIds.Add(externalId))
                        continue;

                    // Col index 13: kg CO2 eq / kg product
                    var carbonRaw = ToDouble(reader.GetValue(13));
                    var carbon = (decimal)Math.Max(0, carbonRaw);

                    // Col index 26: m³ depriv. / kg product → converted to L/kg (×1000)
                    var waterRaw = ToDouble(reader.GetValue(26));
                    var water = (decimal)Math.Max(0, waterRaw * 1000.0);

                    // Col index 6: season code — 0: hors saison, 1: de saison, 2: mix
                    var seasonCode = (int)ToDouble(reader.GetValue(6));
                    var season = seasonCode switch
                    {
                        0 => "hors-saison",
                        1 => "seasonal",
                        _ => "all-year"
                    };

                    // Col index 7: code avion — 1 means imported by air
                    var avion = (int)ToDouble(reader.GetValue(7));
                    var origin = avion == 1 ? "imported" : "national";

                    var categoryFormatted = string.IsNullOrEmpty(category)
                        ? "Autres"
                        : char.ToUpper(category[0]) + category[1..];

                    ingredients.Add(new Ingredient
                    {
                        Name = name.Length > 200 ? name[..200] : name,
                        Category = categoryFormatted.Length > 100 ? categoryFormatted[..100] : categoryFormatted,
                        CarbonEmissionKgPerKg = carbon,
                        WaterFootprintLitersPerKg = water,
                        Season = season,
                        Origin = origin,
                        ApiSource = "Agribalyse 3.2",
                        ExternalId = externalId,
                        IsActive = true
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse row {Row} from AGRIBALYSE Excel, skipping.", rowNumber);
                }
            }

            logger.LogInformation("Parsed {Count} ingredients from AGRIBALYSE Excel.", ingredients.Count);
            return ingredients;
        }

        private static double ToDouble(object? value)
        {
            if (value == null) return 0;
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            if (double.TryParse(value.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        /// <summary>
        /// Fallback data used when the AGRIBALYSE Excel file is not available.
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
}
