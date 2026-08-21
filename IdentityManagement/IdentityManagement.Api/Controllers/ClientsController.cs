using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace IdentityManagement.Api.Controllers
{
    public sealed class ClientsController : ApiControllerBase
    {
        private readonly IClientOnboardingService clientOnboardingService;
        private readonly IConfiguration configuration;

        public ClientsController(IClientOnboardingService clientOnboardingService, IConfiguration configuration)
        {
            this.clientOnboardingService = clientOnboardingService;
            this.configuration = configuration;
        }

        [RequireAccess]
        [PostEndpoint("onboard")]
        public async Task<IActionResult> Onboard([FromBody] OnboardClientRequest request, CancellationToken cancellationToken)
        {
            string setupBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;
            var response = await clientOnboardingService.OnboardClient(request, setupBaseUrl, ct: cancellationToken);
            return Http200(response);
        }
    }
}
