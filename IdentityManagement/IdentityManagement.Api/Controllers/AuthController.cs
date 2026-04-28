using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class AuthController : ApiControllerBase
    {
        private readonly IAuthService authService;
        private readonly IContractService contractService;
        private readonly IRefreshTokenService refreshTokenService;
        private readonly IJwtService jwtService;
        private readonly ILoginSessionService loginSessionService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AuthController(IAuthService authService, IContractService contractService, IRefreshTokenService refreshTokenService, IJwtService jwtService, ILoginSessionService loginSessionService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.authService = authService;
            this.contractService = contractService;
            this.refreshTokenService = refreshTokenService;
            this.jwtService = jwtService;
            this.loginSessionService = loginSessionService;
            this.Localizer = Localizer;
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> Identify([FromBody] IdentifyUserRequest request, CancellationToken cancellationToken)
        {
            object response = await authService.IdentifyUser(request, RequestIpAddress, RequestUserAgent, cancellationToken);
            return Http200(response);
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> LoginWithContract([FromBody] LoginWithContractRequest request, CancellationToken cancellationToken)
        {
            var response = await authService.LoginWithContract(request, RequestIpAddress, RequestUserAgent, cancellationToken);
            return Http200(response);
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await refreshTokenService.RefreshAccessToken(request.RefreshToken, cancellationToken);
            if (result.RefreshToken is null)
            {
                return Http401(Localizer["auth.refreshToken.invalidOrExpired"]);
            }

            DateTimeOffset expiration = jwtService.GetTokenExpiration(result.NewAccessToken);
            int expiresIn = expiration <= DateTimeOffset.UtcNow ? 0 : Convert.ToInt32((expiration - DateTimeOffset.UtcNow).TotalSeconds);

            return Http200(new
            {
                AuthenticationStep = "completed",
                AccessToken = result.NewAccessToken,
                RefreshToken = result.NewRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = expiresIn
            });
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            await refreshTokenService.RevokeRefreshToken(request.RefreshToken, cancellationToken);
            return Http200(message: Localizer["auth.logout.completed"]);
        }

        [RequireAccess]
        [Authorize]
        [PostEndpoint("{sessionId}")]
        public async Task<IActionResult> RevokeSession(string sessionId, CancellationToken cancellationToken)
        {
            await loginSessionService.RevokeSession(sessionId, cancellationToken);
            return Http200(message: Localizer["auth.session.revoked"]);
        }

        [RequireAccess]
        [Authorize]
        [PostEndpoint]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            int count = await loginSessionService.RevokeAllActiveSessions(cancellationToken);
            return Http200(new { Count = count }, Localizer["auth.sessions.revokedAll"]);
        }

        [RequireAccess]
        [Authorize]
        [PostEndpoint]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            if (CurrentUserId != request.UserId && !User.HasClaim("root", "true"))
            {
                return Http403();
            }

            bool changed = await authService.ChangePassword(request, cancellationToken);
            if (!changed)
            {
                return Http400(Localizer["auth.password.currentInvalid"]);
            }

            return Http200(message: Localizer["auth.password.changed"]);
        }

        [AllowAnonymous]
        [GetEndpoint("{clientId}")]
        public async Task<IActionResult> GetContractByClientId(string clientId, CancellationToken cancellationToken)
        {
            var contract = await contractService.GetByClientId(clientId, cancellationToken);
            if (contract is null)
            {
                return Http404(Localizer["contract.notFound"]);
            }

            return Http200(new
            {
                Id = contract.Id,
                ClientId = contract.ClientId,
                Name = contract.SystemApplication.Name,
                JwtSecretKey = contract.JwtSecretKey,
                IsActive = contract.IsValid(),
                AccessTokenLifetime = contract.AccessTokenLifetime,
                RefreshTokenLifetime = contract.RefreshTokenLifetime
            });
        }

        [RequireAccess]
        [Authorize]
        [GetEndpoint("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username, CancellationToken cancellationToken)
        {
            var user = await authService.GetUserByUsername(username, cancellationToken);
            if (user is null)
            {
                return Http404(Localizer["user.notFound"]);
            }

            return Http200(user);
        }
    }
}
