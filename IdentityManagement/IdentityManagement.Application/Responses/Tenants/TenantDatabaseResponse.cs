namespace IdentityManagement.Application.Responses.Tenants
{
    public sealed class TenantDatabaseResponse
    {
        public long Id { get; set; }

        public long ContractId { get; set; }

        public Guid TenantId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string SystemApplicationName { get; set; } = string.Empty;

        public string ApplicationId { get; set; } = string.Empty;

        public string ConnectionString { get; set; } = string.Empty;

        public int DatabaseProvider { get; set; }

        public string SchemaName { get; set; } = "public";

        public string IntegrationSecret { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
