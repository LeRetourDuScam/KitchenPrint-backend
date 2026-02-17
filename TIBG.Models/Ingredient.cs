using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// Ingredient entity with environmental impact data
    /// </summary>
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// CO2 emission in kg per kg of ingredient
        /// </summary>
        public decimal CarbonEmissionKgPerKg { get; set; }

        /// <summary>
        /// Water footprint in liters per kg of ingredient
        /// </summary>
        public decimal WaterFootprintLitersPerKg { get; set; }

        /// <summary>
        /// Seasonality: spring, summer, fall, winter, all-year
        /// </summary>
        [MaxLength(50)]
        public string? Season { get; set; }

        /// <summary>
        /// Origin: local, national, imported
        /// </summary>
        [MaxLength(50)]
        public string? Origin { get; set; }

        /// <summary>
        /// Data source: Agribalyse, OpenFoodFacts, Manual
        /// </summary>
        [MaxLength(50)]
        public string? ApiSource { get; set; }

        /// <summary>
        /// External API identifier
        /// </summary>
        [MaxLength(100)]
        public string? ExternalId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Nutritional data in JSON format (from Open Food Facts)
        /// </summary>
        public string? NutritionData { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
