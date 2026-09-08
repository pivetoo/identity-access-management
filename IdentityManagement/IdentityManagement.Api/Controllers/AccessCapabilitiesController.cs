using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Access;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class AccessCapabilitiesController : ApiControllerBase
    {
        private readonly IAccessCapabilityService accessCapabilityService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AccessCapabilitiesController(IAccessCapabilityService accessCapabilityService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.accessCapabilityService = accessCapabilityService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Sync([FromBody] AccessCapabilitySyncRequest request, CancellationToken cancellationToken)
        {
            var response = await accessCapabilityService.SyncCapabilities(request, cancellationToken);
            return Http200(response, Localizer["accessCapabilities.sync.completed"]);
        }

        [RequireAccess]
        [GetEndpoint("{contractId:long}")]
        public async Task<IActionResult> GetByContract(long contractId, CancellationToken cancellationToken)
        {
            var response = await accessCapabilityService.GetActiveByContract(contractId, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint("{systemApplicationId:long}")]
        public async Task<IActionResult> GetBySystemApplication(long systemApplicationId, CancellationToken cancellationToken)
        {
            var response = await accessCapabilityService.GetActiveBySystemApplication(systemApplicationId, cancellationToken);
            return Http200(response);
        }
    }
}
