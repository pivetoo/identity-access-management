using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class OAuthClientScope : Entity
    {
        public long OAuthClientId { get; private set; }

        public long OAuthScopeId { get; private set; }

        public OAuthClient OAuthClient { get; private set; } = null!;

        public OAuthScope OAuthScope { get; private set; } = null!;

        public bool IsActive { get; private set; } = true;

        private OAuthClientScope()
        {
        }

        public OAuthClientScope(long oAuthClientId, long oAuthScopeId)
        {
            if (oAuthClientId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oAuthClientId));
            }

            if (oAuthScopeId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oAuthScopeId));
            }

            OAuthClientId = oAuthClientId;
            OAuthScopeId = oAuthScopeId;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
