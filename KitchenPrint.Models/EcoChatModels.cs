using System.ComponentModel.DataAnnotations;

namespace KitchenPrint.Core.Models
{
    /// <summary>
    /// Request for eco chat conversation
    /// </summary>
    public class EcoChatRequest
    {
        [Required]
        [StringLength(2000)]
        public string UserMessage { get; set; } = string.Empty;

        public string? RecipeName { get; set; }
        public decimal? TotalCarbonKg { get; set; }
        public decimal? TotalWaterLiters { get; set; }
        public string? EcoScore { get; set; }
        public int? Servings { get; set; }
        public List<EcoChatIngredient> Ingredients { get; set; } = new();
        public List<EcoChatMessage> ConversationHistory { get; set; } = new();

        [StringLength(5)]
        public string Language { get; set; } = "fr";
    }

    /// <summary>
    /// Simplified ingredient info for chat context
    /// </summary>
    public class EcoChatIngredient
    {
        public string Name { get; set; } = string.Empty;
        public decimal QuantityGrams { get; set; }
        public decimal CarbonContributionKg { get; set; }
        public decimal WaterContributionLiters { get; set; }
        public decimal CarbonPercentage { get; set; }
    }

    /// <summary>
    /// A message in the conversation history
    /// </summary>
    public class EcoChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from the eco chat
    /// </summary>
    public class EcoChatResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
