using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    // Registro local de cobranca (charge) espelhando o pagamento no provedor (Asaas).
    public class Payment : Entity
    {
        public string ExternalPaymentId { get; private set; } = string.Empty;

        public long? CompanyId { get; private set; }

        public long? SubscriptionId { get; private set; }

        public string? ExternalSubscriptionId { get; private set; }

        public decimal Value { get; private set; }

        public string? BillingType { get; private set; }

        public PaymentStatus Status { get; private set; }

        public DateTimeOffset? DueDate { get; private set; }

        public DateTimeOffset? PaidDate { get; private set; }

        private Payment()
        {
        }

        public Payment(string externalPaymentId, decimal value, PaymentStatus status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(externalPaymentId);

            ExternalPaymentId = externalPaymentId.Trim();
            Value = value;
            Status = status;
        }

        public void SetSubscription(long? subscriptionId, long? companyId, string? externalSubscriptionId)
        {
            SubscriptionId = subscriptionId;
            CompanyId = companyId;
            ExternalSubscriptionId = externalSubscriptionId;
        }

        public void SetDetails(string? billingType, DateTimeOffset? dueDate)
        {
            BillingType = billingType?.Trim();
            DueDate = dueDate;
        }

        public void UpdateStatus(PaymentStatus status, DateTimeOffset? paidDate)
        {
            Status = status;

            if (paidDate.HasValue)
            {
                PaidDate = paidDate;
            }
        }
    }
}
