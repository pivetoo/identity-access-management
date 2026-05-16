namespace IdentityManagement.Application.Requests.Tenants
{
    public sealed class UpdateTenantDatabaseRequest
    {
        public long Id { get; set; }

        public string ConnectionString { get; set; } = string.Empty;

        public int DatabaseProvider { get; set; }

        public string? SchemaName { get; set; }

        public string ApiKey { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
