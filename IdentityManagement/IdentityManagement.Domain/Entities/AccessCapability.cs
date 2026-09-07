using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    // Permissao de produto ("financeiro.aprovar") sincronizada pelo sistema consumidor. Agrupa os
    // endpoints que a declaram; o perfil marca capacidades e o token e expandido para os endpoints.
    public class AccessCapability : Entity
    {
        public long SystemApplicationId { get; private set; }

        public SystemApplication SystemApplication { get; private set; } = null!;

        public string CapabilityKey { get; private set; } = string.Empty;

        public string Module { get; private set; } = string.Empty;

        public string ModuleLabel { get; private set; } = string.Empty;

        public int ModuleOrder { get; private set; }

        public string Label { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public int SortOrder { get; private set; }

        public bool IsBaseline { get; private set; }

        public bool IsActive { get; private set; } = true;

        private AccessCapability()
        {
        }

        public AccessCapability(long systemApplicationId, string capabilityKey, string module, string moduleLabel, int moduleOrder, string label, string description, int sortOrder, bool isBaseline)
        {
            if (systemApplicationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemApplicationId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

            SystemApplicationId = systemApplicationId;
            CapabilityKey = capabilityKey.Trim();
            Update(module, moduleLabel, moduleOrder, label, description, sortOrder, isBaseline);
        }

        public void Update(string module, string moduleLabel, int moduleOrder, string label, string description, int sortOrder, bool isBaseline)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(module);
            ArgumentNullException.ThrowIfNull(moduleLabel);
            ArgumentNullException.ThrowIfNull(label);
            ArgumentNullException.ThrowIfNull(description);

            Module = module.Trim();
            ModuleLabel = string.IsNullOrWhiteSpace(moduleLabel) ? Module : moduleLabel.Trim();
            ModuleOrder = moduleOrder;
            Label = string.IsNullOrWhiteSpace(label) ? CapabilityKey : label.Trim();
            Description = description.Trim();
            SortOrder = sortOrder;
            IsBaseline = isBaseline;
        }

        public bool Matches(string module, string moduleLabel, int moduleOrder, string label, string description, int sortOrder, bool isBaseline)
        {
            return string.Equals(Module, module?.Trim(), StringComparison.Ordinal) &&
                string.Equals(ModuleLabel, string.IsNullOrWhiteSpace(moduleLabel) ? Module : moduleLabel.Trim(), StringComparison.Ordinal) &&
                ModuleOrder == moduleOrder &&
                string.Equals(Label, string.IsNullOrWhiteSpace(label) ? CapabilityKey : label.Trim(), StringComparison.Ordinal) &&
                string.Equals(Description, description?.Trim() ?? string.Empty, StringComparison.Ordinal) &&
                SortOrder == sortOrder &&
                IsBaseline == isBaseline;
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
