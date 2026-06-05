using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class PlansController : ApiControllerBase
    {
        private readonly IPlanService planService;

        public PlansController(IPlanService planService)
        {
            this.planService = planService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var plans = await planService.GetActiveAsync(cancellationToken);
            return Http200(plans);
        }

        [RequireAccess]
        [GetEndpoint("All")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var plans = await planService.GetAllAsync(cancellationToken);
            return Http200(plans);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var plan = await planService.GetByIdAsync(id, cancellationToken);
            return Http200(plan);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreatePlanRequest request, CancellationToken cancellationToken)
        {
            var response = await planService.CreateAsync(request, cancellationToken);
            return Http201(response);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePlanRequest request, CancellationToken cancellationToken)
        {
            var response = await planService.UpdateAsync(id, request, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint("{id:long}/Activate")]
        public async Task<IActionResult> Activate(long id, CancellationToken cancellationToken)
        {
            await planService.ActivateAsync(id, cancellationToken);
            return Http200();
        }

        [RequireAccess]
        [PostEndpoint("{id:long}/Deactivate")]
        public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
        {
            await planService.DeactivateAsync(id, cancellationToken);
            return Http200();
        }
    }
}
