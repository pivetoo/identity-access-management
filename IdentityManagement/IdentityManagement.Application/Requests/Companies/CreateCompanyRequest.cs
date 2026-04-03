using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Companies
{
    public class CreateCompanyRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string LegalName { get; set; } = string.Empty;

        [StringLength(200, MinimumLength = 2)]
        public string TradeName { get; set; } = string.Empty;

        [Required]
        [StringLength(18, MinimumLength = 11)]
        public string Document { get; set; } = string.Empty;

        [Required]
        [StringLength(150, MinimumLength = 5)]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^$|^[0-9()\-\s+]{8,30}$", ErrorMessage = "The PhoneNumber field must contain between 8 and 30 valid phone characters.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
