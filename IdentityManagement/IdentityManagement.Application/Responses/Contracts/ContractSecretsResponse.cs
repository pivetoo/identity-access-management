namespace IdentityManagement.Application.Responses.Contracts
{
    public class ContractSecretsResponse
    {
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string JwtSecretKey { get; set; } = string.Empty;
    }
}
