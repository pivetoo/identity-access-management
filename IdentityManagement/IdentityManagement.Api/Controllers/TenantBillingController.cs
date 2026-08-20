using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    /// <summary>
    /// Cobranca na visao do proprio tenant, consumida pelos sistemas do ecossistema (hoje o
    /// Mainstay) com a chave de aplicacao. Separado do <c>SubscriptionsController</c>, que serve o
    /// console de administracao e trafega id de empresa.
    ///
    /// O endereçamento e por <c>tenantId</c> de proposito: o sistema consumidor tem esse valor na
    /// claim ja validada do usuario, entao nao precisa (nem consegue) apontar para outra empresa.
    /// </summary>
    public sealed class TenantBillingController : ApiControllerBase
    {
        private readonly ITenantBillingService tenantBillingService;

        public TenantBillingController(ITenantBillingService tenantBillingService)
        {
            this.tenantBillingService = tenantBillingService;
        }

        [RequireAccess]
        [GetEndpoint("{tenantId:guid}")]
        public async Task<IActionResult> Subscription(Guid tenantId, CancellationToken cancellationToken)
        {
            TenantSubscriptionResponse response = await tenantBillingService.GetSubscriptionAsync(tenantId, cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [PutEndpoint("{tenantId:guid}")]
        public async Task<IActionResult> BillingAddress(Guid tenantId, [FromBody] UpdateBillingAddressRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            TenantSubscriptionResponse response = await tenantBillingService.UpdateBillingAddressAsync(tenantId, request, cancellationToken);
            return Http200(response, Localizer["billing.address.updated"]);
        }

        [RequireAccess]
        [PostEndpoint("{tenantId:guid}")]
        public async Task<IActionResult> CardCheckout(Guid tenantId, CancellationToken cancellationToken)
        {
            TenantCheckoutResponse response = await tenantBillingService.StartCardCheckoutAsync(tenantId, cancellationToken);
            return Http200(response);
        }
    }
}
