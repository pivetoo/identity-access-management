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

        public Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, string? externalCustomerId, CancellationToken ct = default)
        {
            return Task.FromResult<GatewaySubscriptionResult?>(null);
        }

        public Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
