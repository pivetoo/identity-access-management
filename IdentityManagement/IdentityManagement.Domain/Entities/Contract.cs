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

        public Guid TenantId { get; set; }

        public Company Company { get; private set; } = null!;

        public SystemApplication SystemApplication { get; private set; } = null!;

        public DateTimeOffset StartDate { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? EndDate { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<Role> Roles => roles.AsReadOnly();

        public IReadOnlyCollection<AuthorizationCode> AuthorizationCodes => authorizationCodes.AsReadOnly();

        public IReadOnlyCollection<RefreshToken> RefreshTokens => refreshTokens.AsReadOnly();

        private Contract()
        {
        }

        public Contract(long companyId, long systemApplicationId)
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
            TenantId = Guid.NewGuid();
        }

        public bool IsValid()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return IsActive && now >= StartDate && (!EndDate.HasValue || now <= EndDate.Value);
        }

        public void Update(long companyId, long systemApplicationId, DateTimeOffset startDate, DateTimeOffset? endDate, bool isActive)
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
            StartDate = NormalizeUtc(startDate);
            EndDate = endDate.HasValue ? NormalizeUtc(endDate.Value) : null;
            IsActive = isActive;
        }

        private static DateTimeOffset NormalizeUtc(DateTimeOffset value)
        {
            return value.ToUniversalTime();
        }
    }
}
