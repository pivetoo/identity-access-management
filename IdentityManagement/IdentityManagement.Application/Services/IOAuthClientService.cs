using Archon.Application.Services;
using IdentityManagement.Application.Requests.OAuthClients;
using IdentityManagement.Application.Responses.OAuthClients;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IOAuthClientService : ICrudService<OAuthClient>
    {
        Task<IReadOnlyCollection<OAuthClientResponse>> GetOAuthClients(CancellationToken cancellationToken = default);

        Task<OAuthClientResponse?> GetOAuthClient(long id, CancellationToken cancellationToken = default);

        Task<OAuthClientResponse> CreateOAuthClient(CreateOAuthClientRequest request, CancellationToken cancellationToken = default);

        Task<OAuthClientResponse> UpdateOAuthClient(long id, UpdateOAuthClientRequest request, CancellationToken cancellationToken = default);
    }
}
