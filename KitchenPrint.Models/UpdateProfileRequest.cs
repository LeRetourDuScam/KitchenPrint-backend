using System.ComponentModel.DataAnnotations;

namespace KitchenPrint.Core.Models
{
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
    }
}
