using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class AuthController : ApiControllerBase
    {
        private readonly IAuthService authService;
        private readonly IRefreshTokenService refreshTokenService;
        private readonly IJwtService jwtService;

        public AuthController(IAuthService authService, IRefreshTokenService refreshTokenService, IJwtService jwtService)
        {
            this.authService = authService;
            this.refreshTokenService = refreshTokenService;
            this.jwtService = jwtService;
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> Identify([FromBody] IdentifyUserRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            object response = await authService.IdentifyUser(request, GetIpAddress(), GetUserAgent(), cancellationToken);
            return Http200(response);
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> LoginWithContract([FromBody] LoginWithContractRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await authService.LoginWithContract(request, GetIpAddress(), GetUserAgent(), cancellationToken);
            return Http200(response);
        }

        [AllowAnonymous]
        [PostEndpoint]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await refreshTokenService.RefreshAccessToken(request.RefreshToken, cancellationToken);
            if (result.RefreshToken is null)
            {
                return Http401("Refresh token is invalid or expired.");
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

        [RequireAccess]
        [Authorize]
        [PostEndpoint]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            await refreshTokenService.RevokeRefreshToken(request.RefreshToken, cancellationToken);
            return Http200(message: "Logout completed successfully.");
        }

        [RequireAccess]
        [Authorize]
        [PostEndpoint]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (CurrentUserId != request.UserId && !User.HasClaim("root", "true"))
            {
                return Http403();
            }

            bool changed = await authService.ChangePassword(request, cancellationToken);
            if (!changed)
            {
                return Http400("Current password is invalid.");
            }

            return Http200(message: "Password changed successfully.");
        }

        [RequireAccess]
        [Authorize]
        [GetEndpoint("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username, CancellationToken cancellationToken)
        {
            var user = await authService.GetUserByUsername(username, cancellationToken);
            if (user is null)
            {
                return Http404("User not found.");
            }

            return Http200(user);
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        private string GetUserAgent()
        {
            return Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
        }
    }
}
