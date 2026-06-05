using System.Text.Json;
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
    [Route("api/billing")]
    public sealed class BillingWebhookController : ApiControllerBase
    {
        private const string AsaasAccessTokenHeader = "asaas-access-token";

        private static readonly JsonSerializerOptions WebhookJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IBillingWebhookService billingWebhookService;
        private readonly AsaasOptions options;

        public BillingWebhookController(IBillingWebhookService billingWebhookService, IOptions<AsaasOptions> options)
        {
            this.billingWebhookService = billingWebhookService;
            this.options = options.Value;
        }

        [AllowAnonymous]
        [PostEndpoint("webhook")]
        public async Task<IActionResult> Receive(CancellationToken cancellationToken)
        {
            // Token vazio = pula validacao (conveniencia de dev).
            if (!string.IsNullOrEmpty(options.WebhookToken))
            {
                string receivedToken = Request.Headers[AsaasAccessTokenHeader].ToString();
                if (!string.Equals(receivedToken, options.WebhookToken, StringComparison.Ordinal))
                {
                    return Unauthorized();
                }
            }

            // O corpo so pode ser lido uma vez; capturamos o payload bruto para auditoria.
            string raw;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                raw = await reader.ReadToEndAsync(cancellationToken);
            }

            AsaasWebhookPayload? payload = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                payload = JsonSerializer.Deserialize<AsaasWebhookPayload>(raw, WebhookJsonOptions);
            }

            if (payload is not null)
            {
                await billingWebhookService.ProcessAsaasEventAsync(payload, raw, cancellationToken);
            }

            // Sempre 200 (aplicado ou ignorado) para que o Asaas pare de reenviar o evento.
            return Http200();
        }
    }
}
