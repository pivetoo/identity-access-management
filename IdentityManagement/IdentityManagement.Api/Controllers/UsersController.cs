using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Users;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class UsersController : ApiControllerBase
    {
        private readonly IUserService userService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public UsersController(IUserService userService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.userService = userService;
            this.Localizer = Localizer;
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var users = await userService.GetActiveUsers(cancellationToken);
            return Http200(users);
        }

        [RequireAccess]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var user = await userService.GetById(id, cancellationToken);
            if (user is null)
            {
                return Http404(Localizer["user.notFound"]);
            }

            return Http200(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Name,
                user.AvatarUrl,
                user.IsActive,
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt
            });
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> RegisterFirst([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<IdentityManagement.Application.Responses.Users.UserResponse> existingUsers = await userService.GetActiveUsers(cancellationToken);
            if (existingUsers.Count > 0)
            {
                return Http403(Localizer["user.firstRegistration.notAllowed"]);
            }

            var response = await userService.CreateUser(request, cancellationToken);
            return Http201(response, Localizer["user.first.created"]);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            var response = await userService.CreateUser(request, cancellationToken);
            return Http201(response, Localizer["user.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var response = await userService.UpdateUser(id, request, cancellationToken);
            return Http200(response, Localizer["user.updated"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}/avatar")]
        public async Task<IActionResult> UpdateAvatar(long id, [FromBody] string? avatarUrl, CancellationToken cancellationToken)
        {
            string? previousAvatarUrl = await userService.UpdateAvatar(id, avatarUrl, cancellationToken);
            return Http200(new
            {
                AvatarUrl = avatarUrl,
                PreviousAvatarUrl = previousAvatarUrl
            }, Localizer["user.avatar.updated"]);
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}/avatar")]
        public async Task<IActionResult> DeleteAvatar(long id, CancellationToken cancellationToken)
        {
            string? previousAvatarUrl = await userService.DeleteAvatar(id, cancellationToken);
            return Http200(new
            {
                PreviousAvatarUrl = previousAvatarUrl
            }, Localizer["user.avatar.deleted"]);
        }

        [RequireAccess]
        [GetEndpoint]
        public async Task<IActionResult> GetByCurrentContract(CancellationToken cancellationToken)
        {
            long? contractId = ResolveCurrentContractId();
            if (!contractId.HasValue)
            {
                return Http403(Localizer["contract.active.notIdentified"]);
            }

            var users = await userService.GetUsersByContract(contractId.Value, cancellationToken);
            return Http200(users);
        }

        [RequireAccess]
        [PostEndpoint]
        public async Task<IActionResult> CreateInCurrentContract([FromBody] CreateUserInContractRequest request, CancellationToken cancellationToken)
        {
            long? contractId = ResolveCurrentContractId();
            if (!contractId.HasValue)
            {
                return Http403(Localizer["contract.active.notIdentified"]);
            }

            var response = await userService.CreateUserInContract(request, contractId.Value, cancellationToken);
            return Http201(response, Localizer["user.contract.created"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> UpdateRoleInCurrentContract(long id, [FromBody] UpdateUserContractRoleRequest request, CancellationToken cancellationToken)
        {
            long? contractId = ResolveCurrentContractId();
            if (!contractId.HasValue)
            {
                return Http403(Localizer["contract.active.notIdentified"]);
            }

            var response = await userService.UpdateUserRoleInContract(id, contractId.Value, request.RoleId, cancellationToken);
            return Http200(response, Localizer["user.contract.roleUpdated"]);
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> SetActive(long id, [FromBody] SetUserActiveRequest request, CancellationToken cancellationToken)
        {
            var response = await userService.SetActive(id, request.IsActive, cancellationToken);
            return Http200(response, Localizer[request.IsActive ? "user.activated" : "user.deactivated"]);
        }

        private long? ResolveCurrentContractId()
        {
            string? value = User.FindFirst("contract_id")?.Value;
            return long.TryParse(value, out long parsed) ? parsed : null;
        }
    }
}
