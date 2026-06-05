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
    }
}
