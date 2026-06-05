using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SubscriptionsController : ApiControllerBase
    {
        private readonly ISubscriptionService subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            this.subscriptionService = subscriptionService;
        }

        [RequireAccess]
        [GetEndpoint("Company/{companyId:long}")]
        public async Task<IActionResult> GetByCompany(long companyId, CancellationToken cancellationToken)
        {
            var subscription = await subscriptionService.GetByCompanyAsync(companyId, cancellationToken);
            return Http200(subscription);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Assign([FromBody] AssignSubscriptionRequest request, CancellationToken cancellationToken)
        {
            var response = await subscriptionService.AssignAsync(request, cancellationToken);
            return Http201(response);
        }

        [RequireAccess]
        [PutEndpoint("ChangePlan/{companyId:long}")]
        public async Task<IActionResult> ChangePlan(long companyId, [FromBody] ChangePlanRequest request, CancellationToken cancellationToken)
        {
            var response = await subscriptionService.ChangePlanAsync(companyId, request, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint("Cancel/{companyId:long}")]
        public async Task<IActionResult> Cancel(long companyId, CancellationToken cancellationToken)
        {
            var response = await subscriptionService.CancelAsync(companyId, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint("Suspend/{companyId:long}")]
        public async Task<IActionResult> Suspend(long companyId, CancellationToken cancellationToken)
        {
            var response = await subscriptionService.SuspendAsync(companyId, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint("Activate/{companyId:long}")]
        public async Task<IActionResult> Activate(long companyId, CancellationToken cancellationToken)
        {
            var response = await subscriptionService.ActivateAsync(companyId, cancellationToken);
            return Http200(response);
        }
    }
}
