using IdentityManagement.Infrastructure.Services;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class PostgresTenantProvisionerTests
    {
        private const string SelfConnection = "Host=db.example;Port=5432;Database=identitymanagement;Username=appdatabase;Password=admin123";

        [Test]
        public void BuildTenantConnectionString_uses_system_credential_when_configured()
        {
            TenantProvisioningOptions options = new()
            {
                SystemCredentials = new()
                {
                    ["agency-campaign"] = new DatabaseCredential { Username = "app_agencycampaign", Password = "secret" }
                }
            };
            PostgresTenantProvisioner provisioner = new(SelfConnection, options);

            string conn = provisioner.BuildTenantConnectionString("agencycampaign_master_12", "agency-campaign");

            Assert.That(conn, Does.Contain("Username=app_agencycampaign"));
            Assert.That(conn, Does.Contain("Password=secret"));
            Assert.That(conn, Does.Contain("Database=agencycampaign_master_12"));
        }

        [Test]
        public void BuildTenantConnectionString_normalizes_audience_key_ignoring_hyphens()
        {
            TenantProvisioningOptions options = new()
            {
                SystemCredentials = new()
                {
                    ["integrationplatform"] = new DatabaseCredential { Username = "app_integrationplatform", Password = "pw" }
                }
            };
            PostgresTenantProvisioner provisioner = new(SelfConnection, options);

            string conn = provisioner.BuildTenantConnectionString("integrationplatform_master_12", "integration-platform");

            Assert.That(conn, Does.Contain("Username=app_integrationplatform"));
        }

        [Test]
        public void BuildTenantConnectionString_falls_back_to_admin_credential_when_system_not_configured()
        {
            PostgresTenantProvisioner provisioner = new(SelfConnection, new TenantProvisioningOptions());

            string conn = provisioner.BuildTenantConnectionString("agencycampaign_master_12", "agency-campaign");

            Assert.That(conn, Does.Contain("Username=appdatabase"));
        }

        [Test]
        public void BuildTenantConnectionString_falls_back_to_admin_credential_when_admin_role_configured()
        {
            TenantProvisioningOptions options = new()
            {
                Admin = new DatabaseCredential { Username = "master_identitymanagement", Password = "mpw" }
            };
            PostgresTenantProvisioner provisioner = new(SelfConnection, options);

            string conn = provisioner.BuildTenantConnectionString("agencycampaign_master_12", "agency-campaign");

            Assert.That(conn, Does.Contain("Username=master_identitymanagement"));
        }

        [Test]
        public void Constructor_rejects_owner_role_that_is_not_a_valid_identifier()
        {
            TenantProvisioningOptions options = new()
            {
                SystemCredentials = new()
                {
                    ["agency-campaign"] = new DatabaseCredential { Username = "app\"; DROP DATABASE x", Password = "pw" }
                }
            };
            PostgresTenantProvisioner provisioner = new(SelfConnection, options);

            // O owner so e resolvido (e validado) na criacao do banco; a connection string nao valida o identificador.
            Assert.That(
                () => provisioner.CreateDatabaseAsync("agencycampaign_master_12", "agency-campaign"),
                Throws.InstanceOf<InvalidOperationException>());
        }
    }
}
