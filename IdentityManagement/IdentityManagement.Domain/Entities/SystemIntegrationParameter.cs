using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class SystemIntegrationParameter : Entity
    {
        public long SystemIntegrationId { get; private set; }

        public string Key { get; private set; } = string.Empty;

        public string? Value { get; private set; }

        public bool IsSecret { get; private set; }

        public SystemIntegrationParameterSource ValueSource { get; private set; } = SystemIntegrationParameterSource.Static;

        public string? SourceAudience { get; private set; }

        private SystemIntegrationParameter()
        {
        }

        public SystemIntegrationParameter(long systemIntegrationId, string key, string? value, bool isSecret, SystemIntegrationParameterSource valueSource, string? sourceAudience)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (valueSource == SystemIntegrationParameterSource.TenantApiKey && string.IsNullOrWhiteSpace(sourceAudience))
            {
                throw new ArgumentException("SourceAudience is required when ValueSource is TenantApiKey.", nameof(sourceAudience));
            }

            if (valueSource == SystemIntegrationParameterSource.Static && value is null)
            {
                throw new ArgumentNullException(nameof(value), "Value is required when ValueSource is Static.");
            }

            SystemIntegrationId = systemIntegrationId;
            Key = key.Trim();
            ValueSource = valueSource;

            if (valueSource == SystemIntegrationParameterSource.TenantApiKey)
            {
                Value = null;
                SourceAudience = sourceAudience!.Trim();
            }
            else if (valueSource == SystemIntegrationParameterSource.TenantId || valueSource == SystemIntegrationParameterSource.GeneratedSecret)
            {
                Value = null;
                SourceAudience = null;
            }
            else
            {
                Value = value;
                SourceAudience = null;
            }

            IsSecret = isSecret;
        }
    }
}
