using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;

namespace IdentityManagement.Application.Services
{
    /// <summary>
    /// Superficie de cobranca voltada ao PROPRIO tenant (a agencia), separada do
    /// <c>SubscriptionsController</c>, que e do console de administracao da Mainstay.
    ///
    /// Tudo aqui e enderecado por <c>tenantId</c>, nunca por id de empresa: quem chama e o sistema
    /// consumidor com a chave de aplicacao, e tenantId e o que ele tem na claim ja validada do
    /// usuario. Assim o consumidor nao consegue apontar para uma empresa que nao e a dele.
    /// </summary>
    public interface ITenantBillingService
    {
        Task<TenantSubscriptionResponse> GetSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);

        Task<TenantSubscriptionResponse> UpdateBillingAddressAsync(Guid tenantId, UpdateBillingAddressRequest request, CancellationToken cancellationToken = default);

        Task<TenantCheckoutResponse> StartCardCheckoutAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Volta a cobranca para PIX: cancela a assinatura de cartao no provedor e cria outra em
        /// PIX. Caminho simetrico ao do checkout — sem ele, sair do cartao viraria chamado no
        /// suporte para uma operacao que o cliente deveria resolver sozinho.
        /// </summary>
        Task<TenantSubscriptionResponse> SwitchToPixAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }
}
