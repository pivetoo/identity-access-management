using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class LoginSession : Entity
    {
        public long UserId { get; private set; }

        public long ContractId { get; private set; }

        public User User { get; private set; } = null!;

        public Contract Contract { get; private set; } = null!;

        public string SessionId { get; private set; } = Guid.NewGuid().ToString("N");

        public string IpAddress { get; private set; } = string.Empty;

        public string UserAgent { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; } = DateTimeOffset.UtcNow.AddHours(24);

        public bool IsActive { get; private set; } = true;

        public DateTimeOffset? RevokedAt { get; private set; }

        private LoginSession()
        {
        }

        public LoginSession(long userId, long contractId, string ipAddress, string userAgent)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (contractId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractId));
            }

            UserId = userId;
            ContractId = contractId;
            IpAddress = ipAddress.Trim();
            UserAgent = userAgent.Trim();
        }

        public bool IsValid()
        {
            return IsActive && DateTimeOffset.UtcNow < ExpiresAt;
        }

        public void Revoke()
        {
            IsActive = false;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
