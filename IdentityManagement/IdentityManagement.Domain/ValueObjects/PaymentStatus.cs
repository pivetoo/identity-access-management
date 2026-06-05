namespace IdentityManagement.Domain.ValueObjects
{
    public enum PaymentStatus
    {
        Pending = 1,
        Confirmed = 2,
        Received = 3,
        Overdue = 4,
        Refunded = 5,
        ChargebackRequested = 6,
        Deleted = 7
    }
}
