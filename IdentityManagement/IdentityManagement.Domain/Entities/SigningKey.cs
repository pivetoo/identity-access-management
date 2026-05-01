using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class SigningKey : Entity
    {
        public string KeyId { get; private set; } = string.Empty;

        public string Algorithm { get; private set; } = "RS256";

        public string PublicKeyPem { get; private set; } = string.Empty;

        public string PrivateKeyEncrypted { get; private set; } = string.Empty;

        public DateTimeOffset NotBefore { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExpiresAt { get; private set; }

        public bool IsActive { get; private set; } = true;

        public DateTimeOffset? RevokedAt { get; private set; }

        private SigningKey()
        {
        }

        public SigningKey(string keyId, string algorithm, string publicKeyPem, string privateKeyEncrypted, DateTimeOffset notBefore, DateTimeOffset? expiresAt = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
            ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
            ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);

            KeyId = keyId.Trim();
            Algorithm = algorithm.Trim();
            PublicKeyPem = publicKeyPem.Trim();
            PrivateKeyEncrypted = privateKeyEncrypted.Trim();
            NotBefore = notBefore.ToUniversalTime();
            ExpiresAt = expiresAt?.ToUniversalTime();
        }

        public bool IsValid()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return IsActive && !RevokedAt.HasValue && now >= NotBefore && (!ExpiresAt.HasValue || now < ExpiresAt.Value);
        }

        public void Revoke()
        {
            IsActive = false;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
