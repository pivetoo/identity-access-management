using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, Contract contract, string? sessionId = null);

        string GenerateRefreshToken();

        bool ValidateToken(string token);

        long? GetUserIdFromToken(string token);

        DateTimeOffset GetTokenExpiration(string token);
    }
}
