using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class OAuthScope : Entity
    {
        private readonly List<OAuthClientScope> clients = [];

        public string Name { get; private set; } = string.Empty;

        public string DisplayName { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool IsIdentityScope { get; private set; }

        public bool IsApiScope { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<OAuthClientScope> Clients => clients.AsReadOnly();

        private OAuthScope()
        {
        }

        public OAuthScope(string name, string displayName, string description, bool isIdentityScope, bool isApiScope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            Name = name.Trim();
            DisplayName = displayName.Trim();
            Description = description.Trim();
            IsIdentityScope = isIdentityScope;
            IsApiScope = isApiScope;
        }

        public void Update(string displayName, string description, bool isIdentityScope, bool isApiScope, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            DisplayName = displayName.Trim();
            Description = description.Trim();
            IsIdentityScope = isIdentityScope;
            IsApiScope = isApiScope;
            IsActive = isActive;
        }
    }
}
