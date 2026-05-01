using IdentityManagement.Application.Responses.Oidc;

namespace IdentityManagement.Application.Services
{
    public interface IOidcDiscoveryService
    {
        Task<OpenIdConfigurationResponse> GetConfiguration(string issuer, CancellationToken cancellationToken = default);

        Task<JsonWebKeySetResponse> GetJsonWebKeySet(CancellationToken cancellationToken = default);
    }
}
