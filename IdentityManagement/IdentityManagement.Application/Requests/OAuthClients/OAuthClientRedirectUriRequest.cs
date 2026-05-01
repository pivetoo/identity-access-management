using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.OAuthClients
{
    public sealed class OAuthClientRedirectUriRequest
    {
        public string Uri { get; set; } = string.Empty;

        public OAuthRedirectUriType Type { get; set; } = OAuthRedirectUriType.SignIn;
    }
}
