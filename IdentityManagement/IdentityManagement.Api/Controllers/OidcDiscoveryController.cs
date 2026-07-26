using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    // Anonimo de proposito: sao os endpoints do protocolo OIDC, chamados antes de existir token.
    [AllowAnonymous]
    public sealed class OidcDiscoveryController : ApiControllerBase
    {
        private readonly IOidcDiscoveryService oidcDiscoveryService;
        private readonly IConfiguration configuration;

        public OidcDiscoveryController(IOidcDiscoveryService oidcDiscoveryService, IConfiguration configuration)
        {
            this.oidcDiscoveryService = oidcDiscoveryService;
            this.configuration = configuration;
        }

        [GetEndpoint("/.well-known/openid-configuration")]
        public async Task<IActionResult> GetOpenIdConfiguration(CancellationToken cancellationToken)
        {
            string issuer = configuration["Oidc:Issuer"] ?? $"{Request.Scheme}://{Request.Host}";
            var response = await oidcDiscoveryService.GetConfiguration(issuer, cancellationToken);
            return Ok(response);
        }

        [GetEndpoint("/.well-known/jwks.json")]
        public async Task<IActionResult> GetJsonWebKeySet(CancellationToken cancellationToken)
        {
            var response = await oidcDiscoveryService.GetJsonWebKeySet(cancellationToken);
            return Ok(response);
        }
    }
}
