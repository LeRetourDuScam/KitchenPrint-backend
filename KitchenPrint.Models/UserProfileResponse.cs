using System;

namespace KitchenPrint.Core.Models
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int RecipeCount { get; set; }
    }
}
