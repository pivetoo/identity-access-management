using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Contact;
using IdentityManagement.Application.Responses.Contact;
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

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> List([FromQuery] bool? pending, [FromQuery] int? take, CancellationToken cancellationToken)
        {
            IReadOnlyList<ContactRequestResponse> contacts = await contactRequestService.ListAsync(pending, take, cancellationToken);
            return Http200(contacts);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}/handled")]
        public async Task<IActionResult> Handled(long id, [FromQuery] bool handled, CancellationToken cancellationToken)
        {
            ContactRequestResponse contact = await contactRequestService.SetHandledAsync(id, handled, cancellationToken);
            return Http200(contact, Localizer[handled ? "contact.handled" : "contact.reopened"]);
        }
    }
}
