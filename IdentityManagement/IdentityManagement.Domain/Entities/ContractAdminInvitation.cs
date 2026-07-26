using Archon.Core.Entities;
using IdentityManagement.Domain.Security;

namespace IdentityManagement.Domain.Entities
{
    public class ContractAdminInvitation : Entity
    {
        public long? ContractId { get; private set; }

        public long? CompanyId { get; private set; }

        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; }

        public DateTimeOffset? UsedAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }

        public long? UserId { get; private set; }

        public Contract? Contract { get; private set; }

        public User? User { get; private set; }

        private ContractAdminInvitation() { }

        public ContractAdminInvitation(long contractId, string token, DateTimeOffset expiresAt)
        {
            ContractId = contractId;
            Token = TokenHasher.Hash(token);
            ExpiresAt = expiresAt;
        }

        public ContractAdminInvitation(long companyId, string token, DateTimeOffset expiresAt, bool companyScoped)
        {
            ContractId = null;
            CompanyId = companyId;
            Token = TokenHasher.Hash(token);
            ExpiresAt = expiresAt;
        }

        public bool IsValid() => UsedAt is null && RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

        public void MarkAsUsed(long userId)
        {
            UsedAt = DateTimeOffset.UtcNow;
            UserId = userId;
        }

        public void Revoke()
        {
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
