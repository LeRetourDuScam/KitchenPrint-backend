using System.ComponentModel.DataAnnotations;

namespace KitchenPrint.Core.Models
{
    public class DeleteAccountRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
