using Archon.Infrastructure.Services;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class RefreshTokenService : CrudService<RefreshToken>, IRefreshTokenService
    {
        private readonly IJwtService jwtService;

        public RefreshTokenService(DbContext dbContext, IJwtService jwtService) : base(dbContext)
        {
            this.jwtService = jwtService;
        }

        public async Task<RefreshToken> CreateRefreshToken(User user, Contract contract, string sessionId, string scopes, string clientId, CancellationToken cancellationToken = default)
        {
            string token = jwtService.GenerateRefreshToken();
            int expirationDays = Math.Max(1, contract.RefreshTokenLifetime / 86400);
            RefreshToken refreshToken = new RefreshToken(token, user.Id, sessionId, scopes, expirationDays, contract.Id, clientId);

            bool success = await Insert(cancellationToken, refreshToken);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return refreshToken;
        }
    }
}
