namespace IdentityManagement.Application.Services
{
    public interface ITenantProvisioner
    {
        string BuildTenantConnectionString(string databaseName, string audience);
        string GenerateApiKey();
        Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default);
        Task CreateDatabaseAsync(string databaseName, string audience, CancellationToken ct = default);
        Task DropDatabaseAsync(string databaseName, CancellationToken ct = default);
    }
}
