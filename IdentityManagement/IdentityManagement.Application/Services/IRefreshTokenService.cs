using Archon.Application.Services;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IRefreshTokenService : ICrudService<RefreshToken>
    {
        Task<RefreshToken> CreateRefreshToken(User user, Contract contract, string sessionId, string? scopes = null, CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetValidRefreshToken(string token, CancellationToken cancellationToken = default);

        Task RevokeRefreshToken(string token, CancellationToken cancellationToken = default);

        Task RevokeAllUserRefreshTokens(long userId, CancellationToken cancellationToken = default);

        Task<(RefreshToken? RefreshToken, string NewAccessToken, string NewRefreshToken)> RefreshAccessToken(string refreshToken, CancellationToken cancellationToken = default);
    }
}
