namespace IdentityManagement.Application.Requests.Billing
{
    public sealed class AsaasWebhookPayload
    {
        public string Id { get; set; } = string.Empty;

        public string Event { get; set; } = string.Empty;

        public AsaasPaymentInfo Payment { get; set; } = new AsaasPaymentInfo();
    }

    public sealed class AsaasPaymentInfo
    {
        public string Id { get; set; } = string.Empty;

        public string? Subscription { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal? Value { get; set; }

        public string? BillingType { get; set; }

        public string? DueDate { get; set; }

        public string? PaymentDate { get; set; }

        public string? ConfirmedDate { get; set; }

        public string? Customer { get; set; }
    }
}
