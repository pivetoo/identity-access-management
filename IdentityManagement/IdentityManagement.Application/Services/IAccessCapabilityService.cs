using Archon.Core.Access;
using IdentityManagement.Application.Responses.AccessCapabilities;
using IdentityManagement.Application.Responses.AccessResources;

namespace IdentityManagement.Application.Services
{
    public interface IAccessCapabilityService
    {
        Task<AccessResourceSyncResponse> SyncCapabilities(AccessCapabilitySyncRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AccessCapabilityResponse>> GetActiveByContract(long contractId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AccessCapabilityResponse>> GetActiveBySystemApplication(long systemApplicationId, CancellationToken cancellationToken = default);
    }
}
