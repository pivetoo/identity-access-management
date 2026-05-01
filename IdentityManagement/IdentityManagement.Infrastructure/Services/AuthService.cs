using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Application.Services;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserService userService;
        private readonly IContractService contractService;
        private readonly ITemporaryTokenService temporaryTokenService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AuthService(IUserService userService, IContractService contractService, ITemporaryTokenService temporaryTokenService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.userService = userService;
            this.contractService = contractService;
            this.temporaryTokenService = temporaryTokenService;
            this.Localizer = Localizer;
        }

        public async Task<ContractSelectionResponse> IdentifyUser(IdentifyUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await userService.Authenticate(request.Username, request.Password, cancellationToken);
            if (user is null)
            {
                throw new UnauthorizedAccessException(Localizer["auth.invalidCredentials"]);
            }

            IReadOnlyCollection<ContractSelectionResponseItem> availableContracts = await contractService.GetActiveContractSelectionsByUserId(user.Id, cancellationToken);
            if (availableContracts.Count == 0)
            {
                throw new UnauthorizedAccessException(Localizer["auth.user.noActiveContracts"]);
            }

            return new ContractSelectionResponse
            {
                AuthenticationStep = "contractSelection",
                UserId = user.Id,
                UserName = user.Name,
                UserEmail = user.Email,
                TemporaryToken = temporaryTokenService.GenerateTemporaryToken(user.Id),
                AvailableContracts = availableContracts.ToList()
            };
        }

        public Task<bool> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            return userService.ChangePassword(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
        }

        public async Task<UserResponse?> GetUserByUsername(string username, CancellationToken cancellationToken = default)
        {
            var user = await userService.GetByUsername(username, cancellationToken);
            return user is null ? null : ToUserResponse(user);
        }

        private static UserResponse ToUserResponse(IdentityManagement.Domain.Entities.User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

    }
}
