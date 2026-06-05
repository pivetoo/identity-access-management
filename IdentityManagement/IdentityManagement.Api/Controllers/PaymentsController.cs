using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class PaymentsController : ApiControllerBase
    {
        private readonly IPaymentService paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> List([FromQuery] PaymentStatus? status, [FromQuery] int? take, CancellationToken cancellationToken)
        {
            var payments = await paymentService.ListAsync(null, status, take, cancellationToken);
            return Http200(payments);
        }

        [RequireAccess]
        [GetEndpoint("company/{companyId:long}")]
        public async Task<IActionResult> GetByCompany(long companyId, CancellationToken cancellationToken)
        {
            var payments = await paymentService.GetByCompanyAsync(companyId, cancellationToken);
            return Http200(payments);
        }

        [RequireAccess]
        [GetEndpoint("webhook-events")]
        public async Task<IActionResult> WebhookEvents([FromQuery] int? take, CancellationToken cancellationToken)
        {
            var events = await paymentService.ListWebhookEventsAsync(take, cancellationToken);
            return Http200(events);
        }
    }
}
