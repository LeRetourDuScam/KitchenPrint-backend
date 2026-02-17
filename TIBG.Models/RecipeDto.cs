using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    /// <summary>
    /// DTO for recipe calculation request
    /// </summary>
    public class RecipeCalculationRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, 100)]
        public int Servings { get; set; } = 1;

        [Required]
        [MinLength(1)]
        public List<RecipeIngredientRequest> Ingredients { get; set; } = new();
    }

    /// <summary>
    /// DTO for recipe ingredient in request
    /// </summary>
    public class RecipeIngredientRequest
    {
        [Required]
        public int IngredientId { get; set; }

        [Required]
        [Range(1, 100000)]
        public decimal QuantityGrams { get; set; }
    }

    /// <summary>
    /// DTO for recipe calculation response
    /// </summary>
    public class RecipeCalculationResponse
    {
        public int? RecipeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Servings { get; set; }
        public decimal TotalCarbonKg { get; set; }
        public decimal TotalWaterLiters { get; set; }
        public string EcoScore { get; set; } = string.Empty;
        public List<RecipeIngredientDto> Ingredients { get; set; } = new();
        public EnvironmentalEquivalents Equivalents { get; set; } = new();
    }

    /// <summary>
    /// DTO for recipe ingredient in response
    /// </summary>
    public class RecipeIngredientDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal QuantityGrams { get; set; }
        public decimal CarbonContributionKg { get; set; }
        public decimal WaterContributionLiters { get; set; }
        public decimal CarbonPercentage { get; set; }
        public decimal WaterPercentage { get; set; }
    }

    /// <summary>
    /// Environmental impact equivalents for better understanding
    /// </summary>
    public class EnvironmentalEquivalents
    {
        /// <summary>
        /// Equivalent distance in km driven by car
        /// </summary>
        public decimal CarKilometers { get; set; }

        /// <summary>
        /// Equivalent number of smartphone charges
        /// </summary>
        public int SmartphoneCharges { get; set; }

        /// <summary>
        /// Equivalent number of 5-minute showers
        /// </summary>
        public int Showers { get; set; }

        /// <summary>
        /// Equivalent number of days of drinking water
        /// </summary>
        public decimal DaysOfDrinkingWater { get; set; }
    }

    /// <summary>
    /// DTO for saved recipe response
    /// </summary>
    public class RecipeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Servings { get; set; }
        public decimal TotalCarbonKg { get; set; }
        public decimal TotalWaterLiters { get; set; }
        public string? EcoScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RecipeIngredientDto> Ingredients { get; set; } = new();
    }
}
