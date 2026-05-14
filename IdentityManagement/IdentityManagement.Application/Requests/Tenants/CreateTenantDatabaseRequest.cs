namespace IdentityManagement.Application.Requests.Tenants
{
    public sealed class CreateTenantDatabaseRequest
    {
        public long ContractId { get; set; }

        public string ConnectionString { get; set; } = string.Empty;

        public int DatabaseProvider { get; set; }

        public string? SchemaName { get; set; }

        public string IntegrationSecret { get; set; } = string.Empty;
    }
}
