using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class OAuthClientRedirectUri : Entity
    {
        public long OAuthClientId { get; private set; }

        public OAuthClient OAuthClient { get; private set; } = null!;

        public string Uri { get; private set; } = string.Empty;

        public OAuthRedirectUriType Type { get; private set; } = OAuthRedirectUriType.SignIn;

        public bool IsActive { get; private set; } = true;

        private OAuthClientRedirectUri()
        {
        }

        public OAuthClientRedirectUri(long oAuthClientId, string uri, OAuthRedirectUriType type)
        {
            if (oAuthClientId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oAuthClientId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(uri);

            OAuthClientId = oAuthClientId;
            Uri = uri.Trim();
            Type = type;
        }

        public void Update(string uri, OAuthRedirectUriType type, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(uri);

            Uri = uri.Trim();
            Type = type;
            IsActive = isActive;
        }
    }
}
