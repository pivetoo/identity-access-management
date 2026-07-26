using Archon.Core.Entities;
using IdentityManagement.Domain.Security;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityManagement.Domain.Entities
{
    public class RefreshToken : Entity
    {
        /// <summary>Hash do token. O valor em claro nunca e persistido.</summary>
        public string Token { get; private set; } = string.Empty;

        /// <summary>
        /// Valor em claro, disponivel apenas na instancia que acabou de ser criada — e o que vai para
        /// o cliente. Depois de recarregar do banco, e nulo, e nao ha como recuperar.
        /// </summary>
        [NotMapped]
        public string? PlainToken { get; private set; }

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

            PlainToken = token.Trim();
            Token = TokenHasher.Hash(token);
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
