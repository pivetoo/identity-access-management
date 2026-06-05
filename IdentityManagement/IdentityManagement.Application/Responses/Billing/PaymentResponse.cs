using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.Billing
{
    public class PaymentResponse
    {
        public long Id { get; set; }

        public string ExternalPaymentId { get; set; } = string.Empty;

        public long? CompanyId { get; set; }

        public string? CompanyName { get; set; }

        public string? ExternalSubscriptionId { get; set; }

        public decimal Value { get; set; }

        public string? BillingType { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTimeOffset? DueDate { get; set; }

        public DateTimeOffset? PaidDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
