using Archon.Core.Entities;
using Archon.Core.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class TenantDatabase : Entity
    {
        public long ContractId { get; private set; }

        public Contract Contract { get; private set; } = null!;

        public string ConnectionString { get; private set; } = string.Empty;

        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.PostgreSql;

        public string SchemaName { get; private set; } = "public";

        public string IntegrationSecret { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        private TenantDatabase()
        {
        }

        public TenantDatabase(long contractId, string connectionString, DatabaseProvider databaseProvider, string integrationSecret, string? schemaName = null)
        {
            if (contractId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contractId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(integrationSecret);

            ContractId = contractId;
            ConnectionString = connectionString.Trim();
            DatabaseProvider = databaseProvider;
            IntegrationSecret = integrationSecret.Trim();
            SchemaName = NormalizeSchema(schemaName);
        }

        public void Update(string connectionString, DatabaseProvider databaseProvider, string integrationSecret, string? schemaName, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(integrationSecret);

            ConnectionString = connectionString.Trim();
            DatabaseProvider = databaseProvider;
            IntegrationSecret = integrationSecret.Trim();
            SchemaName = NormalizeSchema(schemaName);
            IsActive = isActive;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private static string NormalizeSchema(string? schemaName)
        {
            return string.IsNullOrWhiteSpace(schemaName) ? "public" : schemaName.Trim();
        }
    }
}
