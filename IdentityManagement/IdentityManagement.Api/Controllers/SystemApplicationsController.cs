using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.SystemApplications;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SystemApplicationsController : ApiControllerBase
    {
        private readonly ISystemApplicationService systemApplicationService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public SystemApplicationsController(ISystemApplicationService systemApplicationService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.systemApplicationService = systemApplicationService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateSystemApplicationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await systemApplicationService.CreateSystemApplication(request, cancellationToken);
            return Http201(response, Localizer["systemApplication.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSystemApplicationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await systemApplicationService.UpdateSystemApplication(id, request, cancellationToken);
            return Http200(response, Localizer["systemApplication.updated"]);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var systemApplications = await systemApplicationService.GetActiveSystemApplications(cancellationToken);
            return Http200(systemApplications);
        }
    }
}
