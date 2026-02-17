using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TIBG.Models
{
    /// <summary>
    /// Junction table linking recipes to ingredients with quantities
    /// </summary>
    public class RecipeIngredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecipeId { get; set; }

        [Required]
        public int IngredientId { get; set; }

        /// <summary>
        /// Quantity in grams
        /// </summary>
        [Required]
        public decimal QuantityGrams { get; set; }

        /// <summary>
        /// Carbon contribution from this ingredient in kg CO2eq
        /// </summary>
        public decimal CarbonContributionKg { get; set; }

        /// <summary>
        /// Water contribution from this ingredient in liters
        /// </summary>
        public decimal WaterContributionLiters { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RecipeId))]
        public virtual Recipe Recipe { get; set; } = null!;

        [ForeignKey(nameof(IngredientId))]
        public virtual Ingredient Ingredient { get; set; } = null!;
    }
}
