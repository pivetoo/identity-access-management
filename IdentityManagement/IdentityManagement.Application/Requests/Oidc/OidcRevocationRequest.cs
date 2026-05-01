namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcRevocationRequest
    {
        public string Token { get; set; } = string.Empty;

        public string TokenTypeHint { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;
    }
}
