using System.Security.Cryptography;
using System.Text;
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
        private readonly ILogger<BillingWebhookController> logger;

        public BillingWebhookController(IBillingWebhookService billingWebhookService, IOptions<AsaasOptions> options, ILogger<BillingWebhookController> logger)
        {
            this.billingWebhookService = billingWebhookService;
            this.options = options.Value;
            this.logger = logger;
        }

        /// <summary>Comparacao de tempo constante: segredo de webhook nao se compara com `string.Equals`.</summary>
        private static bool FixedTimeEquals(string received, string expected)
        {
            byte[] receivedBytes = Encoding.UTF8.GetBytes(received ?? string.Empty);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

            return CryptographicOperations.FixedTimeEquals(receivedBytes, expectedBytes);
        }

        [AllowAnonymous]
        [PostEndpoint("webhook")]
        public async Task<IActionResult> Receive(CancellationToken cancellationToken)
        {
            // Antes, token vazio pulava a validacao inteira — um esquecimento de deploy deixava este
            // endpoint anonimo aceitando qualquer POST que marca assinatura como paga. A conveniencia
            // de desenvolvimento agora e uma opcao declarada (`Asaas:AllowUnauthenticatedWebhook`),
            // nao o efeito colateral de uma configuracao ausente. Em producao o startup ja falha se o
            // token nao estiver configurado.
            if (string.IsNullOrEmpty(options.WebhookToken))
            {
                if (!options.AllowUnauthenticatedWebhook)
                {
                    return Unauthorized();
                }

                logger.LogWarning("Webhook de billing aceito SEM validacao de token (AllowUnauthenticatedWebhook habilitado).");
            }
            else
            {
                string receivedToken = Request.Headers[AsaasAccessTokenHeader].ToString();
                if (!FixedTimeEquals(receivedToken, options.WebhookToken))
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
