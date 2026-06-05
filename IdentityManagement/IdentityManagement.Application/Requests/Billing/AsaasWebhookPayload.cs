using System.Text.Json.Serialization;

namespace IdentityManagement.Application.Requests.Billing
{
    // Payload de webhook do Asaas (apenas eventos de CHARGE/payment).
    // Os nomes de propriedade JSON do Asaas sao minusculos; mapeados via JsonPropertyName.
    public sealed class AsaasWebhookPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("payment")]
        public AsaasPaymentInfo Payment { get; set; } = new AsaasPaymentInfo();
    }

    public sealed class AsaasPaymentInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("subscription")]
        public string? Subscription { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
