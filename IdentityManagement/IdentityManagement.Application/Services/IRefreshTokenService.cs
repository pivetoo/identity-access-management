using Archon.Application.Services;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IRefreshTokenService : ICrudService<RefreshToken>
    {
        Task<RefreshToken> CreateRefreshToken(User user, Contract contract, int lifetimeSeconds, string sessionId, string scopes, string clientId, CancellationToken cancellationToken = default);
    }
}
