using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    // Rota customizada para obter POST /api/billing/webhook sem depender do nome do controller.
    [Route("api/billing")]
    public sealed class BillingWebhookController : ApiControllerBase
    {
        // Placeholder ate integrar o gateway real (Asaas, Iugu, etc.).
        // Aqui vao chegar os eventos (pago/falhou) que dispararao as transicoes da Subscription.
        [AllowAnonymous]
        [PostEndpoint("webhook")]
        public IActionResult Receive([FromBody] object payload)
        {
            return Http200();
        }
    }
}
