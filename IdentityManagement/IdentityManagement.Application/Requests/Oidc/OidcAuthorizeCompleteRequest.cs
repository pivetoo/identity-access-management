namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcAuthorizeCompleteRequest
    {
        public string AuthorizeUrl { get; set; } = string.Empty;

        public long UserId { get; set; }

        public long ContractId { get; set; }

        public string TemporaryToken { get; set; } = string.Empty;
    }
}
