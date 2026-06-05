namespace IdentityManagement.Application.Services
{
    public interface IBillingGateway
    {
        Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default);

        Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, string? externalCustomerId, CancellationToken ct = default);

        Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default);
    }

    public sealed record GatewaySubscriptionResult(string ExternalSubscriptionId, string? ExternalCustomerId);
}
