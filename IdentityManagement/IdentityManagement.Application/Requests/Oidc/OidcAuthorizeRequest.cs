namespace IdentityManagement.Application.Requests.Oidc
{
    public sealed class OidcAuthorizeRequest
    {
        public string ClientId { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string ResponseType { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string CodeChallenge { get; set; } = string.Empty;

        public string CodeChallengeMethod { get; set; } = string.Empty;

        public long? ContractId { get; set; }
    }
}
