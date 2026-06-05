using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.Billing
{
    public class SubscriptionResponse
    {
        public long Id { get; set; }

        public long CompanyId { get; set; }

        public long PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public SubscriptionStatus Status { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? TrialEndsAt { get; set; }

        public DateTimeOffset CurrentPeriodStart { get; set; }

        public DateTimeOffset CurrentPeriodEnd { get; set; }

        public DateTimeOffset? CanceledAt { get; set; }

        public string? ProviderName { get; set; }

        public bool IsBlocked { get; set; }

        public SubscriptionBlockReason BlockReason { get; set; }

        public DateTimeOffset? BlockedAt { get; set; }
    }
}
