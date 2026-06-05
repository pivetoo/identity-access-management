namespace IdentityManagement.Application.Services
{
    public interface ITenantProvisioner
    {
        string BuildTenantConnectionString(string databaseName);
        string GenerateApiKey();
        Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default);
        Task CreateDatabaseAsync(string databaseName, CancellationToken ct = default);
        Task DropDatabaseAsync(string databaseName, CancellationToken ct = default);
    }
}
