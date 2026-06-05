using System.ComponentModel.DataAnnotations;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.Billing
{
    public class UpdatePlanRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PriceAmount { get; set; }

        [Required]
        public BillingPeriod BillingPeriod { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string Currency { get; set; } = "BRL";

        [Range(0, int.MaxValue)]
        public int TrialDays { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
