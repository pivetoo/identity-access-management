namespace IdentityManagement.Application.Responses.SystemRoleTemplates
{
    public class SystemRoleTemplateResponse
    {
        public long Id { get; set; }

        public long SystemApplicationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public IReadOnlyCollection<long> AccessResourceIds { get; set; } = [];

        public IReadOnlyCollection<string> CapabilityKeys { get; set; } = [];

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
