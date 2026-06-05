using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityManagement.Api.Controllers
{
    // Rota customizada para obter POST /api/billing/webhook sem depender do nome do controller.
    [Route("api/billing")]
    public sealed class BillingWebhookController : ApiControllerBase
    {
        private const string AsaasAccessTokenHeader = "asaas-access-token";

        private readonly IBillingWebhookService billingWebhookService;
        private readonly AsaasOptions options;

        public BillingWebhookController(IBillingWebhookService billingWebhookService, IOptions<AsaasOptions> options)
        {
            this.billingWebhookService = billingWebhookService;
            this.options = options.Value;
        }

        // Recebe eventos de cobranca do Asaas (pago/falhou/estornado) que disparam as transicoes da Subscription.
        [AllowAnonymous]
        [PostEndpoint("webhook")]
        public async Task<IActionResult> Receive([FromBody] AsaasWebhookPayload payload, CancellationToken cancellationToken)
        {
            // Validacao do token: o Asaas devolve o authToken configurado no header asaas-access-token.
            // Se o token configurado estiver vazio, pula a validacao (conveniencia de dev) mas ainda processa.
            if (!string.IsNullOrEmpty(options.WebhookToken))
            {
                string receivedToken = Request.Headers[AsaasAccessTokenHeader].ToString();
                if (!string.Equals(receivedToken, options.WebhookToken, StringComparison.Ordinal))
                {
                    return Unauthorized();
                }
            }

            await billingWebhookService.ProcessAsaasEventAsync(payload, cancellationToken);

            // Sempre 200 (aplicado ou ignorado) para que o Asaas pare de reenviar o evento.
            return Http200();
        }
    }
}
