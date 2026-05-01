using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.OAuthClients
{
    public sealed class OAuthClientRedirectUriResponse
    {
        public long Id { get; set; }

        public string Uri { get; set; } = string.Empty;

        public OAuthRedirectUriType Type { get; set; }

        public bool IsActive { get; set; }
    }
}
