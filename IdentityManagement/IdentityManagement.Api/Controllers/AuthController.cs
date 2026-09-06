using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Services;
using IdentityManagement.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class AuthController : ApiControllerBase
    {
        private readonly IAuthService authService;
        private readonly ILoginSessionService loginSessionService;
        private readonly IConfiguration configuration;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AuthController(IAuthService authService, ILoginSessionService loginSessionService, IConfiguration configuration, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.authService = authService;
            this.loginSessionService = loginSessionService;
            this.configuration = configuration;
            this.Localizer = Localizer;
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> Identify([FromBody] IdentifyUserRequest request, CancellationToken cancellationToken)
        {
            var response = await authService.IdentifyUser(request, cancellationToken);
            return Http200(response);
        }

        // Sem [RequireAccess]: o endpoint e de autoatendimento e qualquer usuario autenticado pode usar a
        // propria sessao para entrar em outra aplicacao (SSO). A permissao por contrato continua sendo
        // validada no complete-authorize.
        [Authorize]
        [PostEndpoint]
        public async Task<IActionResult> IdentifySession([FromBody] IdentifySessionRequest request, CancellationToken cancellationToken)
        {
            if (CurrentUserId is null)
            {
                return Http401();
            }

            var response = await authService.IdentifyUserBySession(CurrentUserId.Value, request.AuthorizeUrl, cancellationToken);
            return Http200(response);
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
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            string resetBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;
            await authService.ForgotPasswordAsync(request, resetBaseUrl, cancellationToken);
            return Http200(message: Localizer["auth.password.resetEmailSent"]);
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return Http400(Localizer["auth.password.tooShort"]);
            }

            bool success = await authService.ResetPasswordAsync(request, cancellationToken);
            if (!success)
            {
                return Http400(Localizer["auth.password.resetTokenInvalid"]);
            }

            return Http200(message: Localizer["auth.password.reset"]);
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [GetEndpoint("{token}")]
        public async Task<IActionResult> GetAdminSetup(string token, CancellationToken cancellationToken)
        {
            var info = await authService.ValidateAdminInvitation(token, cancellationToken);
            if (info is null)
            {
                return Http400(Localizer["auth.adminSetup.invalidToken"]);
            }

            return Http200(info);
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> SetupAdmin([FromBody] SetupAdminRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return Http400(Localizer["auth.password.tooShort"]);
            }

            bool success = await authService.SetupAdmin(request, cancellationToken);
            if (!success)
            {
                return Http400(Localizer["auth.adminSetup.invalidToken"]);
            }

            return Http200(message: Localizer["auth.adminSetup.success"]);
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> SetupAdminExistingUser([FromBody] SetupAdminExistingUserRequest request, CancellationToken cancellationToken)
        {
            bool success = await authService.SetupAdminExistingUser(request, cancellationToken);
            if (!success)
            {
                return Http400(Localizer["auth.adminSetup.invalidToken"]);
            }

            return Http200(message: Localizer["auth.adminSetup.success"]);
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
