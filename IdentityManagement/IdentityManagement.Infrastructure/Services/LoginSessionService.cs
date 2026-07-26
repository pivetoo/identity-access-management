using Archon.Infrastructure.Services;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class LoginSessionService : CrudService<LoginSession>, ILoginSessionService
    {
        public LoginSessionService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<LoginSession> CreateSession(User user, Contract contract, string ipAddress, string userAgent, int tokenLifetimeInSeconds, CancellationToken cancellationToken = default)
        {
            LoginSession session = new LoginSession(user.Id, contract.Id, ipAddress ?? "Unknown", userAgent ?? "Unknown");
            session.SetExpiration(DateTimeOffset.UtcNow.AddSeconds(tokenLifetimeInSeconds));

            bool success = await Insert(cancellationToken, session);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return session;
        }

        public async Task RevokeSession(string sessionId, CancellationToken cancellationToken = default)
        {
            LoginSession? session = await (
                from item in DbContext.Set<LoginSession>().AsTracking()
                where item.SessionId == sessionId && item.IsActive
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (session is not null)
            {
                session.Revoke();
                await Update(session, cancellationToken);
            }

            // Fora do `if` de proposito: sessao ausente ou ja inativa nao significa que os refresh
            // tokens dela morreram junto — e sao eles que mantem o acesso vivo por 30 dias.
            await RevokeRefreshTokens(item => item.SessionId == sessionId, cancellationToken);
        }

        public async Task RevokeAllUserSessions(long userId, CancellationToken cancellationToken = default)
        {
            List<LoginSession> sessions = await (
                from item in DbContext.Set<LoginSession>().AsTracking()
                where item.UserId == userId && item.IsActive
                select item)
                .ToListAsync(cancellationToken);

            foreach (LoginSession session in sessions)
            {
                session.Revoke();
            }

            if (sessions.Count > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            await RevokeRefreshTokens(item => item.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// Revogar a sessao sem revogar o refresh token nao encerra nada: o portador do refresh
        /// continua renovando access token normalmente (30 dias, nos clientes de producao). As duas
        /// revogacoes andam juntas — e por isso ficam num lugar so, e nao repetidas em cada chamador.
        /// </summary>
        private async Task RevokeRefreshTokens(
            System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
            CancellationToken cancellationToken)
        {
            List<RefreshToken> refreshTokens = await DbContext.Set<RefreshToken>()
                .AsTracking()
                .Where(predicate)
                .Where(item => !item.IsRevoked)
                .ToListAsync(cancellationToken);

            if (refreshTokens.Count == 0)
            {
                return;
            }

            foreach (RefreshToken refreshToken in refreshTokens)
            {
                refreshToken.Revoke();
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<LoginSession>> GetActiveUserSessions(long userId, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<LoginSession> sessions = await (
                from item in DbContext.Set<LoginSession>().AsNoTracking()
                where item.UserId == userId && item.IsActive && now < item.ExpiresAt && !item.RevokedAt.HasValue
                orderby item.CreatedAt descending
                select item)
                .ToListAsync(cancellationToken);

            return sessions;
        }

        public async Task<int> RevokeAllActiveSessions(CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            List<LoginSession> sessions = await (
                from item in DbContext.Set<LoginSession>().AsTracking()
                where item.IsActive && now < item.ExpiresAt && !item.RevokedAt.HasValue
                select item)
                .ToListAsync(cancellationToken);

            foreach (LoginSession session in sessions)
            {
                session.Revoke();
            }

            if (sessions.Count > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return sessions.Count;
        }
    }
}
