using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.OAuthClients;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class OAuthClientsController : ApiControllerBase
    {
        private readonly IOAuthClientService oauthClientService;

        public OAuthClientsController(IOAuthClientService oauthClientService)
        {
            this.oauthClientService = oauthClientService;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var response = await oauthClientService.GetOAuthClients(cancellationToken);
            return Http200(response);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var response = await oauthClientService.GetOAuthClient(id, cancellationToken);
            if (response is null)
            {
                return Http404("OAuth client not found.");
            }

            return Http200(response);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateOAuthClientRequest request, CancellationToken cancellationToken)
        {
            var response = await oauthClientService.CreateOAuthClient(request, cancellationToken);
            return Http201(response, "OAuth client created.");
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOAuthClientRequest request, CancellationToken cancellationToken)
        {
            var response = await oauthClientService.UpdateOAuthClient(id, request, cancellationToken);
            return Http200(response, "OAuth client updated.");
        }
    }
}
