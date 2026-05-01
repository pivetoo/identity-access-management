using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public sealed class PendingAuthorizationSession : Entity
    {
        public long UserId { get; private set; }

        public User User { get; private set; } = null!;

        public string Token { get; private set; } = string.Empty;

        public string? AuthorizeRequestHash { get; private set; }

        public DateTimeOffset ExpiresAt { get; private set; }

        public bool IsUsed { get; private set; }

        public DateTimeOffset? UsedAt { get; private set; }

        public bool IsRevoked { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        private PendingAuthorizationSession()
        {
        }

        public PendingAuthorizationSession(long userId, string token, string? authorizeRequestHash = null, int expirationMinutes = 10)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (expirationMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expirationMinutes));
            }

            UserId = userId;
            Token = token.Trim();
            AuthorizeRequestHash = string.IsNullOrWhiteSpace(authorizeRequestHash) ? null : authorizeRequestHash.Trim();
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);
        }

        public bool IsValid()
        {
            return !IsUsed && !IsRevoked && DateTimeOffset.UtcNow < ExpiresAt;
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            UsedAt = DateTimeOffset.UtcNow;
        }

        public void Revoke()
        {
            IsRevoked = true;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
