using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class Role : Entity
    {
        private readonly List<UserRole> userRoles = [];
        private readonly List<RoleAccessResource> roleAccessResources = [];

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public long ContractId { get; private set; }

        public Contract Contract { get; private set; } = null!;

        public bool IsRoot { get; private set; }

        public bool IsDefault { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<UserRole> UserRoles => userRoles.AsReadOnly();

        public IReadOnlyCollection<RoleAccessResource> RoleAccessResources => roleAccessResources.AsReadOnly();

        private Role()
        {
        }

        public Role(string name, string description, long contractId, bool isRoot = false, bool isDefault = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            if (contractId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractId));
            }

            Name = name.Trim();
            Description = description.Trim();
            ContractId = contractId;
            IsRoot = isRoot;
            IsDefault = isDefault;
            IsActive = true;
        }

        public void Update(string name, string description, bool isRoot, bool isDefault)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            Name = name.Trim();
            Description = description.Trim();
            IsRoot = isRoot;
            IsDefault = isDefault;
        }

        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        /// <summary>
        /// Desativar um perfil deixa de exigir revogacao usuario a usuario (ou exclusao). As claims
        /// do token so consideram perfil ativo.
        /// </summary>
        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
