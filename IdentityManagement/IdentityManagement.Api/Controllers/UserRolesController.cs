using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.UserRoles;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class UserRolesController : ApiControllerBase
    {
        private readonly IUserRoleService userRoleService;

        public UserRolesController(IUserRoleService userRoleService)
        {
            this.userRoleService = userRoleService;
        }

        [RequireAccess]
        [PostEndpoint("")]
        public async Task<IActionResult> Assign([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            object response = await userRoleService.AssignUserToRole(request.UserId, request.RoleId, cancellationToken);
            return Http201(response, "User role assigned successfully.");
        }

        [RequireAccess]
        [DeleteEndpoint("")]
        public async Task<IActionResult> Revoke([FromBody] RevokeUserRoleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            object response = await userRoleService.RevokeUserFromRole(request.UserId, request.RoleId, cancellationToken);
            return Http200(response, "User role revoked successfully.");
        }

        [RequireAccess]
        [PostEndpoint("reactivate")]
        public async Task<IActionResult> Reactivate([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            object response = await userRoleService.ReactivateUserRole(request.UserId, request.RoleId, cancellationToken);
            return Http200(response, "User role reactivated successfully.");
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
