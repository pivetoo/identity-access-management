namespace IdentityManagement.Domain.ValueObjects
{
    public enum SubscriptionStatus
    {
        Trialing = 1,
        Active = 2,
        PastDue = 3,
        Suspended = 4,
        Canceled = 5
    }
}
