using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TIBG.ENTITIES;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Seeds the database with comprehensive ingredient data based on Agribalyse/ADEME values.
    /// CO2 values in kg CO2eq per kg of product.
    /// Water footprint in liters per kg of product.
    /// Sources: Agribalyse 3.1.1, ADEME Base Carbone, Water Footprint Network.
    /// </summary>
    public static class IngredientDataSeeder
    {
        public static async Task SeedAsync(FytAiDbContext context, ILogger logger)
        {
            if (await context.Ingredients.AnyAsync())
            {
                logger.LogInformation("Ingredients already seeded. Skipping.");
                return;
            }

            logger.LogInformation("Seeding ingredient database with Agribalyse/ADEME data...");

            var ingredients = GetSeedIngredients();

            await context.Ingredients.AddRangeAsync(ingredients);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} ingredients successfully.", ingredients.Count);
        }

        private static List<Ingredient> GetSeedIngredients()
        {
            return new List<Ingredient>
            {
                // ===== VIANDES (Meats) =====
                new() { Name = "Bœuf (steak)", Category = "Viandes", CarbonEmissionKgPerKg = 27.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beef_steak" },
                new() { Name = "Bœuf (haché)", Category = "Viandes", CarbonEmissionKgPerKg = 25.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beef_ground" },
                new() { Name = "Bœuf (rôti)", Category = "Viandes", CarbonEmissionKgPerKg = 26.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beef_roast" },
                new() { Name = "Veau", Category = "Viandes", CarbonEmissionKgPerKg = 22.0m, WaterFootprintLitersPerKg = 15400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_veal" },
                new() { Name = "Agneau", Category = "Viandes", CarbonEmissionKgPerKg = 39.0m, WaterFootprintLitersPerKg = 10400m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lamb" },
                new() { Name = "Porc (côtelette)", Category = "Viandes", CarbonEmissionKgPerKg = 7.0m, WaterFootprintLitersPerKg = 6000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pork_chop" },
                new() { Name = "Porc (filet)", Category = "Viandes", CarbonEmissionKgPerKg = 6.5m, WaterFootprintLitersPerKg = 6000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pork_fillet" },
                new() { Name = "Porc (saucisse)", Category = "Viandes", CarbonEmissionKgPerKg = 7.5m, WaterFootprintLitersPerKg = 6000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pork_sausage" },
                new() { Name = "Jambon", Category = "Viandes", CarbonEmissionKgPerKg = 6.0m, WaterFootprintLitersPerKg = 5900m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_ham" },
                new() { Name = "Poulet", Category = "Viandes", CarbonEmissionKgPerKg = 5.1m, WaterFootprintLitersPerKg = 4300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_chicken" },
                new() { Name = "Poulet (filet)", Category = "Viandes", CarbonEmissionKgPerKg = 5.5m, WaterFootprintLitersPerKg = 4300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_chicken_breast" },
                new() { Name = "Dinde", Category = "Viandes", CarbonEmissionKgPerKg = 5.0m, WaterFootprintLitersPerKg = 4500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_turkey" },
                new() { Name = "Canard", Category = "Viandes", CarbonEmissionKgPerKg = 6.0m, WaterFootprintLitersPerKg = 4500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_duck" },
                new() { Name = "Lapin", Category = "Viandes", CarbonEmissionKgPerKg = 8.0m, WaterFootprintLitersPerKg = 6300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_rabbit" },

                // ===== POISSONS (Fish & Seafood) =====
                new() { Name = "Saumon", Category = "Poissons", CarbonEmissionKgPerKg = 11.9m, WaterFootprintLitersPerKg = 2000m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_salmon" },
                new() { Name = "Thon", Category = "Poissons", CarbonEmissionKgPerKg = 6.1m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_tuna" },
                new() { Name = "Cabillaud", Category = "Poissons", CarbonEmissionKgPerKg = 5.4m, WaterFootprintLitersPerKg = 1500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_cod" },
                new() { Name = "Sardine", Category = "Poissons", CarbonEmissionKgPerKg = 1.8m, WaterFootprintLitersPerKg = 800m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sardine" },
                new() { Name = "Maquereau", Category = "Poissons", CarbonEmissionKgPerKg = 1.7m, WaterFootprintLitersPerKg = 700m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_mackerel" },
                new() { Name = "Truite", Category = "Poissons", CarbonEmissionKgPerKg = 5.0m, WaterFootprintLitersPerKg = 1200m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_trout" },
                new() { Name = "Crevettes", Category = "Poissons", CarbonEmissionKgPerKg = 20.0m, WaterFootprintLitersPerKg = 3500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_shrimp" },
                new() { Name = "Moules", Category = "Poissons", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 200m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_mussels" },

                // ===== PRODUITS LAITIERS (Dairy) =====
                new() { Name = "Lait entier", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.4m, WaterFootprintLitersPerKg = 1020m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_whole_milk" },
                new() { Name = "Lait demi-écrémé", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.3m, WaterFootprintLitersPerKg = 1020m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_semi_skimmed_milk" },
                new() { Name = "Crème fraîche", Category = "Produits laitiers", CarbonEmissionKgPerKg = 3.5m, WaterFootprintLitersPerKg = 2500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cream" },
                new() { Name = "Beurre", Category = "Produits laitiers", CarbonEmissionKgPerKg = 9.0m, WaterFootprintLitersPerKg = 5550m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_butter" },
                new() { Name = "Fromage (emmental)", Category = "Produits laitiers", CarbonEmissionKgPerKg = 8.5m, WaterFootprintLitersPerKg = 5000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_emmental" },
                new() { Name = "Fromage (comté)", Category = "Produits laitiers", CarbonEmissionKgPerKg = 8.8m, WaterFootprintLitersPerKg = 5000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_comte" },
                new() { Name = "Fromage (camembert)", Category = "Produits laitiers", CarbonEmissionKgPerKg = 7.5m, WaterFootprintLitersPerKg = 5000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_camembert" },
                new() { Name = "Fromage (chèvre)", Category = "Produits laitiers", CarbonEmissionKgPerKg = 5.5m, WaterFootprintLitersPerKg = 3500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_goat_cheese" },
                new() { Name = "Mozzarella", Category = "Produits laitiers", CarbonEmissionKgPerKg = 7.0m, WaterFootprintLitersPerKg = 4800m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_mozzarella" },
                new() { Name = "Yaourt nature", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.7m, WaterFootprintLitersPerKg = 1100m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_yogurt" },
                new() { Name = "Fromage blanc", Category = "Produits laitiers", CarbonEmissionKgPerKg = 1.5m, WaterFootprintLitersPerKg = 1000m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_fromage_blanc" },

                // ===== ŒUFS (Eggs) =====
                new() { Name = "Œufs", Category = "Œufs", CarbonEmissionKgPerKg = 4.5m, WaterFootprintLitersPerKg = 3300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_eggs" },
                new() { Name = "Œufs bio", Category = "Œufs", CarbonEmissionKgPerKg = 3.8m, WaterFootprintLitersPerKg = 3300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_eggs_organic" },

                // ===== CÉRÉALES (Cereals & Grains) =====
                new() { Name = "Riz blanc", Category = "Céréales", CarbonEmissionKgPerKg = 3.6m, WaterFootprintLitersPerKg = 2500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_white_rice" },
                new() { Name = "Riz complet", Category = "Céréales", CarbonEmissionKgPerKg = 3.4m, WaterFootprintLitersPerKg = 2500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_brown_rice" },
                new() { Name = "Pâtes", Category = "Céréales", CarbonEmissionKgPerKg = 1.3m, WaterFootprintLitersPerKg = 1850m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pasta" },
                new() { Name = "Pain", Category = "Céréales", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 1600m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_bread" },
                new() { Name = "Pain complet", Category = "Céréales", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 1500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_whole_bread" },
                new() { Name = "Farine de blé", Category = "Céréales", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_wheat_flour" },
                new() { Name = "Semoule", Category = "Céréales", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_semolina" },
                new() { Name = "Quinoa", Category = "Céréales", CarbonEmissionKgPerKg = 1.2m, WaterFootprintLitersPerKg = 1500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_quinoa" },
                new() { Name = "Avoine", Category = "Céréales", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_oats" },
                new() { Name = "Maïs", Category = "Céréales", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 1200m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_corn" },
                new() { Name = "Boulgour", Category = "Céréales", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_bulgur" },

                // ===== LÉGUMES (Vegetables) =====
                new() { Name = "Tomate", Category = "Légumes", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 214m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_tomato" },
                new() { Name = "Tomate (hors saison)", Category = "Légumes", CarbonEmissionKgPerKg = 2.2m, WaterFootprintLitersPerKg = 214m, Season = "winter", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_tomato_offseason" },
                new() { Name = "Carotte", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 195m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_carrot" },
                new() { Name = "Pomme de terre", Category = "Légumes", CarbonEmissionKgPerKg = 0.2m, WaterFootprintLitersPerKg = 290m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_potato" },
                new() { Name = "Oignon", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 272m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_onion" },
                new() { Name = "Ail", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 590m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_garlic" },
                new() { Name = "Poivron", Category = "Légumes", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 379m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_bell_pepper" },
                new() { Name = "Courgette", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 353m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_zucchini" },
                new() { Name = "Aubergine", Category = "Légumes", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 362m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_eggplant" },
                new() { Name = "Concombre", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 353m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cucumber" },
                new() { Name = "Salade (laitue)", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 237m, Season = "spring,summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lettuce" },
                new() { Name = "Épinards", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 292m, Season = "spring,fall", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_spinach" },
                new() { Name = "Brocoli", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 284m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_broccoli" },
                new() { Name = "Chou-fleur", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 284m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cauliflower" },
                new() { Name = "Chou", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 237m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cabbage" },
                new() { Name = "Haricots verts", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 561m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_green_beans" },
                new() { Name = "Petits pois", Category = "Légumes", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 595m, Season = "spring,summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_peas" },
                new() { Name = "Champignons", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 200m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_mushrooms" },
                new() { Name = "Poireau", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 285m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_leek" },
                new() { Name = "Céleri", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 280m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_celery" },
                new() { Name = "Navet", Category = "Légumes", CarbonEmissionKgPerKg = 0.2m, WaterFootprintLitersPerKg = 250m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_turnip" },
                new() { Name = "Betterave", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 250m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_beetroot" },
                new() { Name = "Radis", Category = "Légumes", CarbonEmissionKgPerKg = 0.2m, WaterFootprintLitersPerKg = 200m, Season = "spring,summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_radish" },
                new() { Name = "Artichaut", Category = "Légumes", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 818m, Season = "spring,summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_artichoke" },
                new() { Name = "Asperges", Category = "Légumes", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 1471m, Season = "spring", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_asparagus" },
                new() { Name = "Avocat", Category = "Légumes", CarbonEmissionKgPerKg = 2.0m, WaterFootprintLitersPerKg = 1981m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_avocado" },
                new() { Name = "Patate douce", Category = "Légumes", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 425m, Season = "fall,winter", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_sweet_potato" },

                // ===== LÉGUMINEUSES (Legumes) =====
                new() { Name = "Lentilles", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 5874m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lentils" },
                new() { Name = "Lentilles corail", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 5874m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_red_lentils" },
                new() { Name = "Pois chiches", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 4177m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_chickpeas" },
                new() { Name = "Haricots blancs", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 5053m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_white_beans" },
                new() { Name = "Haricots rouges", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 5053m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_red_beans" },
                new() { Name = "Tofu", Category = "Légumineuses", CarbonEmissionKgPerKg = 2.0m, WaterFootprintLitersPerKg = 2523m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_tofu" },
                new() { Name = "Soja", Category = "Légumineuses", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 2145m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_soy" },
                new() { Name = "Fèves", Category = "Légumineuses", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 4055m, Season = "spring", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_fava_beans" },

                // ===== FRUITS =====
                new() { Name = "Pomme", Category = "Fruits", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 822m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_apple" },
                new() { Name = "Poire", Category = "Fruits", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 922m, Season = "fall,winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_pear" },
                new() { Name = "Banane", Category = "Fruits", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 790m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_banana" },
                new() { Name = "Orange", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 560m, Season = "winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_orange" },
                new() { Name = "Citron", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 560m, Season = "winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_lemon" },
                new() { Name = "Fraise", Category = "Fruits", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 347m, Season = "spring,summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_strawberry" },
                new() { Name = "Fraise (hors saison)", Category = "Fruits", CarbonEmissionKgPerKg = 2.5m, WaterFootprintLitersPerKg = 347m, Season = "winter", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_strawberry_offseason" },
                new() { Name = "Raisin", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 608m, Season = "fall", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_grapes" },
                new() { Name = "Pêche", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 910m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_peach" },
                new() { Name = "Abricot", Category = "Fruits", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 910m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_apricot" },
                new() { Name = "Cerise", Category = "Fruits", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 560m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_cherry" },
                new() { Name = "Mangue", Category = "Fruits", CarbonEmissionKgPerKg = 3.0m, WaterFootprintLitersPerKg = 1800m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_mango" },
                new() { Name = "Ananas", Category = "Fruits", CarbonEmissionKgPerKg = 1.5m, WaterFootprintLitersPerKg = 255m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_pineapple" },
                new() { Name = "Kiwi", Category = "Fruits", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 514m, Season = "winter", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_kiwi" },
                new() { Name = "Melon", Category = "Fruits", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 235m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_melon" },
                new() { Name = "Pastèque", Category = "Fruits", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 235m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_watermelon" },
                new() { Name = "Myrtilles", Category = "Fruits", CarbonEmissionKgPerKg = 0.7m, WaterFootprintLitersPerKg = 845m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_blueberries" },
                new() { Name = "Framboises", Category = "Fruits", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 413m, Season = "summer", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_raspberries" },

                // ===== HUILES (Oils & Fats) =====
                new() { Name = "Huile d'olive", Category = "Huiles", CarbonEmissionKgPerKg = 3.2m, WaterFootprintLitersPerKg = 14500m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_olive_oil" },
                new() { Name = "Huile de tournesol", Category = "Huiles", CarbonEmissionKgPerKg = 2.5m, WaterFootprintLitersPerKg = 6800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sunflower_oil" },
                new() { Name = "Huile de colza", Category = "Huiles", CarbonEmissionKgPerKg = 2.2m, WaterFootprintLitersPerKg = 4300m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_rapeseed_oil" },
                new() { Name = "Huile de coco", Category = "Huiles", CarbonEmissionKgPerKg = 2.5m, WaterFootprintLitersPerKg = 2100m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_coconut_oil" },
                new() { Name = "Margarine", Category = "Huiles", CarbonEmissionKgPerKg = 3.0m, WaterFootprintLitersPerKg = 3500m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_margarine" },

                // ===== ÉPICES & CONDIMENTS =====
                new() { Name = "Sel", Category = "Condiments", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 50m, Season = "all-year", Origin = "national", ApiSource = "ADEME", ExternalId = "ademe_salt" },
                new() { Name = "Poivre", Category = "Condiments", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 7000m, Season = "all-year", Origin = "imported", ApiSource = "ADEME", ExternalId = "ademe_pepper" },
                new() { Name = "Sucre", Category = "Condiments", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 1782m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_sugar" },
                new() { Name = "Miel", Category = "Condiments", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 800m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_honey" },
                new() { Name = "Vinaigre", Category = "Condiments", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 300m, Season = "all-year", Origin = "national", ApiSource = "ADEME", ExternalId = "ademe_vinegar" },
                new() { Name = "Moutarde", Category = "Condiments", CarbonEmissionKgPerKg = 0.6m, WaterFootprintLitersPerKg = 500m, Season = "all-year", Origin = "national", ApiSource = "ADEME", ExternalId = "ademe_mustard" },
                new() { Name = "Sauce tomate", Category = "Condiments", CarbonEmissionKgPerKg = 0.8m, WaterFootprintLitersPerKg = 350m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_tomato_sauce" },
                new() { Name = "Sauce soja", Category = "Condiments", CarbonEmissionKgPerKg = 0.9m, WaterFootprintLitersPerKg = 600m, Season = "all-year", Origin = "imported", ApiSource = "ADEME", ExternalId = "ademe_soy_sauce" },

                // ===== BOISSONS & AUTRES =====
                new() { Name = "Chocolat noir", Category = "Autres", CarbonEmissionKgPerKg = 4.2m, WaterFootprintLitersPerKg = 17196m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_dark_chocolate" },
                new() { Name = "Chocolat au lait", Category = "Autres", CarbonEmissionKgPerKg = 4.5m, WaterFootprintLitersPerKg = 17196m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_milk_chocolate" },
                new() { Name = "Café", Category = "Autres", CarbonEmissionKgPerKg = 5.7m, WaterFootprintLitersPerKg = 15897m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_coffee" },
                new() { Name = "Thé", Category = "Autres", CarbonEmissionKgPerKg = 1.5m, WaterFootprintLitersPerKg = 8860m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_tea" },
                new() { Name = "Noix", Category = "Fruits à coque", CarbonEmissionKgPerKg = 1.2m, WaterFootprintLitersPerKg = 9063m, Season = "fall", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_walnuts" },
                new() { Name = "Amandes", Category = "Fruits à coque", CarbonEmissionKgPerKg = 1.4m, WaterFootprintLitersPerKg = 16194m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_almonds" },
                new() { Name = "Noisettes", Category = "Fruits à coque", CarbonEmissionKgPerKg = 1.0m, WaterFootprintLitersPerKg = 5000m, Season = "fall", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_hazelnuts" },
                new() { Name = "Lait d'avoine", Category = "Boissons végétales", CarbonEmissionKgPerKg = 0.3m, WaterFootprintLitersPerKg = 480m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_oat_milk" },
                new() { Name = "Lait de soja", Category = "Boissons végétales", CarbonEmissionKgPerKg = 0.4m, WaterFootprintLitersPerKg = 270m, Season = "all-year", Origin = "national", ApiSource = "Agribalyse", ExternalId = "agri_soy_milk" },
                new() { Name = "Lait d'amande", Category = "Boissons végétales", CarbonEmissionKgPerKg = 0.5m, WaterFootprintLitersPerKg = 5650m, Season = "all-year", Origin = "imported", ApiSource = "Agribalyse", ExternalId = "agri_almond_milk" },
            };
        }
    }
}
