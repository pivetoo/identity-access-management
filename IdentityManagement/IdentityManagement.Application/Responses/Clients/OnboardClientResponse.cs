namespace IdentityManagement.Application.Responses.Clients
{
    public sealed class OnboardClientResponse
    {
        public long CompanyId { get; set; }
        public long[] ContractIds { get; set; } = Array.Empty<long>();
        public string[] DatabaseNames { get; set; } = Array.Empty<string>();
        public List<SystemBootstrapResult> BootstrapResults { get; set; } = new();
        public SubscriptionProvisionResult? Subscription { get; set; }
    }

    public sealed class SubscriptionProvisionResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public long? SubscriptionId { get; set; }
        public long? PlanId { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? TrialEndsAt { get; set; }
        public string? Detail { get; set; }
    }

    public sealed class SystemBootstrapResult
    {
        public string Audience { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string? Detail { get; set; }
    }
}
