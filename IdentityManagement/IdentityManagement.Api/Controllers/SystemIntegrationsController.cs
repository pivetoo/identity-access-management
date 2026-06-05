using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.SystemIntegrations;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SystemIntegrationsController : ApiControllerBase
    {
        private readonly ISystemIntegrationService systemIntegrationService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public SystemIntegrationsController(ISystemIntegrationService systemIntegrationService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.systemIntegrationService = systemIntegrationService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint("{systemApplicationId:long}")]
        public async Task<IActionResult> BySystem(long systemApplicationId, CancellationToken cancellationToken)
        {
            var integrations = await systemIntegrationService.GetBySystemApplicationAsync(systemApplicationId, cancellationToken);
            return Http200(integrations);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] UpsertSystemIntegrationRequest request, CancellationToken cancellationToken)
        {
            var response = await systemIntegrationService.CreateAsync(request, cancellationToken);
            return Http201(response, Localizer["systemIntegration.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpsertSystemIntegrationRequest request, CancellationToken cancellationToken)
        {
            var response = await systemIntegrationService.UpdateAsync(id, request, cancellationToken);
            return Http200(response, Localizer["systemIntegration.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await systemIntegrationService.DeleteAsync(id, cancellationToken);
            return Http200(message: Localizer["systemIntegration.deleted"]);
        }
    }
}
