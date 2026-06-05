using System.ComponentModel.DataAnnotations;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.SystemIntegrations
{
    public class SystemIntegrationParameterRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        public bool IsSecret { get; set; }

        public SystemIntegrationParameterSource ValueSource { get; set; } = SystemIntegrationParameterSource.Static;

        [StringLength(200)]
        public string? SourceAudience { get; set; }
    }
}
