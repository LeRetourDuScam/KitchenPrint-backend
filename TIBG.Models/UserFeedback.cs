using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TIBG.Models
{
    /// <summary>
    /// User feedback on recipe calculations and suggestions
    /// </summary>
    public class UserFeedback
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Rating: excellent, good, average, poor
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Rating { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Comment { get; set; }

        /// <summary>
        /// Recipe ID this feedback relates to
        /// </summary>
        public int? RecipeId { get; set; }

        /// <summary>
        /// User ID who provided feedback
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Feedback context: calculation, suggestion, alternative
        /// </summary>
        [MaxLength(100)]
        public string? Context { get; set; }

        /// <summary>
        /// Carbon score of the recipe when feedback was given
        /// </summary>
        public decimal? CarbonScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RecipeId))]
        public virtual Recipe? Recipe { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
