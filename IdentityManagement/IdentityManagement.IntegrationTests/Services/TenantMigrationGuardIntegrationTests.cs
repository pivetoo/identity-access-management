using Archon.Core.ValueObjects;
using Archon.Infrastructure.Migrations;
using IdentityManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Reflection;

namespace IdentityManagement.IntegrationTests.Services
{
    // Exercita o TenantMigrationGuard contra um Postgres real (Testcontainers): cria um banco vazio,
    // garante que ele esta sem tabelas, roda a migracao on-demand e confere que o schema foi criado.
    [TestFixture]
    public sealed class TenantMigrationGuardIntegrationTests : IntegrationTestBase
    {
        private const string TestDbName = "it_migrationguard_test";

        [Test]
        public async Task EnsureMigrated_creates_schema_on_empty_database()
        {
            ITenantProvisioner provisioner = TestHarness.Services.GetRequiredService<ITenantProvisioner>();

            try
            {
                await provisioner.CreateDatabaseAsync(TestDbName);
                string connectionString = provisioner.BuildTenantConnectionString(TestDbName);

                int tablesBefore = await CountPublicTables(connectionString);
                tablesBefore.Should().Be(0);

                Assembly[] migrationAssemblies = new[]
                {
                    typeof(DatabaseMigrator).Assembly,
                    typeof(IdentityManagement.Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly
                };

                TenantMigrationGuard guard = new TenantMigrationGuard(migrationAssemblies);

                guard.EnsureMigrated(connectionString, DatabaseProvider.PostgreSql, "public");

                int tablesAfter = await CountPublicTables(connectionString);
                tablesAfter.Should().BeGreaterThan(0);

                // Segunda chamada e idempotente/cacheada: nao deve lancar.
                Action secondCall = () => guard.EnsureMigrated(connectionString, DatabaseProvider.PostgreSql, "public");
                secondCall.Should().NotThrow();
            }
            finally
            {
                await provisioner.DropDatabaseAsync(TestDbName);
            }
        }

        private static async Task<int> CountPublicTables(string connectionString)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'";
            object? result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
