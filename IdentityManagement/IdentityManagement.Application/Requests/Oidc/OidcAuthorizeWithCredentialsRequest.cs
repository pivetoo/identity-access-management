namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcAuthorizeWithCredentialsRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public long ContractId { get; set; }

        public string AuthorizeUrl { get; set; } = string.Empty;
    }
}
