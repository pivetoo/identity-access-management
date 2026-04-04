using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class SystemRoleTemplate : Entity
    {
        private readonly List<SystemRoleTemplateAccessResource> systemRoleTemplateAccessResources = [];

        public long SystemApplicationId { get; private set; }

        public SystemApplication SystemApplication { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool IsRoot { get; private set; }

        public bool IsDefault { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<SystemRoleTemplateAccessResource> SystemRoleTemplateAccessResources => systemRoleTemplateAccessResources.AsReadOnly();

        private SystemRoleTemplate()
        {
        }

        public SystemRoleTemplate(long systemApplicationId, string name, string description, bool isRoot = false, bool isDefault = false)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            SystemApplicationId = systemApplicationId;
            Name = name.Trim();
            Description = description.Trim();
            IsRoot = isRoot;
            IsDefault = isDefault;
        }

        public void Update(string name, string description, bool isRoot, bool isDefault, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            Name = name.Trim();
            Description = description.Trim();
            IsRoot = isRoot;
            IsDefault = isDefault;
            IsActive = isActive;
        }

        public void SetDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }
    }
}
