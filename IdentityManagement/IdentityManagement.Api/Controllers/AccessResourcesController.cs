using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Access;
using IdentityManagement.Api.Attributes;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    [AllowAnonymous]
    public sealed class AccessResourcesController : ApiControllerBase
    {
        private readonly IAccessResourceService accessResourceService;

        public AccessResourcesController(IAccessResourceService accessResourceService)
        {
            this.accessResourceService = accessResourceService;
        }

        [RequireIntegrationSecret]
        [PostEndpoint("/api/access-resources/sync")]
        public async Task<IActionResult> Sync([FromBody] List<AccessResourceModel> resources, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(resources);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await accessResourceService.SyncResources(resources, cancellationToken);
            return Http200(response, "Access resources synchronized successfully.");
        }
    }
}
