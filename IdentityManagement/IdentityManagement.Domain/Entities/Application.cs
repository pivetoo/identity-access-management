using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class Application : Entity
    {
        private readonly List<Contract> contracts = [];

        public string Name { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public string RedirectUris { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public string Audience { get; private set; } = string.Empty;

        public ApplicationType Type { get; private set; } = ApplicationType.External;

        public IReadOnlyCollection<Contract> Contracts => contracts.AsReadOnly();

        private Application()
        {
        }

        public Application(string name, string description, string redirectUris, string audience, ApplicationType type = ApplicationType.External)
        {
            SetName(name);
            Description = description.Trim();
            RedirectUris = redirectUris.Trim();
            Audience = audience.Trim();
            Type = type;
        }

        public bool IsRedirectUriValid(string redirectUri)
        {
            if (string.IsNullOrWhiteSpace(RedirectUris))
            {
                return false;
            }

            return RedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(uri => string.Equals(uri, redirectUri, StringComparison.OrdinalIgnoreCase));
        }

        private void SetName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
        }
    }
}
