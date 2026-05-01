using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class RefreshToken : Entity
    {
        public string Token { get; private set; } = string.Empty;

        public long UserId { get; private set; }

        public long? ContractId { get; private set; }

        public string ClientId { get; private set; } = string.Empty;

        public User User { get; private set; } = null!;

        public Contract? Contract { get; private set; }

        public string SessionId { get; private set; } = string.Empty;

        public string Scopes { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; }

        public bool IsRevoked { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public DateTimeOffset? LastUsedAt { get; private set; }

        private RefreshToken()
        {
        }

        public RefreshToken(string token, long userId, string sessionId, string scopes, int expirationDays, long? contractId, string clientId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            Token = token.Trim();
            UserId = userId;
            ContractId = contractId;
            ClientId = clientId.Trim();
            SessionId = sessionId.Trim();
            Scopes = scopes.Trim();
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays);
        }

        public bool IsValid()
        {
            return !IsRevoked && DateTimeOffset.UtcNow < ExpiresAt;
        }

        public void MarkAsUsed()
        {
            LastUsedAt = DateTimeOffset.UtcNow;
        }

        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
