using IdentityManagement.Application.Requests.Oidc;
using IdentityManagement.Application.Responses.Oidc;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class OidcAuthorizationService : IOidcAuthorizationService
    {
        private readonly DbContext dbContext;
        private readonly IJwtService jwtService;
        private readonly ILoginSessionService loginSessionService;
        private readonly IRefreshTokenService refreshTokenService;
        private readonly IConfiguration configuration;
        private readonly IContractService contractService;

        public OidcAuthorizationService(DbContext dbContext, IJwtService jwtService, ILoginSessionService loginSessionService, IRefreshTokenService refreshTokenService, IConfiguration configuration, IContractService contractService)
        {
            this.dbContext = dbContext;
            this.jwtService = jwtService;
            this.loginSessionService = loginSessionService;
            this.refreshTokenService = refreshTokenService;
            this.configuration = configuration;
            this.contractService = contractService;
        }

        public async Task<OidcAuthorizeResult> Authorize(
            OidcAuthorizeRequest request,
            string authorizeUrl,
            long? currentUserId,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            OAuthClient? client = await GetClient(request.ClientId, cancellationToken);
            if (client is null)
            {
                return Error("invalid_client", "Client is invalid or inactive.");
            }

            if (!IsResponseTypeValid(request.ResponseType))
            {
                return RedirectError(request.RedirectUri, request.State, "unsupported_response_type", "Only response_type=code is supported.");
            }

            if (!IsRedirectUriAllowed(client, request.RedirectUri, OAuthRedirectUriType.SignIn))
            {
                return Error("invalid_request", "redirect_uri is invalid.");
            }

            if (!AreScopesAllowed(client, request.Scope))
            {
                return RedirectError(request.RedirectUri, request.State, "invalid_scope", "One or more scopes are not allowed for this client.");
            }

            if (!IsPkceValid(client, request))
            {
                return RedirectError(request.RedirectUri, request.State, "invalid_request", "PKCE code_challenge is required and must use S256.");
            }

            if (RequiresOpenId(request.Scope) && string.IsNullOrWhiteSpace(request.Nonce))
            {
                return RedirectError(request.RedirectUri, request.State, "invalid_request", "nonce is required for OpenID Connect requests.");
            }

            if (!currentUserId.HasValue || !request.ContractId.HasValue)
            {
                string? loginPageUrl = configuration["Oidc:LoginPageUrl"];
                string loginUrl = loginPageUrl is not null
                    ? $"{loginPageUrl.TrimEnd('/')}/?returnUrl={Uri.EscapeDataString(authorizeUrl)}"
                    : $"/?returnUrl={Uri.EscapeDataString(authorizeUrl)}";
                return new OidcAuthorizeResult
                {
                    IsRedirect = true,
                    RedirectUrl = loginUrl
                };
            }

            Contract? contract = await GetUserContract(currentUserId.Value, request.ContractId.Value, client.SystemApplicationId, cancellationToken);
            if (contract is null)
            {
                return RedirectError(request.RedirectUri, request.State, "access_denied", "User does not have access to the requested contract.");
            }

            User? user = await dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == currentUserId.Value && item.IsActive, cancellationToken);

            if (user is null)
            {
                return RedirectError(request.RedirectUri, request.State, "access_denied", "User is invalid or inactive.");
            }

            LoginSession session = await loginSessionService.CreateSession(user, contract, ipAddress, userAgent, client.AccessTokenLifetime, cancellationToken);
            string code = GenerateOpaqueToken();
            AuthorizationCode authorizationCode = new(
                code,
                user.Id,
                client.ClientId,
                NormalizeScopes(request.Scope),
                request.RedirectUri,
                session.SessionId,
                5,
                contract.Id,
                request.Nonce,
                request.CodeChallenge,
                request.CodeChallengeMethod);

            dbContext.Set<AuthorizationCode>().Add(authorizationCode);
            await dbContext.SaveChangesAsync(cancellationToken);

            string redirectUrl = AppendQuery(request.RedirectUri, new Dictionary<string, string?>
            {
                ["code"] = code,
                ["state"] = request.State
            });

            return new OidcAuthorizeResult
            {
                IsRedirect = true,
                RedirectUrl = redirectUrl
            };
        }

        public async Task<OidcTokenResponse> ExchangeToken(OidcTokenRequest request, CancellationToken cancellationToken = default)
        {
            if (string.Equals(request.GrantType, "refresh_token", StringComparison.Ordinal))
            {
                return await RefreshToken(request, cancellationToken);
            }

            if (!string.Equals(request.GrantType, "authorization_code", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("unsupported_grant_type");
            }

            AuthorizationCode? authorizationCode = await dbContext.Set<AuthorizationCode>()
                .AsTracking()
                .Include(item => item.User)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.Company)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.SystemApplication)
                .FirstOrDefaultAsync(item => item.Code == request.Code, cancellationToken);

            if (authorizationCode is null || !authorizationCode.IsValid() || authorizationCode.Contract is null)
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            if (!string.Equals(authorizationCode.ClientId, request.ClientId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            if (!string.Equals(authorizationCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            OAuthClient? client = await GetClient(request.ClientId, cancellationToken);
            if (client is null)
            {
                throw new UnauthorizedAccessException("invalid_client");
            }

            if (!ValidateClientSecret(client, request.ClientSecret))
            {
                throw new UnauthorizedAccessException("invalid_client");
            }

            if (!ValidateCodeVerifier(authorizationCode, request.CodeVerifier))
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            string accessToken = await jwtService.GenerateAccessToken(
                authorizationCode.User,
                authorizationCode.Contract,
                client.AccessTokenLifetime,
                authorizationCode.SessionId,
                client.ClientId,
                cancellationToken);
            string idToken = await jwtService.GenerateIdToken(
                authorizationCode.User,
                authorizationCode.Contract,
                request.ClientId,
                client.IdentityTokenLifetime,
                authorizationCode.Nonce,
                authorizationCode.SessionId,
                cancellationToken);
            RefreshToken? refreshToken = null;

            if (client.AllowOfflineAccess && HasScope(authorizationCode.Scopes, "offline_access"))
            {
                refreshToken = await refreshTokenService.CreateRefreshToken(
                    authorizationCode.User,
                    authorizationCode.Contract,
                    client.RefreshTokenLifetime,
                    authorizationCode.SessionId,
                    authorizationCode.Scopes,
                    client.ClientId,
                    cancellationToken);
            }

            DateTimeOffset tokenExpiration = jwtService.GetTokenExpiration(accessToken);
            authorizationCode.MarkAsUsed(tokenExpiration);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new OidcTokenResponse
            {
                AccessToken = accessToken,
                IdToken = idToken,
                RefreshToken = refreshToken?.Token,
                TokenType = "Bearer",
                ExpiresIn = Math.Max(0, Convert.ToInt32((tokenExpiration - DateTimeOffset.UtcNow).TotalSeconds)),
                Scope = authorizationCode.Scopes
            };
        }

        private async Task<OidcTokenResponse> RefreshToken(OidcTokenRequest request, CancellationToken cancellationToken)
        {
            OAuthClient? client = await GetClient(request.ClientId, cancellationToken);
            if (client is null)
            {
                throw new UnauthorizedAccessException("invalid_client");
            }

            if (!ValidateClientSecret(client, request.ClientSecret))
            {
                throw new UnauthorizedAccessException("invalid_client");
            }

            RefreshToken? existingRefreshToken = await dbContext.Set<RefreshToken>()
                .AsTracking()
                .Include(item => item.User)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.Company)
                .Include(item => item.Contract!)
                    .ThenInclude(item => item!.SystemApplication)
                .FirstOrDefaultAsync(
                    item => item.Token == request.RefreshToken &&
                            item.ClientId == request.ClientId &&
                            !item.IsRevoked &&
                            DateTimeOffset.UtcNow < item.ExpiresAt,
                    cancellationToken);

            if (existingRefreshToken is null || existingRefreshToken.Contract is null)
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            if (!client.AllowOfflineAccess)
            {
                throw new UnauthorizedAccessException("invalid_grant");
            }

            existingRefreshToken.Revoke();
            existingRefreshToken.MarkAsUsed();

            string accessToken = await jwtService.GenerateAccessToken(
                existingRefreshToken.User,
                existingRefreshToken.Contract,
                client.AccessTokenLifetime,
                existingRefreshToken.SessionId,
                client.ClientId,
                cancellationToken);

            string idToken = HasScope(existingRefreshToken.Scopes, "openid")
                ? await jwtService.GenerateIdToken(
                    existingRefreshToken.User,
                    existingRefreshToken.Contract,
                    request.ClientId,
                    client.IdentityTokenLifetime,
                    null,
                    existingRefreshToken.SessionId,
                    cancellationToken)
                : string.Empty;

            RefreshToken newRefreshToken = await refreshTokenService.CreateRefreshToken(
                existingRefreshToken.User,
                existingRefreshToken.Contract,
                client.RefreshTokenLifetime,
                existingRefreshToken.SessionId,
                existingRefreshToken.Scopes,
                client.ClientId,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            DateTimeOffset tokenExpiration = jwtService.GetTokenExpiration(accessToken);
            return new OidcTokenResponse
            {
                AccessToken = accessToken,
                IdToken = idToken,
                RefreshToken = newRefreshToken.Token,
                TokenType = "Bearer",
                ExpiresIn = Math.Max(0, Convert.ToInt32((tokenExpiration - DateTimeOffset.UtcNow).TotalSeconds)),
                Scope = existingRefreshToken.Scopes
            };
        }

        public async Task<OidcAuthorizeCompleteResponse> CompleteAuthorize(
            OidcCompleteAuthorizeRequest request,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            PendingAuthorizationSession? authorizationSession = await dbContext.Set<PendingAuthorizationSession>()
                .AsTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.Token == request.AuthorizationSessionToken, cancellationToken);

            if (authorizationSession is null || !authorizationSession.IsValid() || !authorizationSession.User.IsActive)
            {
                throw new UnauthorizedAccessException("invalid_authorization_session");
            }

            if (!MatchesAuthorizeRequestContext(authorizationSession, request.AuthorizeUrl))
            {
                throw new UnauthorizedAccessException("invalid_authorization_session");
            }

            var availableContracts = await contractService.GetActiveContractSelectionsByUserId(authorizationSession.UserId, cancellationToken);
            if (!availableContracts.Any(item => item.ContractId == request.ContractId))
            {
                throw new UnauthorizedAccessException("invalid_contract");
            }

            Dictionary<string, string> parameters;
            try
            {
                parameters = ParseQuery(new Uri(request.AuthorizeUrl).Query);
            }
            catch (UriFormatException)
            {
                throw new InvalidOperationException("invalid_authorize_url");
            }

            OidcAuthorizeRequest authorizeRequest = new()
            {
                ClientId = parameters.GetValueOrDefault("client_id") ?? string.Empty,
                RedirectUri = parameters.GetValueOrDefault("redirect_uri") ?? string.Empty,
                ResponseType = parameters.GetValueOrDefault("response_type") ?? string.Empty,
                Scope = parameters.GetValueOrDefault("scope") ?? string.Empty,
                State = parameters.GetValueOrDefault("state") ?? string.Empty,
                Nonce = parameters.GetValueOrDefault("nonce") ?? string.Empty,
                CodeChallenge = parameters.GetValueOrDefault("code_challenge") ?? string.Empty,
                CodeChallengeMethod = parameters.GetValueOrDefault("code_challenge_method") ?? string.Empty,
                ContractId = request.ContractId
            };

            authorizationSession.MarkAsUsed();

            OidcAuthorizeResult result = await Authorize(
                authorizeRequest,
                request.AuthorizeUrl,
                authorizationSession.UserId,
                ipAddress,
                userAgent,
                cancellationToken);

            if (!result.IsRedirect)
            {
                throw new InvalidOperationException(result.Error ?? "invalid_request");
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new OidcAuthorizeCompleteResponse
            {
                RedirectUrl = result.RedirectUrl
            };
        }

        public async Task<Dictionary<string, object>> GetUserInfo(string accessToken, CancellationToken cancellationToken = default)
        {
            ClaimsPrincipal principal = await ValidateAccessToken(accessToken, cancellationToken);

            string? sessionId = principal.Claims.FirstOrDefault(item => item.Type == "session_id")?.Value;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new UnauthorizedAccessException("invalid_token");
            }

            LoginSession? session = await dbContext.Set<LoginSession>()
                .AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.Contract)
                    .ThenInclude(item => item.Company)
                .Include(item => item.Contract)
                    .ThenInclude(item => item.SystemApplication)
                .FirstOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);

            if (session is null || !session.IsValid() || !session.User.IsActive)
            {
                throw new UnauthorizedAccessException("invalid_token");
            }

            return new Dictionary<string, object>
            {
                ["sub"] = session.User.Id.ToString(),
                ["name"] = session.User.Name,
                ["preferred_username"] = session.User.Username,
                ["email"] = session.User.Email,
                ["email_verified"] = true,
                ["contract_id"] = session.Contract.Id,
                ["company_name"] = session.Contract.Company.LegalName,
                ["system_application_name"] = session.Contract.SystemApplication.Name
            };
        }

        public async Task RevokeToken(OidcRevocationRequest request, CancellationToken cancellationToken = default)
        {
            OAuthClient? client = await GetClient(request.ClientId, cancellationToken);
            if (client is null || !ValidateClientSecret(client, request.ClientSecret))
            {
                throw new UnauthorizedAccessException("invalid_client");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return;
            }

            RefreshToken? refreshToken = await dbContext.Set<RefreshToken>()
                .AsTracking()
                .FirstOrDefaultAsync(
                    item => item.Token == request.Token &&
                            item.ClientId == client.ClientId &&
                            !item.IsRevoked,
                    cancellationToken);

            if (refreshToken is null)
            {
                return;
            }

            refreshToken.Revoke();

            if (!string.IsNullOrWhiteSpace(refreshToken.SessionId))
            {
                LoginSession? session = await dbContext.Set<LoginSession>()
                    .AsTracking()
                    .FirstOrDefaultAsync(
                        item => item.SessionId == refreshToken.SessionId && item.IsActive,
                        cancellationToken);

                session?.Revoke();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<OidcEndSessionResult> EndSession(OidcEndSessionRequest request, CancellationToken cancellationToken = default)
        {
            JwtSecurityToken? idToken = TryReadJwtToken(request.IdTokenHint);
            string clientId = !string.IsNullOrWhiteSpace(request.ClientId)
                ? request.ClientId
                : idToken?.Audiences.FirstOrDefault() ?? string.Empty;

            OAuthClient? client = string.IsNullOrWhiteSpace(clientId)
                ? null
                : await GetClient(clientId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri))
            {
                if (client is null || !IsRedirectUriAllowed(client, request.PostLogoutRedirectUri, OAuthRedirectUriType.PostLogout))
                {
                    throw new InvalidOperationException("invalid_request");
                }
            }

            ClaimsPrincipal? principal = client is null
                ? null
                : await TryValidateIdToken(request.IdTokenHint, client.ClientId, cancellationToken);

            string? sessionId = principal?.Claims.FirstOrDefault(item => item.Type == "sid")?.Value;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await RevokeSessionTokens(sessionId, cancellationToken);
            }

            string redirectUrl = string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri)
                ? string.Empty
                : AppendQuery(request.PostLogoutRedirectUri, new Dictionary<string, string?>
                {
                    ["state"] = request.State
                });

            return new OidcEndSessionResult
            {
                RedirectUrl = redirectUrl
            };
        }

        private Task<OAuthClient?> GetClient(string clientId, CancellationToken cancellationToken)
        {
            return dbContext.Set<OAuthClient>()
                .AsNoTracking()
                .Include(item => item.RedirectUris)
                .Include(item => item.Scopes)
                    .ThenInclude(item => item.OAuthScope)
                .FirstOrDefaultAsync(item => item.ClientId == clientId && item.IsActive, cancellationToken);
        }

        private async Task RevokeSessionTokens(string sessionId, CancellationToken cancellationToken)
        {
            LoginSession? session = await dbContext.Set<LoginSession>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.SessionId == sessionId && item.IsActive, cancellationToken);

            if (session is not null)
            {
                session.Revoke();
            }

            List<RefreshToken> refreshTokens = await dbContext.Set<RefreshToken>()
                .AsTracking()
                .Where(item => item.SessionId == sessionId && !item.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (RefreshToken refreshToken in refreshTokens)
            {
                refreshToken.Revoke();
            }

            if (session is not null || refreshTokens.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<ClaimsPrincipal> ValidateAccessToken(string accessToken, CancellationToken cancellationToken)
        {
            JwtSecurityToken jwtToken = ReadJwtToken(accessToken);
            string? sessionId = jwtToken.Claims.FirstOrDefault(item => item.Type == "session_id")?.Value;
            string? contractIdValue = jwtToken.Claims.FirstOrDefault(item => item.Type == "contract_id")?.Value;

            if (string.IsNullOrWhiteSpace(sessionId) || !long.TryParse(contractIdValue, out long contractId))
            {
                throw new UnauthorizedAccessException("invalid_token");
            }

            Contract? contract = await dbContext.Set<Contract>()
                .AsNoTracking()
                .Include(item => item.SystemApplication)
                .FirstOrDefaultAsync(item => item.Id == contractId, cancellationToken);

            if (contract is null)
            {
                throw new UnauthorizedAccessException("invalid_token");
            }

            List<SecurityKey> keys = await GetSigningSecurityKeys(cancellationToken);
            foreach (SecurityKey key in keys)
            {
                TokenValidationParameters parameters = CreateValidationParameters(contract.SystemApplication.Audience, key);
                try
                {
                    return new JwtSecurityTokenHandler().ValidateToken(accessToken, parameters, out _);
                }
                catch
                {
                    // Try the next active signing key.
                }
            }

            throw new UnauthorizedAccessException("invalid_token");
        }

        private async Task<ClaimsPrincipal?> TryValidateIdToken(string idToken, string clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return null;
            }

            List<SecurityKey> keys = await GetSigningSecurityKeys(cancellationToken);

            foreach (SecurityKey key in keys)
            {
                TokenValidationParameters parameters = CreateValidationParameters(clientId, key);
                try
                {
                    return new JwtSecurityTokenHandler().ValidateToken(idToken, parameters, out _);
                }
                catch
                {
                    // Try the next active signing key.
                }
            }

            return null;
        }

        private async Task<List<SecurityKey>> GetSigningSecurityKeys(CancellationToken cancellationToken)
        {
            List<SigningKey> signingKeys = await dbContext.Set<SigningKey>()
                .AsNoTracking()
                .Where(item => item.IsActive &&
                               !item.RevokedAt.HasValue &&
                               item.Algorithm == SecurityAlgorithms.RsaSha256 &&
                               DateTimeOffset.UtcNow >= item.NotBefore &&
                               (!item.ExpiresAt.HasValue || DateTimeOffset.UtcNow < item.ExpiresAt.Value))
                .OrderByDescending(item => item.NotBefore)
                .ToListAsync(cancellationToken);

            List<SecurityKey> keys = [];
            foreach (SigningKey signingKey in signingKeys)
            {
                try
                {
                    RSA rsa = RSA.Create();
                    rsa.ImportFromPem(signingKey.PublicKeyPem);
                    keys.Add(new RsaSecurityKey(rsa)
                    {
                        KeyId = signingKey.KeyId
                    });
                }
                catch
                {
                    // Ignore invalid persisted keys and continue with other active keys.
                }
            }

            return keys;
        }

        private TokenValidationParameters CreateValidationParameters(string audience, SecurityKey key)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }

        private Task<Contract?> GetUserContract(long userId, long contractId, long systemApplicationId, CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return (
                from userRole in dbContext.Set<UserRole>().AsNoTracking()
                join role in dbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join contract in dbContext.Set<Contract>()
                    .Include(item => item.Company)
                    .Include(item => item.SystemApplication)
                    .AsNoTracking() on role.ContractId equals contract.Id
                where userRole.UserId == userId &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue &&
                      contract.Id == contractId &&
                      contract.SystemApplicationId == systemApplicationId &&
                      contract.IsActive &&
                      now >= contract.StartDate &&
                      (!contract.EndDate.HasValue || now <= contract.EndDate.Value)
                select contract)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static bool IsResponseTypeValid(string responseType)
        {
            return string.Equals(responseType, "code", StringComparison.Ordinal);
        }

        private static bool IsRedirectUriAllowed(OAuthClient client, string redirectUri, OAuthRedirectUriType type)
        {
            return client.RedirectUris.Any(item =>
                item.IsActive &&
                item.Type == type &&
                string.Equals(item.Uri, redirectUri, StringComparison.Ordinal));
        }

        private static bool AreScopesAllowed(OAuthClient client, string scope)
        {
            List<string> requestedScopes = SplitScopes(scope);
            if (requestedScopes.Count == 0)
            {
                return false;
            }

            HashSet<string> allowedScopes = client.Scopes
                .Where(item => item.IsActive && item.OAuthScope.IsActive)
                .Select(item => item.OAuthScope.Name)
                .ToHashSet(StringComparer.Ordinal);

            return requestedScopes.All(allowedScopes.Contains);
        }

        private static bool IsPkceValid(OAuthClient client, OidcAuthorizeRequest request)
        {
            if (!client.RequirePkce)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(request.CodeChallenge) &&
                   string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal);
        }

        private static bool ValidateClientSecret(OAuthClient client, string clientSecret)
        {
            if (client.ClientType != OAuthClientType.Confidential)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(client.ClientSecretHash) &&
                   !string.IsNullOrWhiteSpace(clientSecret) &&
                   BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash);
        }

        private static bool ValidateCodeVerifier(AuthorizationCode authorizationCode, string codeVerifier)
        {
            if (string.IsNullOrWhiteSpace(authorizationCode.CodeChallenge))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(codeVerifier) ||
                !string.Equals(authorizationCode.CodeChallengeMethod, "S256", StringComparison.Ordinal))
            {
                return false;
            }

            byte[] hash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
            string challenge = Base64UrlEncoder.Encode(hash);
            return string.Equals(challenge, authorizationCode.CodeChallenge, StringComparison.Ordinal);
        }

        private static string GenerateOpaqueToken()
        {
            return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        }

        private static bool RequiresOpenId(string scope)
        {
            return HasScope(scope, "openid");
        }

        private static bool HasScope(string scope, string value)
        {
            return SplitScopes(scope).Contains(value, StringComparer.Ordinal);
        }

        private static string NormalizeScopes(string scope)
        {
            return string.Join(' ', SplitScopes(scope).Distinct(StringComparer.Ordinal));
        }

        private static List<string> SplitScopes(string scope)
        {
            return scope
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static OidcAuthorizeResult Error(string error, string description)
        {
            return new OidcAuthorizeResult
            {
                Error = error,
                ErrorDescription = description
            };
        }

        private static OidcAuthorizeResult RedirectError(string redirectUri, string state, string error, string description)
        {
            return new OidcAuthorizeResult
            {
                IsRedirect = true,
                RedirectUrl = AppendQuery(redirectUri, new Dictionary<string, string?>
                {
                    ["error"] = error,
                    ["error_description"] = description,
                    ["state"] = state
                })
            };
        }

        private static string AppendQuery(string url, IReadOnlyDictionary<string, string?> parameters)
        {
            UriBuilder builder = new(url);
            List<string> queryParts = string.IsNullOrWhiteSpace(builder.Query)
                ? []
                : [builder.Query.TrimStart('?')];

            queryParts.AddRange(parameters
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));

            builder.Query = string.Join('&', queryParts);
            return builder.Uri.ToString();
        }

        private static JwtSecurityToken ReadJwtToken(string token)
        {
            JwtSecurityToken? jwtToken = TryReadJwtToken(token);
            if (jwtToken is null)
            {
                throw new UnauthorizedAccessException("invalid_token");
            }

            return jwtToken;
        }

        private static JwtSecurityToken? TryReadJwtToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                return new JwtSecurityTokenHandler().ReadJwtToken(token);
            }
            catch
            {
                return null;
            }
        }

        private static OidcAuthorizeRequest ParseAuthorizeUrl(string authorizeUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authorizeUrl);

            Uri uri = new(authorizeUrl);
            Dictionary<string, string> query = ParseQuery(uri.Query);

            return new OidcAuthorizeRequest
            {
                ClientId = query.GetValueOrDefault("client_id") ?? string.Empty,
                RedirectUri = query.GetValueOrDefault("redirect_uri") ?? string.Empty,
                ResponseType = query.GetValueOrDefault("response_type") ?? string.Empty,
                Scope = query.GetValueOrDefault("scope") ?? string.Empty,
                State = query.GetValueOrDefault("state") ?? string.Empty,
                Nonce = query.GetValueOrDefault("nonce") ?? string.Empty,
                CodeChallenge = query.GetValueOrDefault("code_challenge") ?? string.Empty,
                CodeChallengeMethod = query.GetValueOrDefault("code_challenge_method") ?? string.Empty
            };
        }

        private static Dictionary<string, string> ParseQuery(string queryString)
        {
            return queryString
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part =>
                {
                    string[] pair = part.Split('=', 2);
                    string key = Uri.UnescapeDataString(pair[0]);
                    string value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : string.Empty;
                    return new KeyValuePair<string, string>(key, value);
                })
                .GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
        }

        private static bool MatchesAuthorizeRequestContext(PendingAuthorizationSession authorizationSession, string authorizeUrl)
        {
            if (string.IsNullOrWhiteSpace(authorizationSession.AuthorizeRequestHash))
            {
                return true;
            }

            try
            {
                return string.Equals(
                    authorizationSession.AuthorizeRequestHash,
                    ComputeAuthorizeRequestHash(authorizeUrl),
                    StringComparison.Ordinal);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        private static string ComputeAuthorizeRequestHash(string authorizeUrl)
        {
            Uri uri = new(authorizeUrl);
            Dictionary<string, string> parameters = ParseQuery(uri.Query);
            string normalizedRequest = string.Join("|", [
                $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}",
                parameters.GetValueOrDefault("client_id") ?? string.Empty,
                parameters.GetValueOrDefault("redirect_uri") ?? string.Empty,
                parameters.GetValueOrDefault("response_type") ?? string.Empty,
                parameters.GetValueOrDefault("scope") ?? string.Empty,
                parameters.GetValueOrDefault("state") ?? string.Empty,
                parameters.GetValueOrDefault("nonce") ?? string.Empty,
                parameters.GetValueOrDefault("code_challenge") ?? string.Empty,
                parameters.GetValueOrDefault("code_challenge_method") ?? string.Empty
            ]);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRequest));
            return Base64UrlEncoder.Encode(hash);
        }
    }
}
