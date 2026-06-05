using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.SystemIntegrations
{
    public class SystemIntegrationParameterResponse
    {
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        public bool IsSecret { get; set; }

        public SystemIntegrationParameterSource ValueSource { get; set; }

        public string? SourceAudience { get; set; }
    }
}
