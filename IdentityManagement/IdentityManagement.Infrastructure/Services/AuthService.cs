using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserService userService;
        private readonly IJwtService jwtService;
        private readonly IContractService contractService;
        private readonly ITemporaryTokenService temporaryTokenService;
        private readonly ILoginSessionService loginSessionService;
        private readonly IRefreshTokenService refreshTokenService;

        public AuthService(IUserService userService, IJwtService jwtService, IContractService contractService, ITemporaryTokenService temporaryTokenService, ILoginSessionService loginSessionService, IRefreshTokenService refreshTokenService)
        {
            this.userService = userService;
            this.jwtService = jwtService;
            this.contractService = contractService;
            this.temporaryTokenService = temporaryTokenService;
            this.loginSessionService = loginSessionService;
            this.refreshTokenService = refreshTokenService;
        }

        public async Task<object> IdentifyUser(IdentifyUserRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
        {
            var user = await userService.Authenticate(request.Username, request.Password, cancellationToken);
            if (user is null)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            IReadOnlyCollection<ContractSelectionResponseItem> availableContracts = await contractService.GetActiveContractSelectionsByUserId(user.Id, cancellationToken);
            if (availableContracts.Count == 0)
            {
                throw new UnauthorizedAccessException("User does not have active contracts.");
            }

            if (availableContracts.Count == 1)
            {
                ContractSelectionResponseItem selectedContract = availableContracts.First();
                var contract = await contractService.GetByIdWithRelations(selectedContract.ContractId, cancellationToken);
                if (contract is null)
                {
                    throw new UnauthorizedAccessException("Contract not found.");
                }

                var session = await loginSessionService.CreateSession(user, contract, ipAddress, userAgent, contract.AccessTokenLifetime, cancellationToken);
                string accessToken = await jwtService.GenerateAccessToken(user, contract, session.SessionId, cancellationToken);
                var refreshToken = await refreshTokenService.CreateRefreshToken(user, contract, session.SessionId, cancellationToken: cancellationToken);

                return new LoginResponse
                {
                    AuthenticationStep = "completed",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    TokenType = "Bearer",
                    ExpiresIn = contract.AccessTokenLifetime,
                    RedirectUrl = BuildRedirectUrl(contract.SystemApplication, accessToken, refreshToken.Token),
                    User = ToUserResponse(user),
                    Contract = selectedContract
                };
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

        public async Task<LoginResponse> LoginWithContract(LoginWithContractRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
        {
            if (!temporaryTokenService.ValidateTemporaryTokenForUser(request.TemporaryToken, request.UserId))
            {
                throw new UnauthorizedAccessException("Temporary token is invalid or expired.");
            }

            var user = await userService.GetById(request.UserId, cancellationToken);

            if (user is null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            IReadOnlyCollection<ContractSelectionResponseItem> availableContracts = await contractService.GetActiveContractSelectionsByUserId(request.UserId, cancellationToken);
            ContractSelectionResponseItem? selectedContract = availableContracts.FirstOrDefault(item => item.ContractId == request.ContractId);
            if (selectedContract is null)
            {
                throw new UnauthorizedAccessException("User does not have access to this contract.");
            }

            var contract = await contractService.GetByIdWithRelations(request.ContractId, cancellationToken);
            if (contract is null || !contract.IsValid())
            {
                throw new UnauthorizedAccessException("Contract is invalid.");
            }

            var session = await loginSessionService.CreateSession(user, contract, ipAddress, userAgent, contract.AccessTokenLifetime, cancellationToken);
            string accessToken = await jwtService.GenerateAccessToken(user, contract, session.SessionId, cancellationToken);
            var refreshToken = await refreshTokenService.CreateRefreshToken(user, contract, session.SessionId, cancellationToken: cancellationToken);

            return new LoginResponse
            {
                AuthenticationStep = "completed",
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenType = "Bearer",
                ExpiresIn = contract.AccessTokenLifetime,
                RedirectUrl = BuildRedirectUrl(contract.SystemApplication, accessToken, refreshToken.Token),
                User = ToUserResponse(user),
                Contract = selectedContract
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

        private static string? BuildRedirectUrl(SystemApplication systemApplication, string accessToken, string refreshToken)
        {
            if (systemApplication.Type != ApplicationType.External)
            {
                return null;
            }

            string? baseUrl = systemApplication.RedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            return $"{baseUrl}/callback?accessToken={accessToken}&refreshToken={refreshToken}";
        }
    }
}
