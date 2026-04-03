using Archon.Application.Services;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface ILoginSessionService : ICrudService<LoginSession>
    {
        Task<LoginSession> CreateSession(User user, Contract contract, string ipAddress, string userAgent, int tokenLifetimeInSeconds, CancellationToken cancellationToken = default);

        Task RevokeSession(string sessionId, CancellationToken cancellationToken = default);

        Task RevokeAllUserSessions(long userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<LoginSession>> GetActiveUserSessions(long userId, CancellationToken cancellationToken = default);

        Task<int> RevokeAllActiveSessions(CancellationToken cancellationToken = default);
    }
}
