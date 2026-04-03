using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.SystemApplications;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SystemApplicationsController : ApiControllerBase
    {
        private readonly ISystemApplicationService systemApplicationService;

        public SystemApplicationsController(ISystemApplicationService systemApplicationService)
        {
            this.systemApplicationService = systemApplicationService;
        }

        [RequireAccess]
        [PostEndpoint("")]
        public async Task<IActionResult> Create([FromBody] CreateSystemApplicationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await systemApplicationService.CreateSystemApplication(request, cancellationToken);
            return Http201(response, "System application created successfully.");
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
            return Http200(response, "System application updated successfully.");
        }

        [RequireAccess]
        [GetEndpoint("")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var systemApplications = await systemApplicationService.GetActiveSystemApplications(cancellationToken);
            return Http200(systemApplications);
        }
    }
}
