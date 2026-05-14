using IdentityManagement.Application.Requests.Tenants;
using IdentityManagement.Application.Responses.Tenants;

namespace IdentityManagement.Application.Services
{
    public interface ITenantDatabaseService
    {
        Task<IReadOnlyCollection<TenantDatabaseResponse>> GetAll(CancellationToken cancellationToken = default);

        Task<TenantDatabaseResponse?> GetById(long id, CancellationToken cancellationToken = default);

        Task<TenantDatabaseResponse> Create(CreateTenantDatabaseRequest request, CancellationToken cancellationToken = default);

        Task<TenantDatabaseResponse> Update(long id, UpdateTenantDatabaseRequest request, CancellationToken cancellationToken = default);

        Task<TenantDatabaseResponse> SetActive(long id, bool isActive, CancellationToken cancellationToken = default);
    }
}
