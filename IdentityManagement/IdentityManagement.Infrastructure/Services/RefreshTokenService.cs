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

        public async Task<RefreshToken> CreateRefreshToken(User user, Contract contract, string sessionId, string? scopes = null, CancellationToken cancellationToken = default)
        {
            string token = jwtService.GenerateRefreshToken();
            int expirationDays = Math.Max(1, contract.RefreshTokenLifetime / 86400);
            RefreshToken refreshToken = new RefreshToken(token, user.Id, sessionId, scopes ?? string.Empty, expirationDays, contract.Id);

            bool success = await Insert(cancellationToken, refreshToken);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return refreshToken;
        }

        public Task<RefreshToken?> GetValidRefreshToken(string token, CancellationToken cancellationToken = default)
        {
            return (
                from refreshToken in DbContext.Set<RefreshToken>().AsTracking()
                where refreshToken.Token == token && !refreshToken.IsRevoked && DateTimeOffset.UtcNow < refreshToken.ExpiresAt
                select refreshToken)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task RevokeRefreshToken(string token, CancellationToken cancellationToken = default)
        {
            RefreshToken? refreshToken = await (
                from item in DbContext.Set<RefreshToken>().AsTracking()
                where item.Token == token && !item.IsRevoked
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (refreshToken is null)
            {
                return;
            }

            refreshToken.Revoke();
            await Update(refreshToken, cancellationToken);
        }

        public async Task RevokeAllUserRefreshTokens(long userId, CancellationToken cancellationToken = default)
        {
            List<RefreshToken> refreshTokens = await (
                from item in DbContext.Set<RefreshToken>().AsTracking()
                where item.UserId == userId && !item.IsRevoked
                select item)
                .ToListAsync(cancellationToken);

            foreach (RefreshToken refreshToken in refreshTokens)
            {
                refreshToken.Revoke();
            }

            if (refreshTokens.Count > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<(RefreshToken? RefreshToken, string NewAccessToken, string NewRefreshToken)> RefreshAccessToken(string refreshToken, CancellationToken cancellationToken = default)
        {
            RefreshToken? existingRefreshToken = await DbContext.Set<RefreshToken>()
                .AsTracking()
                .Include(item => item.User)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.Company)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.SystemApplication)
                .FirstOrDefaultAsync(
                    item => item.Token == refreshToken &&
                            !item.IsRevoked &&
                            DateTimeOffset.UtcNow < item.ExpiresAt,
                    cancellationToken);

            if (existingRefreshToken is null || existingRefreshToken.Contract is null)
            {
                return (null, string.Empty, string.Empty);
            }

            existingRefreshToken.Revoke();
            existingRefreshToken.MarkAsUsed();

            string newAccessToken = await jwtService.GenerateAccessToken(existingRefreshToken.User, existingRefreshToken.Contract, existingRefreshToken.SessionId, cancellationToken);
            RefreshToken newRefreshToken = await CreateRefreshToken(
                existingRefreshToken.User,
                existingRefreshToken.Contract,
                existingRefreshToken.SessionId,
                existingRefreshToken.Scopes,
                cancellationToken);

            await DbContext.SaveChangesAsync(cancellationToken);

            return (newRefreshToken, newAccessToken, newRefreshToken.Token);
        }
    }
}
