using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class SystemApplication : Entity
    {
        private readonly List<Contract> contracts = [];
        private readonly List<SystemIntegration> integrations = [];

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public string Audience { get; private set; } = string.Empty;

        public ApplicationType Type { get; private set; } = ApplicationType.External;

        public string CatalogApiKey { get; private set; } = string.Empty;

        public string? BaseUrl { get; private set; }

        public IReadOnlyCollection<Contract> Contracts => contracts.AsReadOnly();

        public IReadOnlyCollection<SystemIntegration> Integrations => integrations.AsReadOnly();

        private SystemApplication()
        {
        }

        public SystemApplication(string name, string description, string audience, ApplicationType type = ApplicationType.External)
        {
            SetName(name);
            Description = description.Trim();
            Audience = audience.Trim();
            Type = type;
            CatalogApiKey = Guid.NewGuid().ToString();
        }

        public void RegenerateCatalogApiKey()
        {
            CatalogApiKey = Guid.NewGuid().ToString();
        }

        public void SetBaseUrl(string? baseUrl)
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.Trim();
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
