using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Oidc;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IdentityManagement.Api.Controllers
{
    public sealed class OidcController : ApiControllerBase
    {
        private readonly IOidcAuthorizationService oidcAuthorizationService;

        public OidcController(IOidcAuthorizationService oidcAuthorizationService)
        {
            this.oidcAuthorizationService = oidcAuthorizationService;
        }

        [GetEndpoint("/connect/authorize")]
        public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
        {
            OidcAuthorizeRequest request = new()
            {
                ClientId = Request.Query["client_id"].FirstOrDefault() ?? string.Empty,
                RedirectUri = Request.Query["redirect_uri"].FirstOrDefault() ?? string.Empty,
                ResponseType = Request.Query["response_type"].FirstOrDefault() ?? string.Empty,
                Scope = Request.Query["scope"].FirstOrDefault() ?? string.Empty,
                State = Request.Query["state"].FirstOrDefault() ?? string.Empty,
                Nonce = Request.Query["nonce"].FirstOrDefault() ?? string.Empty,
                CodeChallenge = Request.Query["code_challenge"].FirstOrDefault() ?? string.Empty,
                CodeChallengeMethod = Request.Query["code_challenge_method"].FirstOrDefault() ?? string.Empty,
                ContractId = long.TryParse(Request.Query["contract_id"].FirstOrDefault(), out long contractId) ? contractId : null
            };

            string authorizeUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
            var result = await oidcAuthorizationService.Authorize(request, authorizeUrl, CurrentUserId, RequestIpAddress, RequestUserAgent, cancellationToken);

            if (result.IsRedirect)
            {
                return Redirect(result.RedirectUrl);
            }

            return BadRequest(new
            {
                error = result.Error,
                error_description = result.ErrorDescription
            });
        }

        [PostEndpoint("/connect/token")]
        public async Task<IActionResult> Token(CancellationToken cancellationToken)
        {
            IFormCollection form = await Request.ReadFormAsync(cancellationToken);
            OidcTokenRequest request = new()
            {
                GrantType = form["grant_type"].FirstOrDefault() ?? string.Empty,
                ClientId = form["client_id"].FirstOrDefault() ?? string.Empty,
                ClientSecret = form["client_secret"].FirstOrDefault() ?? string.Empty,
                Code = form["code"].FirstOrDefault() ?? string.Empty,
                RedirectUri = form["redirect_uri"].FirstOrDefault() ?? string.Empty,
                CodeVerifier = form["code_verifier"].FirstOrDefault() ?? string.Empty,
                RefreshToken = form["refresh_token"].FirstOrDefault() ?? string.Empty
            };

            ApplyBasicClientCredentials(request);

            try
            {
                var response = await oidcAuthorizationService.ExchangeToken(request, cancellationToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException error)
            {
                return Unauthorized(new
                {
                    error = error.Message
                });
            }
            catch (InvalidOperationException error)
            {
                return BadRequest(new
                {
                    error = error.Message
                });
            }
        }

        [GetEndpoint("/connect/userinfo")]
        [PostEndpoint("/connect/userinfo")]
        public async Task<IActionResult> UserInfo(CancellationToken cancellationToken)
        {
            string accessToken = GetBearerToken();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized(new { error = "invalid_token" });
            }

            try
            {
                Dictionary<string, object> response = await oidcAuthorizationService.GetUserInfo(accessToken, cancellationToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException error)
            {
                return Unauthorized(new { error = error.Message });
            }
        }

        [PostEndpoint("/connect/revocation")]
        public async Task<IActionResult> Revocation(CancellationToken cancellationToken)
        {
            IFormCollection form = await Request.ReadFormAsync(cancellationToken);
            OidcRevocationRequest request = new()
            {
                Token = form["token"].FirstOrDefault() ?? string.Empty,
                TokenTypeHint = form["token_type_hint"].FirstOrDefault() ?? string.Empty,
                ClientId = form["client_id"].FirstOrDefault() ?? string.Empty,
                ClientSecret = form["client_secret"].FirstOrDefault() ?? string.Empty
            };

            ApplyBasicClientCredentials(request);

            try
            {
                await oidcAuthorizationService.RevokeToken(request, cancellationToken);
                return Ok();
            }
            catch (UnauthorizedAccessException error)
            {
                return Unauthorized(new { error = error.Message });
            }
        }

        [GetEndpoint("/connect/logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            OidcEndSessionRequest request = new()
            {
                IdTokenHint = Request.Query["id_token_hint"].FirstOrDefault() ?? string.Empty,
                PostLogoutRedirectUri = Request.Query["post_logout_redirect_uri"].FirstOrDefault() ?? string.Empty,
                State = Request.Query["state"].FirstOrDefault() ?? string.Empty,
                ClientId = Request.Query["client_id"].FirstOrDefault() ?? string.Empty
            };

            return await EndSession(request, cancellationToken);
        }

        [PostEndpoint("/connect/logout")]
        public async Task<IActionResult> LogoutPost(CancellationToken cancellationToken)
        {
            IFormCollection form = await Request.ReadFormAsync(cancellationToken);
            OidcEndSessionRequest request = new()
            {
                IdTokenHint = form["id_token_hint"].FirstOrDefault() ?? string.Empty,
                PostLogoutRedirectUri = form["post_logout_redirect_uri"].FirstOrDefault() ?? string.Empty,
                State = form["state"].FirstOrDefault() ?? string.Empty,
                ClientId = form["client_id"].FirstOrDefault() ?? string.Empty
            };

            return await EndSession(request, cancellationToken);
        }

        [PostEndpoint]
        public async Task<IActionResult> CompleteAuthorize([FromBody] OidcAuthorizeCompleteRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await oidcAuthorizationService.CompleteAuthorize(request, RequestIpAddress, RequestUserAgent, cancellationToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException error)
            {
                return Unauthorized(new
                {
                    error = error.Message
                });
            }
            catch (InvalidOperationException error)
            {
                return BadRequest(new
                {
                    error = error.Message
                });
            }
        }

        [PostEndpoint("complete-authorize")]
        public async Task<IActionResult> AuthorizeWithCredentials([FromBody] OidcAuthorizeWithCredentialsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await oidcAuthorizationService.AuthorizeWithCredentials(request, RequestIpAddress, RequestUserAgent, cancellationToken);
                return Ok(response);
            }
            catch (UnauthorizedAccessException error)
            {
                return Unauthorized(new { error = error.Message });
            }
            catch (InvalidOperationException error)
            {
                return BadRequest(new { error = error.Message });
            }
        }

        private void ApplyBasicClientCredentials(OidcTokenRequest request)
        {
            (string ClientId, string ClientSecret)? credentials = GetBasicClientCredentials();
            if (!credentials.HasValue)
            {
                return;
            }

            request.ClientId = credentials.Value.ClientId;
            request.ClientSecret = credentials.Value.ClientSecret;
        }

        private void ApplyBasicClientCredentials(OidcRevocationRequest request)
        {
            (string ClientId, string ClientSecret)? credentials = GetBasicClientCredentials();
            if (!credentials.HasValue)
            {
                return;
            }

            request.ClientId = credentials.Value.ClientId;
            request.ClientSecret = credentials.Value.ClientSecret;
        }

        private async Task<IActionResult> EndSession(OidcEndSessionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await oidcAuthorizationService.EndSession(request, cancellationToken);
                return string.IsNullOrWhiteSpace(response.RedirectUrl) ? Ok() : Redirect(response.RedirectUrl);
            }
            catch (InvalidOperationException error)
            {
                return BadRequest(new { error = error.Message });
            }
        }

        private string GetBearerToken()
        {
            string? authorization = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return authorization["Bearer ".Length..].Trim();
        }

        private (string ClientId, string ClientSecret)? GetBasicClientCredentials()
        {
            string? authorization = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                string encodedCredentials = authorization["Basic ".Length..].Trim();
                string decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                int separatorIndex = decodedCredentials.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    return null;
                }

                return (
                    Uri.UnescapeDataString(decodedCredentials[..separatorIndex]),
                    Uri.UnescapeDataString(decodedCredentials[(separatorIndex + 1)..]));
            }
            catch
            {
                return null;
            }
        }
    }
}
