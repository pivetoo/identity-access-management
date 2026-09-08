using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    // Capacidade que o template carrega para os perfis criados no provisionamento de um contrato.
    // Guarda a chave, e nao o id do catalogo: o catalogo e reescrito a cada sync do sistema consumidor.
    public class SystemRoleTemplateCapability : Entity
    {
        public long SystemRoleTemplateId { get; private set; }

        public SystemRoleTemplate SystemRoleTemplate { get; private set; } = null!;

        public string CapabilityKey { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        private SystemRoleTemplateCapability()
        {
        }

        public SystemRoleTemplateCapability(long systemRoleTemplateId, string capabilityKey)
        {
            if (systemRoleTemplateId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemRoleTemplateId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

            SystemRoleTemplateId = systemRoleTemplateId;
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
