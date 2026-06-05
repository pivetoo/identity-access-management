using IdentityManagement.Application.Requests.SystemIntegrations;
using IdentityManagement.Application.Responses.SystemIntegrations;

namespace IdentityManagement.Application.Services
{
    public interface ISystemIntegrationService
    {
        Task<IReadOnlyCollection<SystemIntegrationResponse>> GetBySystemApplicationAsync(long systemApplicationId, CancellationToken cancellationToken = default);

        Task<SystemIntegrationResponse> CreateAsync(UpsertSystemIntegrationRequest request, CancellationToken cancellationToken = default);

        Task<SystemIntegrationResponse> UpdateAsync(long id, UpsertSystemIntegrationRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
