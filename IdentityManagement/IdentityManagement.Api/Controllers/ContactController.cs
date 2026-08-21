using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Contact;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    /// <summary>
    /// Contato vindo do site publico (mainstay.com.br).
    ///
    /// Anonimo por natureza — quem preenche ainda nao e cliente. As travas sao: rate limit por IP,
    /// honeypot no corpo, limite de tamanho em cada campo e destinatario fixo em configuracao.
    /// A resposta e sempre a mesma, inclusive quando o honeypot descarta: dizer ao robo que ele foi
    /// detectado so ensina a proxima tentativa.
    /// </summary>
    public sealed class ContactController : ApiControllerBase
    {
        private readonly IContactRequestService contactRequestService;

        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public ContactController(IContactRequestService contactRequestService, IStringLocalizer<IdentityManagementResource> localizer)
        {
            this.contactRequestService = contactRequestService;
            Localizer = localizer;
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Contact)]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] ContactRequestInput request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            string? sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            await contactRequestService.SubmitAsync(request, sourceIp, cancellationToken);

            return Http200(new { received = true }, Localizer["contact.received"]);
        }
    }
}
