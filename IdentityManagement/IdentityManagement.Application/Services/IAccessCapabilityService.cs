using Archon.Core.Access;
using IdentityManagement.Application.Responses.AccessCapabilities;
using IdentityManagement.Application.Responses.AccessResources;

namespace IdentityManagement.Application.Services
{
    public interface IAccessCapabilityService
    {
        Task<AccessResourceSyncResponse> SyncCapabilities(AccessCapabilitySyncRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AccessCapabilityResponse>> GetActiveByContract(long contractId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nomes dos recursos ativos do sistema cobertos por qualquer uma das capacidades informadas.
        /// </summary>
        Task<IReadOnlyCollection<string>> ExpandResourceNames(long systemApplicationId, IReadOnlyCollection<string> capabilityKeys, CancellationToken cancellationToken = default);
    }
}
