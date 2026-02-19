using System.ComponentModel.DataAnnotations;

namespace KitchenPrint.Core.Models
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
