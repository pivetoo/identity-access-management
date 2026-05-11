using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class PasswordResetToken : Entity
    {
        public long UserId { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; }

        public DateTimeOffset? UsedAt { get; private set; }

        public User User { get; private set; } = null!;

        private PasswordResetToken() { }

        public PasswordResetToken(long userId, string token, DateTimeOffset expiresAt)
        {
            UserId = userId;
            Token = token;
            ExpiresAt = expiresAt;
        }

        public bool IsValid() => UsedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

        public void MarkAsUsed()
        {
            UsedAt = DateTimeOffset.UtcNow;
        }
    }
}
