using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class RoleAccessResource : Entity
    {
        public long RoleId { get; private set; }

        public long AccessResourceId { get; private set; }

        public Role Role { get; private set; } = null!;

        public AccessResource AccessResource { get; private set; } = null!;

        public bool IsActive { get; private set; } = true;

        private RoleAccessResource()
        {
        }

        public RoleAccessResource(long roleId, long accessResourceId)
        {
            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roleId));
            }

            if (accessResourceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(accessResourceId));
            }

            RoleId = roleId;
            AccessResourceId = accessResourceId;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
