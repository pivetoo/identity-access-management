using IdentityManagement.Application.Responses.Tenants;

namespace IdentityManagement.Application.Services
{
    public interface ITenantResolutionService
    {
        Task<TenantResolutionResponse?> ResolveByTenantAndApplication(Guid tenantId, string applicationId, CancellationToken cancellationToken = default);

        Task<TenantResolutionResponse?> ResolveByIntegrationSecret(string integrationSecret, string? applicationId, CancellationToken cancellationToken = default);
    }
}
