using Archon.Infrastructure.Services;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.OAuthClients;
using IdentityManagement.Application.Responses.OAuthClients;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class OAuthClientService : CrudService<OAuthClient>, IOAuthClientService
    {
        private readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public OAuthClientService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> localizer) : base(dbContext)
        {
            Localizer = localizer;
        }

        public async Task<IReadOnlyCollection<OAuthClientResponse>> GetOAuthClients(CancellationToken cancellationToken = default)
        {
            List<OAuthClient> clients = await QueryClients()
                .OrderBy(item => item.ClientName)
                .ToListAsync(cancellationToken);

            return clients.Select(ToResponse).ToList();
        }

        public async Task<OAuthClientResponse?> GetOAuthClient(long id, CancellationToken cancellationToken = default)
        {
            OAuthClient? client = await QueryClients()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return client is null ? null : ToResponse(client);
        }

        public async Task<OAuthClientResponse> CreateOAuthClient(CreateOAuthClientRequest request, CancellationToken cancellationToken = default)
        {
            await ValidateClient(request.SystemApplicationId, request.ClientId, request.ClientType, request.ClientSecret, null, cancellationToken);

            OAuthClient client = new(request.SystemApplicationId, request.ClientId, request.ClientName, request.ClientType);
            client.Update(
                request.ClientName,
                request.ClientType,
                HashSecret(request.ClientType, request.ClientSecret),
                request.RequirePkce,
                request.RequireConsent,
                request.AllowOfflineAccess,
                true,
                request.AccessTokenLifetime,
                request.IdentityTokenLifetime,
                request.RefreshTokenLifetime,
                request.RefreshTokenRotationEnabled);

            DbContext.Set<OAuthClient>().Add(client);
            await DbContext.SaveChangesAsync(cancellationToken);
            await ReplaceRedirectUris(client.Id, request.RedirectUris, cancellationToken);
            await ReplaceScopes(client.Id, request.Scopes, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            OAuthClient created = await QueryClients().FirstAsync(item => item.Id == client.Id, cancellationToken);
            return ToResponse(created);
        }

        public async Task<OAuthClientResponse> UpdateOAuthClient(long id, UpdateOAuthClientRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            OAuthClient? client = await DbContext.Set<OAuthClient>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (client is null)
            {
                throw new InvalidOperationException(Localizer["oauthClient.notFound"]);
            }

            await ValidateClient(client.SystemApplicationId, client.ClientId, request.ClientType, request.ClientSecret, id, cancellationToken, request.RotateClientSecret);

            string clientSecretHash = request.RotateClientSecret
                ? HashSecret(request.ClientType, request.ClientSecret)
                : client.ClientSecretHash;

            client.Update(
                request.ClientName,
                request.ClientType,
                clientSecretHash,
                request.RequirePkce,
                request.RequireConsent,
                request.AllowOfflineAccess,
                request.IsActive,
                request.AccessTokenLifetime,
                request.IdentityTokenLifetime,
                request.RefreshTokenLifetime,
                request.RefreshTokenRotationEnabled);

            await ReplaceRedirectUris(client.Id, request.RedirectUris, cancellationToken);
            await ReplaceScopes(client.Id, request.Scopes, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            OAuthClient updated = await QueryClients().FirstAsync(item => item.Id == client.Id, cancellationToken);
            return ToResponse(updated);
        }

        private IQueryable<OAuthClient> QueryClients()
        {
            return DbContext.Set<OAuthClient>()
                .AsNoTracking()
                .Include(item => item.SystemApplication)
                .Include(item => item.RedirectUris)
                .Include(item => item.Scopes)
                    .ThenInclude(item => item.OAuthScope);
        }

        private async Task ValidateClient(
            long systemApplicationId,
            string clientId,
            OAuthClientType clientType,
            string clientSecret,
            long? currentClientId,
            CancellationToken cancellationToken,
            bool requireSecret = true)
        {
            bool systemExists = await DbContext.Set<SystemApplication>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == systemApplicationId && item.IsActive, cancellationToken);

            if (!systemExists)
            {
                throw new InvalidOperationException(Localizer["systemApplication.notFoundOrInactive"]);
            }

            bool clientIdExists = await DbContext.Set<OAuthClient>()
                .AsNoTracking()
                .AnyAsync(item => item.ClientId == clientId && (!currentClientId.HasValue || item.Id != currentClientId.Value), cancellationToken);

            if (clientIdExists)
            {
                throw new InvalidOperationException(Localizer["oauthClient.clientId.alreadyExists"]);
            }

            if (clientType == OAuthClientType.Confidential && requireSecret && string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException(Localizer["oauthClient.clientSecret.requiredForConfidential"]);
            }
        }

        private async Task ReplaceRedirectUris(long clientId, IReadOnlyCollection<OAuthClientRedirectUriRequest> redirectUris, CancellationToken cancellationToken)
        {
            if (redirectUris.Count == 0)
            {
                throw new InvalidOperationException(Localizer["oauthClient.redirectUris.required"]);
            }

            foreach (OAuthClientRedirectUriRequest redirectUri in redirectUris)
            {
                if (!Uri.TryCreate(redirectUri.Uri, UriKind.Absolute, out Uri? uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Host != "localhost" && uri.Host != "127.0.0.1"))
                {
                    throw new InvalidOperationException(Localizer["oauthClient.redirectUris.invalid"]);
                }
            }

            List<OAuthClientRedirectUri> existing = await DbContext.Set<OAuthClientRedirectUri>()
                .Where(item => item.OAuthClientId == clientId)
                .ToListAsync(cancellationToken);

            DbContext.Set<OAuthClientRedirectUri>().RemoveRange(existing);

            foreach (OAuthClientRedirectUriRequest redirectUri in redirectUris.DistinctBy(item => (item.Uri, item.Type)))
            {
                DbContext.Set<OAuthClientRedirectUri>().Add(new OAuthClientRedirectUri(clientId, redirectUri.Uri, redirectUri.Type));
            }
        }

        private async Task ReplaceScopes(long clientId, IReadOnlyCollection<string> scopeNames, CancellationToken cancellationToken)
        {
            List<string> normalizedScopes = scopeNames
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (normalizedScopes.Count == 0 || !normalizedScopes.Contains("openid", StringComparer.Ordinal))
            {
                throw new InvalidOperationException(Localizer["oauthClient.scopes.openidRequired"]);
            }

            List<OAuthClientScope> existing = await DbContext.Set<OAuthClientScope>()
                .Where(item => item.OAuthClientId == clientId)
                .ToListAsync(cancellationToken);

            DbContext.Set<OAuthClientScope>().RemoveRange(existing);

            List<OAuthScope> scopes = await DbContext.Set<OAuthScope>()
                .Where(item => normalizedScopes.Contains(item.Name) && item.IsActive)
                .ToListAsync(cancellationToken);

            if (scopes.Count != normalizedScopes.Count)
            {
                throw new InvalidOperationException(Localizer["oauthClient.scopes.invalidOrInactive"]);
            }

            foreach (OAuthScope scope in scopes)
            {
                DbContext.Set<OAuthClientScope>().Add(new OAuthClientScope(clientId, scope.Id));
            }
        }

        private static string HashSecret(OAuthClientType clientType, string clientSecret)
        {
            return clientType == OAuthClientType.Confidential
                ? BCrypt.Net.BCrypt.HashPassword(clientSecret)
                : string.Empty;
        }

        private static OAuthClientResponse ToResponse(OAuthClient client)
        {
            return new OAuthClientResponse
            {
                Id = client.Id,
                SystemApplicationId = client.SystemApplicationId,
                SystemApplicationName = client.SystemApplication.Name,
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                ClientType = client.ClientType,
                RequirePkce = client.RequirePkce,
                RequireConsent = client.RequireConsent,
                AllowOfflineAccess = client.AllowOfflineAccess,
                IsActive = client.IsActive,
                AccessTokenLifetime = client.AccessTokenLifetime,
                IdentityTokenLifetime = client.IdentityTokenLifetime,
                RefreshTokenLifetime = client.RefreshTokenLifetime,
                RefreshTokenRotationEnabled = client.RefreshTokenRotationEnabled,
                RedirectUris = client.RedirectUris
                    .OrderBy(item => item.Type)
                    .ThenBy(item => item.Uri)
                    .Select(item => new OAuthClientRedirectUriResponse
                    {
                        Id = item.Id,
                        Uri = item.Uri,
                        Type = item.Type,
                        IsActive = item.IsActive
                    })
                    .ToList(),
                Scopes = client.Scopes
                    .Where(item => item.IsActive && item.OAuthScope.IsActive)
                    .Select(item => item.OAuthScope.Name)
                    .OrderBy(item => item)
                    .ToList()
            };
        }
    }
}
