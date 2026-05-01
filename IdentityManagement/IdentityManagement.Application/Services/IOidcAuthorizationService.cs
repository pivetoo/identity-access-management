using IdentityManagement.Application.Requests.Oidc;
using IdentityManagement.Application.Responses.Oidc;

namespace IdentityManagement.Application.Services
{
    public interface IOidcAuthorizationService
    {
        Task<OidcAuthorizeResult> Authorize(
            OidcAuthorizeRequest request,
            string authorizeUrl,
            long? currentUserId,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default);

        Task<OidcAuthorizeCompleteResponse> CompleteAuthorize(
            OidcCompleteAuthorizeRequest request,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default);

        Task<OidcTokenResponse> ExchangeToken(OidcTokenRequest request, CancellationToken cancellationToken = default);

        Task<Dictionary<string, object>> GetUserInfo(string accessToken, CancellationToken cancellationToken = default);

        Task RevokeToken(OidcRevocationRequest request, CancellationToken cancellationToken = default);

        Task<OidcEndSessionResult> EndSession(OidcEndSessionRequest request, CancellationToken cancellationToken = default);
    }
}
