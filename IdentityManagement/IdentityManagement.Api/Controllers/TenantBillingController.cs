using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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

        // O Localizer do ApiControllerBase aponta para o resource do FRAMEWORK: chave do sistema
        // passada por ele volta crua na resposta. Por isso o localizador proprio.
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public TenantBillingController(ITenantBillingService tenantBillingService, IStringLocalizer<IdentityManagementResource> localizer)
        {
            this.tenantBillingService = tenantBillingService;
            Localizer = localizer;
        }

        [RequireAccess]
        [GetEndpoint("{tenantId:guid}")]
        public async Task<IActionResult> CompanyProfile(Guid tenantId, CancellationToken cancellationToken)
        {
            return Http200(await tenantBillingService.GetCompanyProfileAsync(tenantId, cancellationToken));
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

        [RequireAccess]
        [PostEndpoint("{tenantId:guid}")]
        public async Task<IActionResult> SwitchToPix(Guid tenantId, CancellationToken cancellationToken)
        {
            TenantSubscriptionResponse response = await tenantBillingService.SwitchToPixAsync(tenantId, cancellationToken);
            return Http200(response, Localizer["billing.paymentMethod.switchedToPix"]);
        }
    }
}
