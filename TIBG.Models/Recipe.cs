using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TIBG.Models
{
    /// <summary>
    /// Recipe entity representing a user's recipe with environmental impact
    /// </summary>
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Total carbon footprint in kg CO2eq
        /// </summary>
        public decimal TotalCarbonKg { get; set; }

        /// <summary>
        /// Total water footprint in liters
        /// </summary>
        public decimal TotalWaterLiters { get; set; }

        /// <summary>
        /// Eco score from A (best) to E (worst)
        /// </summary>
        [MaxLength(1)]
        public string? EcoScore { get; set; }

        /// <summary>
        /// Number of servings
        /// </summary>
        public int Servings { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsPublic { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
