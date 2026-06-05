namespace IdentityManagement.Infrastructure.Billing
{
    // Configuracao do gateway Asaas, vinculada a secao "Asaas" da configuracao.
    public sealed class AsaasOptions
    {
        public const string SectionName = "Asaas";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string WebhookToken { get; set; } = string.Empty;

        public string BillingType { get; set; } = "PIX";
    }
}
