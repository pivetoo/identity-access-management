using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Users;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class UsersController : ApiControllerBase
    {
        private readonly IUserService userService;

        public UsersController(IUserService userService)
        {
            this.userService = userService;
        }

        [AllowAnonymous]
        [PostEndpoint("register-first")]
        public async Task<IActionResult> RegisterFirst([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IReadOnlyCollection<IdentityManagement.Application.Responses.Users.UserResponse> existingUsers = await userService.GetActiveUsers(cancellationToken);
            if (existingUsers.Count > 0)
            {
                return Http403("First user registration is not allowed because active users already exist.");
            }

            var response = await userService.CreateUser(request, cancellationToken);
            return Http201(response, "First user created successfully.");
        }

        [RequireAccess]
        [PostEndpoint("")]
        public async Task<IActionResult> Create([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await userService.CreateUser(request, cancellationToken);
            return Http201(response, "User created successfully.");
        }

        [RequireAccess]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await userService.UpdateUser(id, request, cancellationToken);
            return Http200(response, "User updated successfully.");
        }

        [RequireAccess]
        [GetEndpoint("")]
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
                return Http404("User not found.");
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

        [RequireAccess]
        [PutEndpoint("{id:long}/avatar")]
        public async Task<IActionResult> UpdateAvatar(long id, [FromBody] string? avatarUrl, CancellationToken cancellationToken)
        {
            string? previousAvatarUrl = await userService.UpdateAvatar(id, avatarUrl, cancellationToken);
            return Http200(new
            {
                AvatarUrl = avatarUrl,
                PreviousAvatarUrl = previousAvatarUrl
            }, "User avatar updated successfully.");
        }

        [RequireAccess]
        [DeleteEndpoint("{id:long}/avatar")]
        public async Task<IActionResult> DeleteAvatar(long id, CancellationToken cancellationToken)
        {
            string? previousAvatarUrl = await userService.DeleteAvatar(id, cancellationToken);
            return Http200(new
            {
                PreviousAvatarUrl = previousAvatarUrl
            }, "User avatar deleted successfully.");
        }
    }
}
