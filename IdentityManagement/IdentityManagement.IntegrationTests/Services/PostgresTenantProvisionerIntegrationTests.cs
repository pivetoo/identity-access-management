using IdentityManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class PostgresTenantProvisionerIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task Creates_and_drops_a_real_database()
        {
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                string db = "it_provision_test_db";
                (await provisioner.DatabaseExistsAsync(db)).Should().BeFalse();
                await provisioner.CreateDatabaseAsync(db, "agency-campaign");
                (await provisioner.DatabaseExistsAsync(db)).Should().BeTrue();
                provisioner.BuildTenantConnectionString(db, "agency-campaign").Should().Contain("Database=it_provision_test_db");
                await provisioner.DropDatabaseAsync(db);
                (await provisioner.DatabaseExistsAsync(db)).Should().BeFalse();
            });
        }

        [Test]
        public async Task CreateDatabase_rejects_invalid_identifier()
        {
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                Func<Task> act = () => provisioner.CreateDatabaseAsync("bad; DROP DATABASE x", "agency-campaign");
                await act.Should().ThrowAsync<ArgumentException>();
            });
        }
    }
}
