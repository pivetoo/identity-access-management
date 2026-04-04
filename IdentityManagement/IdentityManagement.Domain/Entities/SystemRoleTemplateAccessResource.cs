using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    public class SystemRoleTemplateAccessResource : Entity
    {
        public long SystemRoleTemplateId { get; private set; }

        public long AccessResourceId { get; private set; }

        public SystemRoleTemplate SystemRoleTemplate { get; private set; } = null!;

        public AccessResource AccessResource { get; private set; } = null!;

        public bool IsActive { get; private set; } = true;

        private SystemRoleTemplateAccessResource()
        {
        }

        public SystemRoleTemplateAccessResource(long systemRoleTemplateId, long accessResourceId)
        {
            if (systemRoleTemplateId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemRoleTemplateId));
            }

            if (accessResourceId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(accessResourceId));
            }

            SystemRoleTemplateId = systemRoleTemplateId;
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
