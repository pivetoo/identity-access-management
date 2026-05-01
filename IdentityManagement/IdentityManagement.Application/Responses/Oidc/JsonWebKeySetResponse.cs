using System.Text.Json.Serialization;

namespace IdentityManagement.Application.Responses.Oidc
{
    public sealed class JsonWebKeySetResponse
    {
        [JsonPropertyName("keys")]
        public IReadOnlyCollection<JsonWebKeyResponse> Keys { get; set; } = [];
    }

    public sealed class JsonWebKeyResponse
    {
        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = string.Empty;

        [JsonPropertyName("use")]
        public string PublicKeyUse { get; set; } = "sig";

        [JsonPropertyName("kid")]
        public string KeyId { get; set; } = string.Empty;

        [JsonPropertyName("alg")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string Modulus { get; set; } = string.Empty;

        [JsonPropertyName("e")]
        public string Exponent { get; set; } = string.Empty;
    }
}
