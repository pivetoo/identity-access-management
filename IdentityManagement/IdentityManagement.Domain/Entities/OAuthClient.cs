using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class OAuthClient : Entity
    {
        private readonly List<OAuthClientRedirectUri> redirectUris = [];
        private readonly List<OAuthClientScope> scopes = [];

        public long SystemApplicationId { get; private set; }

        public SystemApplication SystemApplication { get; private set; } = null!;

        public string ClientId { get; private set; } = string.Empty;

        public string ClientName { get; private set; } = string.Empty;

        public OAuthClientType ClientType { get; private set; } = OAuthClientType.Public;

        public string ClientSecretHash { get; private set; } = string.Empty;

        public bool RequirePkce { get; private set; } = true;

        public bool RequireConsent { get; private set; }

        public bool AllowOfflineAccess { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool IsDefault { get; private set; }

        public int AccessTokenLifetime { get; private set; } = 900;

        public int IdentityTokenLifetime { get; private set; } = 900;

        public int RefreshTokenLifetime { get; private set; } = 2592000;

        public bool RefreshTokenRotationEnabled { get; private set; } = true;

        public IReadOnlyCollection<OAuthClientRedirectUri> RedirectUris => redirectUris.AsReadOnly();

        public IReadOnlyCollection<OAuthClientScope> Scopes => scopes.AsReadOnly();

        private OAuthClient()
        {
        }

        public OAuthClient(long systemApplicationId, string clientId, string clientName, OAuthClientType clientType)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

            SystemApplicationId = systemApplicationId;
            ClientId = clientId.Trim();
            ClientName = clientName.Trim();
            ClientType = clientType;
            RequirePkce = clientType == OAuthClientType.Public;
        }

        public void Update(
            string clientName,
            OAuthClientType clientType,
            string clientSecretHash,
            bool requirePkce,
            bool requireConsent,
            bool allowOfflineAccess,
            bool isActive,
            bool isDefault,
            int accessTokenLifetime,
            int identityTokenLifetime,
            int refreshTokenLifetime,
            bool refreshTokenRotationEnabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

            ClientName = clientName.Trim();
            ClientType = clientType;
            ClientSecretHash = clientSecretHash.Trim();
            RequirePkce = requirePkce;
            RequireConsent = requireConsent;
            AllowOfflineAccess = allowOfflineAccess;
            IsActive = isActive;
            IsDefault = isDefault;
            AccessTokenLifetime = accessTokenLifetime;
            IdentityTokenLifetime = identityTokenLifetime;
            RefreshTokenLifetime = refreshTokenLifetime;
            RefreshTokenRotationEnabled = refreshTokenRotationEnabled;
        }
    }
}
