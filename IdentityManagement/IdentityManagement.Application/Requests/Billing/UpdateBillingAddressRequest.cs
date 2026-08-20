using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Billing
{
    public sealed class UpdateBillingAddressRequest
    {
        [Required]
        [StringLength(9, MinimumLength = 8)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Number { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Complement { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string District { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string State { get; set; } = string.Empty;
    }
}
