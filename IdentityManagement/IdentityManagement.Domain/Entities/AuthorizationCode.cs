using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class AuthorizationCode : Entity
    {
        public string Code { get; private set; } = string.Empty;

        public long UserId { get; private set; }

        public long? ContractId { get; private set; }

        public User User { get; private set; } = null!;

        public Contract? Contract { get; private set; }

        public string Scopes { get; private set; } = string.Empty;

        public string RedirectUri { get; private set; } = string.Empty;

        public string SessionId { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; }

        public bool IsUsed { get; private set; }

        public bool IsRevoked { get; private set; }

        public bool Success { get; private set; }

        public DateTimeOffset? TokenExpiration { get; private set; }

        public DateTimeOffset? UsedAt { get; private set; }

        private AuthorizationCode()
        {
        }

        public AuthorizationCode(string code, long userId, string scopes, string redirectUri, string sessionId, int expirationMinutes, long? contractId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(scopes);
            ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            Code = code.Trim();
            UserId = userId;
            ContractId = contractId;
            Scopes = scopes.Trim();
            RedirectUri = redirectUri.Trim();
            SessionId = sessionId.Trim();
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);
        }

        public bool IsValid()
        {
            return !IsUsed && !IsRevoked && DateTimeOffset.UtcNow < ExpiresAt;
        }

        public void MarkAsUsed(DateTimeOffset tokenExpiration)
        {
            IsUsed = true;
            Success = true;
            UsedAt = DateTimeOffset.UtcNow;
            TokenExpiration = tokenExpiration;
        }

        public void Revoke()
        {
            IsRevoked = true;
        }
    }
}
