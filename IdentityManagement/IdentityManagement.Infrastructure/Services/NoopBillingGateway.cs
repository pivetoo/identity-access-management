using IdentityManagement.Application.Services;

namespace IdentityManagement.Infrastructure.Services
{
    // Implementacao placeholder ate que um gateway real (Asaas, Iugu, etc.) seja integrado.
    // Todos os metodos sao no-ops e retornam nulo.
    public sealed class NoopBillingGateway : IBillingGateway
    {
        public Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, decimal priceAmount, string? externalCustomerId, CancellationToken ct = default)
        {
            return Task.FromResult<GatewaySubscriptionResult?>(null);
        }

        public Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        // Sem gateway configurado nao existe pagina de checkout para onde mandar o cliente. Falhar
        // aqui e melhor que devolver URL vazia: o erro aparece no ato, e nao numa tela em branco.
        public Task<GatewayCheckoutResult> CreateRecurringCardCheckoutAsync(long companyId, long planId, decimal priceAmount, CancellationToken ct = default)
        {
            throw new InvalidOperationException("billing.gateway.notConfigured");
        }

        public Task<IReadOnlyList<GatewaySubscriptionSummary>> ListActiveCardSubscriptionsAsync(string externalCustomerId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<GatewaySubscriptionSummary>>([]);
        }

        public Task<GatewayPendingCharge?> GetPendingChargeAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            return Task.FromResult<GatewayPendingCharge?>(null);
        }
    }
}
