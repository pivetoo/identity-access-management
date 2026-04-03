using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class Contract : Entity
    {
        private readonly List<Role> roles = [];
        private readonly List<AuthorizationCode> authorizationCodes = [];
        private readonly List<RefreshToken> refreshTokens = [];

        public long CompanyId { get; private set; }

        public long SystemApplicationId { get; private set; }

        public Company Company { get; private set; } = null!;

        public SystemApplication SystemApplication { get; private set; } = null!;

        public DateTimeOffset StartDate { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? EndDate { get; private set; }

        public bool IsActive { get; private set; } = true;

        public string ClientId { get; private set; } = string.Empty;

        public string ClientSecret { get; private set; } = string.Empty;

        public int AccessTokenLifetime { get; private set; } = 3600;

        public int RefreshTokenLifetime { get; private set; } = 2592000;

        public string JwtSecretKey { get; private set; } = string.Empty;

        public IReadOnlyCollection<Role> Roles => roles.AsReadOnly();

        public IReadOnlyCollection<AuthorizationCode> AuthorizationCodes => authorizationCodes.AsReadOnly();

        public IReadOnlyCollection<RefreshToken> RefreshTokens => refreshTokens.AsReadOnly();

        private Contract()
        {
        }

        public Contract(long companyId, long systemApplicationId, string clientId, string clientSecret, string jwtSecretKey)
        {
            if (companyId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(companyId));
            }

            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
            ArgumentException.ThrowIfNullOrWhiteSpace(jwtSecretKey);

            CompanyId = companyId;
            SystemApplicationId = systemApplicationId;
            ClientId = clientId.Trim();
            ClientSecret = clientSecret.Trim();
            JwtSecretKey = jwtSecretKey.Trim();
        }

        public bool IsValid()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return IsActive && now >= StartDate && (!EndDate.HasValue || now <= EndDate.Value);
        }

        public void Update(long companyId, long systemApplicationId, DateTimeOffset startDate, DateTimeOffset? endDate, bool isActive, int accessTokenLifetime, int refreshTokenLifetime)
        {
            if (companyId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(companyId));
            }

            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            CompanyId = companyId;
            SystemApplicationId = systemApplicationId;
            StartDate = startDate;
            EndDate = endDate;
            IsActive = isActive;
            AccessTokenLifetime = accessTokenLifetime;
            RefreshTokenLifetime = refreshTokenLifetime;
        }
    }
}
