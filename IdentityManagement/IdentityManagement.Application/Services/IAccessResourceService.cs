using Archon.Core.Access;
using IdentityManagement.Application.Responses.AccessResources;

namespace IdentityManagement.Application.Services
{
    public interface IAccessResourceService
    {
        Task<IReadOnlyCollection<AccessResourceResponse>> GetActiveResources(CancellationToken cancellationToken = default);

        Task<AccessResourceSyncResponse> SyncResources(IReadOnlyCollection<AccessResourceModel> resources, CancellationToken cancellationToken = default);
    }
}
