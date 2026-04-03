using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Responses.SystemApplications
{
    public class SystemApplicationResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string RedirectUris { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string Audience { get; set; } = string.Empty;

        public ApplicationType Type { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
