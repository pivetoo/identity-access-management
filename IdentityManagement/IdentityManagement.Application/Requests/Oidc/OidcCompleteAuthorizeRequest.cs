namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcCompleteAuthorizeRequest
    {
        public string AuthorizationSessionToken { get; set; } = string.Empty;

        public long ContractId { get; set; }

        public string AuthorizeUrl { get; set; } = string.Empty;
    }
}
