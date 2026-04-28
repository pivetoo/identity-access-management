using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Access;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class AccessResourcesController : ApiControllerBase
    {
        private readonly IAccessResourceService accessResourceService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AccessResourcesController(IAccessResourceService accessResourceService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.accessResourceService = accessResourceService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var response = await accessResourceService.GetActiveResources(cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Sync([FromBody] List<AccessResourceModel> resources, CancellationToken cancellationToken)
        {
            var response = await accessResourceService.SyncResources(resources, cancellationToken);
            return Http200(response, Localizer["accessResources.sync.completed"]);
        }
    }
}
