using IdentityManagement.Application.Responses.Tenants;

namespace IdentityManagement.Application.Services
{
    public interface ITenantResolutionService
    {
        Task<TenantResolutionResponse?> ResolveByTenantAndApplication(Guid tenantId, string applicationId, CancellationToken cancellationToken = default);

        Task<TenantResolutionResponse?> ResolveByApiKey(string apiKey, string? applicationId, CancellationToken cancellationToken = default);
    }
}
