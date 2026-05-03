using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.OAuthClients
{
    public sealed class OAuthClientResponse
    {
        public long Id { get; set; }

        public long SystemApplicationId { get; set; }

        public string SystemApplicationName { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public OAuthClientType ClientType { get; set; }

        public bool RequirePkce { get; set; }

        public bool RequireConsent { get; set; }

        public bool AllowOfflineAccess { get; set; }

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }

        public int AccessTokenLifetime { get; set; }

        public int IdentityTokenLifetime { get; set; }

        public int RefreshTokenLifetime { get; set; }

        public bool RefreshTokenRotationEnabled { get; set; }

        public List<OAuthClientRedirectUriResponse> RedirectUris { get; set; } = [];

        public List<string> Scopes { get; set; } = [];
    }
}
