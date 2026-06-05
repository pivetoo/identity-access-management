using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.Billing
{
    public class PlanResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal PriceAmount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public BillingPeriod BillingPeriod { get; set; }

        public int TrialDays { get; set; }

        public bool IsActive { get; set; }
    }
}
