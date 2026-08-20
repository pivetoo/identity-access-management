using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IJwtService
    {
        Task<string> GenerateAccessToken(User user, Contract contract, int lifetimeSeconds, string sessionId, string clientId, bool subscriptionBlocked = false, CancellationToken cancellationToken = default);

        Task<string> GenerateIdToken(User user, Contract contract, string clientId, int lifetimeSeconds, string? nonce, string? sessionId = null, CancellationToken cancellationToken = default);

        string GenerateRefreshToken();

        DateTimeOffset GetTokenExpiration(string token);
    }
}
