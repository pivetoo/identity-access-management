using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.OAuthClients
{
    public sealed class CreateOAuthClientRequest
    {
        public long SystemApplicationId { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public OAuthClientType ClientType { get; set; } = OAuthClientType.Public;

        public string ClientSecret { get; set; } = string.Empty;

        public bool RequirePkce { get; set; } = true;

        public bool RequireConsent { get; set; }

        public bool AllowOfflineAccess { get; set; } = true;

        public bool IsDefault { get; set; }

        public int AccessTokenLifetime { get; set; } = 900;

        public int IdentityTokenLifetime { get; set; } = 900;

        public int RefreshTokenLifetime { get; set; } = 2592000;

        public bool RefreshTokenRotationEnabled { get; set; } = true;

        public List<OAuthClientRedirectUriRequest> RedirectUris { get; set; } = [];

        public List<string> Scopes { get; set; } = ["openid", "profile", "email", "offline_access"];
    }
}
