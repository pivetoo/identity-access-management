namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcEndSessionRequest
    {
        public string IdTokenHint { get; set; } = string.Empty;

        public string PostLogoutRedirectUri { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;
    }
}
