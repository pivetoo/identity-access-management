using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.UserRoles;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class UserRolesController : ApiControllerBase
    {
        private readonly IUserRoleService userRoleService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public UserRolesController(IUserRoleService userRoleService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.userRoleService = userRoleService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Assign([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            object response = await userRoleService.AssignUserToRole(request.UserId, request.RoleId, cancellationToken);
            return Http201(response, Localizer["userRole.assigned"]);
        }

        [RequireAccess]
        [DeleteEndpoint]
        public async Task<IActionResult> Revoke([FromBody] RevokeUserRoleRequest request, CancellationToken cancellationToken)
        {
            object response = await userRoleService.RevokeUserFromRole(request.UserId, request.RoleId, cancellationToken);
            return Http200(response, Localizer["userRole.revoked"]);
        }

        [RequireAccess]
        [PostEndpoint("reactivate")]
        public async Task<IActionResult> Reactivate([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            object response = await userRoleService.ReactivateUserRole(request.UserId, request.RoleId, cancellationToken);
            return Http200(response, Localizer["userRole.reactivated"]);
        }

        [RequireAccess]
        [GetEndpoint("user/{userId:long}")]
        public async Task<IActionResult> GetByUser(long userId, CancellationToken cancellationToken)
        {
            var roles = await userRoleService.GetUserRoles(userId, cancellationToken);
            return Http200(roles);
        }

        [RequireAccess]
        [GetEndpoint("user/{userId:long}/active")]
        public async Task<IActionResult> GetActiveByUser(long userId, CancellationToken cancellationToken)
        {
            var roles = await userRoleService.GetActiveUserRoles(userId, cancellationToken);
            return Http200(roles);
        }

        [RequireAccess]
        [GetEndpoint("role/{roleId:long}")]
        public async Task<IActionResult> GetByRole(long roleId, CancellationToken cancellationToken)
        {
            var users = await userRoleService.GetRoleUsers(roleId, cancellationToken);
            return Http200(users);
        }

        [RequireAccess]
        [GetEndpoint("contract/{contractId:long}")]
        public async Task<IActionResult> GetByContract(long contractId, CancellationToken cancellationToken)
        {
            var userRoles = await userRoleService.GetUserRolesByContract(contractId, cancellationToken);
            return Http200(userRoles);
        }

        [RequireAccess]
        [GetEndpoint("user/{userId:long}/role/{roleId:long}/has-access")]
        public async Task<IActionResult> HasAccess(long userId, long roleId, CancellationToken cancellationToken)
        {
            bool hasAccess = await userRoleService.HasUserAccess(userId, roleId, cancellationToken);
            return Http200(new { HasAccess = hasAccess });
        }
    }
}
