using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class SystemApplication : Entity
    {
        private readonly List<Contract> contracts = [];

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public string Audience { get; private set; } = string.Empty;

        public ApplicationType Type { get; private set; } = ApplicationType.External;

        public IReadOnlyCollection<Contract> Contracts => contracts.AsReadOnly();

        private SystemApplication()
        {
        }

        public SystemApplication(string name, string description, string audience, ApplicationType type = ApplicationType.External)
        {
            SetName(name);
            Description = description.Trim();
            Audience = audience.Trim();
            Type = type;
        }

        public void Update(string name, string description, string audience, ApplicationType type, bool isActive)
        {
            SetName(name);
            Description = description.Trim();
            Audience = audience.Trim();
            Type = type;
            IsActive = isActive;
        }

        private void SetName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
        }
    }
}
