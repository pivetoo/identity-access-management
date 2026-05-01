namespace IdentityManagement.Application.Responses.Oidc
{
    public sealed class OidcAuthorizeResult
    {
        public bool IsRedirect { get; set; }

        public string RedirectUrl { get; set; } = string.Empty;

        public string? Error { get; set; }

        public string? ErrorDescription { get; set; }
    }
}
