using System.Text.Json.Serialization;

namespace IdentityManagement.Application.Responses.Oidc
{
    public sealed class OpenIdConfigurationResponse
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        [JsonPropertyName("revocation_endpoint")]
        public string RevocationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("end_session_endpoint")]
        public string EndSessionEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("login_page_url")]
        public string LoginPageUrl { get; set; } = string.Empty;

        [JsonPropertyName("response_types_supported")]
        public IReadOnlyCollection<string> ResponseTypesSupported { get; set; } = [];

        [JsonPropertyName("grant_types_supported")]
        public IReadOnlyCollection<string> GrantTypesSupported { get; set; } = [];

        [JsonPropertyName("subject_types_supported")]
        public IReadOnlyCollection<string> SubjectTypesSupported { get; set; } = [];

        [JsonPropertyName("id_token_signing_alg_values_supported")]
        public IReadOnlyCollection<string> IdTokenSigningAlgValuesSupported { get; set; } = [];

        [JsonPropertyName("scopes_supported")]
        public IReadOnlyCollection<string> ScopesSupported { get; set; } = [];

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public IReadOnlyCollection<string> TokenEndpointAuthMethodsSupported { get; set; } = [];

        [JsonPropertyName("code_challenge_methods_supported")]
        public IReadOnlyCollection<string> CodeChallengeMethodsSupported { get; set; } = [];
    }
}
