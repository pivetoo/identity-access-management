using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class TenantsController : ApiControllerBase
    {
        private readonly ITenantResolutionService tenantResolutionService;

        public TenantsController(ITenantResolutionService tenantResolutionService)
        {
            this.tenantResolutionService = tenantResolutionService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Resolve([FromQuery] Guid tenantId, [FromQuery] string applicationId, CancellationToken cancellationToken)
        {
            var response = await tenantResolutionService.ResolveByTenantAndApplication(tenantId, applicationId, cancellationToken);
            if (response is null)
            {
                return Http404();
            }

            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> ResolveByApiKey([FromQuery] string apiKey, [FromQuery] string? applicationId, CancellationToken cancellationToken)
        {
            var response = await tenantResolutionService.ResolveByApiKey(apiKey, applicationId, cancellationToken);
            if (response is null)
            {
                return Http404();
            }

            return Http200(response);
        }
    }
}
