using IdentityManagement.Application.Responses.Oidc;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class OidcDiscoveryService : IOidcDiscoveryService
    {
        private readonly DbContext dbContext;

        private readonly IConfiguration configuration;

        public OidcDiscoveryService(DbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        public async Task<OpenIdConfigurationResponse> GetConfiguration(string issuer, CancellationToken cancellationToken = default)
        {
            string normalizedIssuer = issuer.TrimEnd('/');
            List<string> scopes = await dbContext.Set<OAuthScope>()
                .AsNoTracking()
                .Where(scope => scope.IsActive)
                .OrderBy(scope => scope.Name)
                .Select(scope => scope.Name)
                .ToListAsync(cancellationToken);

            string? loginPageUrl = configuration["Oidc:LoginPageUrl"];

            return new OpenIdConfigurationResponse
            {
                Issuer = normalizedIssuer,
                AuthorizationEndpoint = $"{normalizedIssuer}/connect/authorize",
                TokenEndpoint = $"{normalizedIssuer}/connect/token",
                UserInfoEndpoint = $"{normalizedIssuer}/connect/userinfo",
                JwksUri = $"{normalizedIssuer}/.well-known/jwks.json",
                RevocationEndpoint = $"{normalizedIssuer}/connect/revocation",
                EndSessionEndpoint = $"{normalizedIssuer}/connect/logout",
                LoginPageUrl = loginPageUrl ?? $"{normalizedIssuer}/connect/authorize",
                ResponseTypesSupported = ["code"],
                GrantTypesSupported = ["authorization_code", "refresh_token"],
                SubjectTypesSupported = ["public"],
                IdTokenSigningAlgValuesSupported = ["RS256"],
                ScopesSupported = scopes.Count > 0 ? scopes : ["openid", "profile", "email", "offline_access"],
                TokenEndpointAuthMethodsSupported = ["none", "client_secret_basic", "client_secret_post"],
                CodeChallengeMethodsSupported = ["S256"]
            };
        }

        public async Task<JsonWebKeySetResponse> GetJsonWebKeySet(CancellationToken cancellationToken = default)
        {
            List<SigningKey> signingKeys = await dbContext.Set<SigningKey>()
                .AsNoTracking()
                .Where(key => key.IsActive && !key.RevokedAt.HasValue && key.Algorithm == "RS256")
                .OrderByDescending(key => key.NotBefore)
                .ToListAsync(cancellationToken);

            return new JsonWebKeySetResponse
            {
                Keys = signingKeys
                    .Where(key => key.IsValid())
                    .Select(TryCreateJsonWebKey)
                    .Where(key => key is not null)
                    .Cast<JsonWebKeyResponse>()
                    .ToList()
            };
        }

        private static JsonWebKeyResponse? TryCreateJsonWebKey(SigningKey signingKey)
        {
            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(signingKey.PublicKeyPem);
                RSAParameters parameters = rsa.ExportParameters(false);

                return new JsonWebKeyResponse
                {
                    KeyType = "RSA",
                    PublicKeyUse = "sig",
                    KeyId = signingKey.KeyId,
                    Algorithm = signingKey.Algorithm,
                    Modulus = Base64UrlEncoder.Encode(parameters.Modulus),
                    Exponent = Base64UrlEncoder.Encode(parameters.Exponent)
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
