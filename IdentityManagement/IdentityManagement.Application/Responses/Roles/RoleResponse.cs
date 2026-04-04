namespace IdentityManagement.Application.Responses.Roles
{
    public class RoleResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public long ContractId { get; set; }

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }

        public IReadOnlyCollection<long> AccessResourceIds { get; set; } = [];

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
