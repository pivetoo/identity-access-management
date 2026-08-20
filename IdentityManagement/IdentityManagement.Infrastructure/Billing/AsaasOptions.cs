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

        /// <summary>
        /// Para onde o Asaas devolve o cliente ao fim do checkout. Vem de CONFIGURACAO, nunca do
        /// corpo da requisicao: URL de retorno vinda do cliente e redirecionamento aberto.
        /// </summary>
        public string CheckoutSuccessUrl { get; set; } = string.Empty;

        public string CheckoutCancelUrl { get; set; } = string.Empty;

        public string CheckoutExpiredUrl { get; set; } = string.Empty;

        public int CheckoutMinutesToExpire { get; set; } = 60;
    }
}
