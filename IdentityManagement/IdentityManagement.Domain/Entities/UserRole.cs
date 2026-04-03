using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class UserRole : Entity
    {
        public long UserId { get; private set; }

        public long RoleId { get; private set; }

        public User User { get; private set; } = null!;

        public Role Role { get; private set; } = null!;

        public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? RevokedAt { get; private set; }

        public bool IsActive { get; private set; } = true;

        private UserRole()
        {
        }

        public UserRole(long userId, long roleId)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roleId));
            }

            UserId = userId;
            RoleId = roleId;
        }

        public bool IsValid()
        {
            return IsActive && !RevokedAt.HasValue;
        }

        public void Revoke()
        {
            IsActive = false;
            RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
