using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Roles;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class RolesController : ApiControllerBase
    {
        private readonly IRoleService roleService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public RolesController(IRoleService roleService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.roleService = roleService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await roleService.CreateRole(request, cancellationToken);
            return Http201(response, Localizer["role.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await roleService.UpdateRole(id, request, cancellationToken);
            return Http200(response, Localizer["role.updated"]);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var roles = await roleService.GetActiveRoles(cancellationToken);
            return Http200(roles);
        }

        [RequireAccess]
        [GetEndpoint("contract/{contractId:long}")]
        public async Task<IActionResult> GetByContract(long contractId, CancellationToken cancellationToken)
        {
            var roles = await roleService.GetRolesByContract(contractId, cancellationToken);
            return Http200(roles);
        }

        [RequireAccess]
        [GetEndpoint("contract/{contractId:long}/default")]
        public async Task<IActionResult> GetDefaultByContract(long contractId, CancellationToken cancellationToken)
        {
            var role = await roleService.GetDefaultRoleByContract(contractId, cancellationToken);
            if (role is null)
            {
                return Http404(Localizer["role.default.notFound"]);
            }

            return Http200(role);
        }
    }
}
