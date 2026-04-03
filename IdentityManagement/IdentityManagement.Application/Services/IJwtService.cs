using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IJwtService
    {
        Task<string> GenerateAccessToken(User user, Contract contract, string? sessionId = null, CancellationToken cancellationToken = default);

        string GenerateRefreshToken();

        bool ValidateToken(string token);

        long? GetUserIdFromToken(string token);

        DateTimeOffset GetTokenExpiration(string token);
    }
}
