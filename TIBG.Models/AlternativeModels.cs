using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Response containing alternative ingredient suggestions
    /// </summary>
    public class AlternativeSuggestionsResponse
    {
        public int OriginalIngredientId { get; set; }
        public string OriginalIngredientName { get; set; } = string.Empty;
        public decimal OriginalCarbonKgPerKg { get; set; }
        public decimal OriginalWaterLitersPerKg { get; set; }
        public List<AlternativeIngredient> Alternatives { get; set; } = new();
        public string? AiInsight { get; set; }
    }

    /// <summary>
    /// An alternative ingredient with comparison data
    /// </summary>
    public class AlternativeIngredient
    {
        public int? IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal CarbonEmissionKgPerKg { get; set; }
        public decimal WaterFootprintLitersPerKg { get; set; }
        public decimal CarbonReductionPercent { get; set; }
        public decimal WaterReductionPercent { get; set; }
        public string? Season { get; set; }
        public string? Origin { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Nutritional information for an ingredient (per 100g)
    /// </summary>
    public class NutritionInfo
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal CaloriesKcal { get; set; }
        public decimal ProteinG { get; set; }
        public decimal CarbohydratesG { get; set; }
        public decimal FatG { get; set; }
        public decimal FiberG { get; set; }
        public decimal SaltG { get; set; }
        public string? Source { get; set; }
    }

    /// <summary>
    /// Nutritional data for an entire recipe
    /// </summary>
    public class RecipeNutritionResponse
    {
        public decimal TotalCalories { get; set; }
        public decimal TotalProteinG { get; set; }
        public decimal TotalCarbohydratesG { get; set; }
        public decimal TotalFatG { get; set; }
        public decimal TotalFiberG { get; set; }
        public List<IngredientNutritionDetail> IngredientDetails { get; set; } = new();
    }

    /// <summary>
    /// Nutritional contribution per ingredient in a recipe
    /// </summary>
    public class IngredientNutritionDetail
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal QuantityGrams { get; set; }
        public decimal CaloriesKcal { get; set; }
        public decimal ProteinG { get; set; }
        public decimal CarbohydratesG { get; set; }
        public decimal FatG { get; set; }
        public decimal FiberG { get; set; }
    }
}
