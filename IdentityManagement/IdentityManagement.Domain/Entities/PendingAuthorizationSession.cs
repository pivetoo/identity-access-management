using Archon.Core.Entities;
using IdentityManagement.Domain.Security;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagement.Domain.Entities
{
    public sealed class PendingAuthorizationSession : Entity
    {
        public long UserId { get; private set; }

        public User User { get; private set; } = null!;

        /// <summary>Hash do token. O valor em claro nunca e persistido.</summary>
        public string Token { get; private set; } = string.Empty;

        /// <summary>Valor em claro, disponivel so na instancia recem-criada — e o que vai ao cliente.</summary>
        [NotMapped]
        public string? PlainToken { get; private set; }

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
            PlainToken = token.Trim();
            Token = TokenHasher.Hash(token);
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
