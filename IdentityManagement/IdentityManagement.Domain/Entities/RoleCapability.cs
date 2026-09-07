using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    // Guarda a chave, e nao o id do catalogo: o catalogo e reescrito a cada sync do sistema e o
    // vinculo do perfil precisa sobreviver a isso.
    public class RoleCapability : Entity
    {
        public long RoleId { get; private set; }

        public Role Role { get; private set; } = null!;

        public string CapabilityKey { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        private RoleCapability()
        {
        }

        public RoleCapability(long roleId, string capabilityKey)
        {
            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roleId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

            RoleId = roleId;
            CapabilityKey = capabilityKey.Trim();
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
