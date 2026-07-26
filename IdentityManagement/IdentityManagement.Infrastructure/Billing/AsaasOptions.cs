namespace IdentityManagement.Infrastructure.Billing
{
    public sealed class AsaasOptions
    {
        public const string SectionName = "Asaas";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public string WebhookToken { get; set; } = string.Empty;

        public string BillingType { get; set; } = "PIX";

        /// <summary>
        /// Aceita webhook sem validar token. Existe para desenvolvimento local e precisa ser LIGADA
        /// de proposito — antes o mesmo efeito vinha de nao configurar o token, o que transformava
        /// um esquecimento de deploy em endpoint anonimo que altera estado de assinatura.
        /// </summary>
        public bool AllowUnauthenticatedWebhook { get; set; }
    }
}
