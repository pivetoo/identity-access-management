using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Tenants;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class TenantDatabasesController : ApiControllerBase
    {
        private readonly ITenantDatabaseService tenantDatabaseService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public TenantDatabasesController(ITenantDatabaseService tenantDatabaseService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.tenantDatabaseService = tenantDatabaseService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var items = await tenantDatabaseService.GetAll(cancellationToken);
            return Http200(items);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var item = await tenantDatabaseService.GetById(id, cancellationToken);
            if (item is null)
            {
                return Http404(Localizer["tenantDatabase.notFound"]);
            }

            return Http200(item);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateTenantDatabaseRequest request, CancellationToken cancellationToken)
        {
            var response = await tenantDatabaseService.Create(request, cancellationToken);
            return Http201(response, Localizer["tenantDatabase.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateTenantDatabaseRequest request, CancellationToken cancellationToken)
        {
            var response = await tenantDatabaseService.Update(id, request, cancellationToken);
            return Http200(response, Localizer["tenantDatabase.updated"]);
        }
    }
}
