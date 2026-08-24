namespace IdentityManagement.Application.Services
{
    public interface IBillingGateway
    {
        Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default);

        Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default);

        Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, decimal priceAmount, string? externalCustomerId, CancellationToken ct = default);

        /// <summary>
        /// Abre um checkout hospedado de assinatura RECORRENTE no cartao.
        ///
        /// Os dados de cartao nunca passam por aqui: o cliente e redirecionado para a pagina do
        /// provedor. Por isso a operacao devolve so uma URL — quem confirma o pagamento e o webhook.
        /// </summary>
        Task<GatewayCheckoutResult> CreateRecurringCardCheckoutAsync(long companyId, long planId, decimal priceAmount, CancellationToken ct = default);

        /// <summary>
        /// Assinaturas ATIVAS de cartao do cliente no provedor. O evento de checkout pago nao traz o
        /// id da assinatura que ele criou, entao a correlacao e feita consultando o cliente depois.
        /// </summary>
        Task<IReadOnlyList<GatewaySubscriptionSummary>> ListActiveCardSubscriptionsAsync(string externalCustomerId, CancellationToken ct = default);

        /// <summary>
        /// Cobranca em aberto (pendente ou vencida) da assinatura, com o que o cliente precisa para
        /// pagar: link da fatura e, no PIX, o copia e cola e o QR code.
        ///
        /// Sem isso a agencia nao tem onde pagar dentro do sistema — sobra so o e-mail do provedor.
        /// </summary>
        Task<GatewayPendingCharge?> GetPendingChargeAsync(string externalSubscriptionId, CancellationToken ct = default);
    }

    public sealed record GatewaySubscriptionResult(string ExternalSubscriptionId, string? ExternalCustomerId, string PaymentMethod);

    public sealed record GatewayCheckoutResult(string CheckoutId, string CheckoutUrl, DateTimeOffset ExpiresAt);

    public sealed record GatewaySubscriptionSummary(string ExternalSubscriptionId, string BillingType, string Status, DateTimeOffset? CreatedAt);

    public sealed record GatewayPendingCharge(
        string ExternalPaymentId,
        decimal Value,
        DateTimeOffset? DueDate,
        string BillingType,
        string Status,
        string? InvoiceUrl,
        string? PixPayload,
        string? PixQrCodeBase64);
}
