using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IJwtService
    {
        Task<string> GenerateAccessToken(User user, Contract contract, string sessionId, string clientId, CancellationToken cancellationToken = default);

        Task<string> GenerateIdToken(User user, Contract contract, string clientId, int lifetimeSeconds, string? nonce, string? sessionId = null, CancellationToken cancellationToken = default);

        string GenerateRefreshToken();

        bool ValidateToken(string token);

        long? GetUserIdFromToken(string token);

        DateTimeOffset GetTokenExpiration(string token);
    }
}
