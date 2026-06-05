using Archon.Infrastructure.RestApi;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Responses.Clients;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class ClientOnboardingServiceIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task OnboardClient_creates_company_contracts_databases_and_one_invitation()
        {
            List<string> createdDatabases = new();

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);

                IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();

                OnboardClientRequest request = new OnboardClientRequest
                {
                    LegalName = "Empresa Onboarding LTDA",
                    TradeName = "Onboarding",
                    Document = "12345678000199",
                    Email = "admin@onboarding.example",
                    PhoneNumber = "11999990000",
                    Systems = new List<OnboardClientSystemItem>
                    {
                        new OnboardClientSystemItem { SystemApplicationId = agencyAppId, StartDate = DateTimeOffset.UtcNow },
                        new OnboardClientSystemItem { SystemApplicationId = integrationAppId, StartDate = DateTimeOffset.UtcNow }
                    }
                };

                OnboardClientResponse response = await onboarding.OnboardClient(request, "https://auth.mainstay.com.br");
                createdDatabases.AddRange(response.DatabaseNames);

                response.ContractIds.Should().HaveCount(2);
                response.DatabaseNames.Should().HaveCount(2);

                (await dbContext.Set<Company>().CountAsync()).Should().Be(1);
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(2);
                (await dbContext.Set<TenantDatabase>().CountAsync()).Should().Be(2);

                List<ContractAdminInvitation> invitations = await dbContext.Set<ContractAdminInvitation>().ToListAsync();
                invitations.Should().HaveCount(1);
                invitations[0].CompanyId.Should().Be(response.CompanyId);

                foreach (string db in response.DatabaseNames)
                {
                    (await provisioner.DatabaseExistsAsync(db)).Should().BeTrue();
                }
            });

            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                foreach (string db in createdDatabases)
                {
                    await provisioner.DropDatabaseAsync(db);
                }
            });
        }

        [Test]
        public async Task OnboardClient_compensates_on_provisioning_failure()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);

                IContractService contractService = sp.GetRequiredService<IContractService>();
                FailingTenantProvisioner provisioner = new FailingTenantProvisioner();

                ClientOnboardingService onboarding = new ClientOnboardingService(
                    dbContext,
                    contractService,
                    provisioner,
                    new NoOpEmailSender(),
                    new RestApi(new HttpClient()),
                    NullLogger<ClientOnboardingService>.Instance);

                OnboardClientRequest request = new OnboardClientRequest
                {
                    LegalName = "Empresa Falha LTDA",
                    TradeName = "Falha",
                    Document = "98765432000111",
                    Email = "fail@onboarding.example",
                    Systems = new List<OnboardClientSystemItem>
                    {
                        new OnboardClientSystemItem { SystemApplicationId = agencyAppId, StartDate = DateTimeOffset.UtcNow },
                        new OnboardClientSystemItem { SystemApplicationId = integrationAppId, StartDate = DateTimeOffset.UtcNow }
                    }
                };

                Func<Task> act = () => onboarding.OnboardClient(request, "https://auth.mainstay.com.br");
                await act.Should().ThrowAsync<InvalidOperationException>();

                provisioner.CreatedDatabases.Should().HaveCount(1);
                provisioner.DroppedDatabases.Should().Contain(provisioner.CreatedDatabases[0]);

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0);
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(0);
                (await dbContext.Set<TenantDatabase>().CountAsync()).Should().Be(0);
            });
        }

        private static async Task<(long agencyAppId, long integrationAppId)> SeedSystemApplications(DbContext dbContext)
        {
            SystemApplication agencyApp = new SystemApplication("AgencyCampaign", "Kanvas", "agency-campaign", ApplicationType.External);
            SystemApplication integrationApp = new SystemApplication("IntegrationPlatform", "Plataforma de Integracoes", "integration-platform", ApplicationType.External);

            await dbContext.Set<SystemApplication>().AddRangeAsync(agencyApp, integrationApp);
            await dbContext.SaveChangesAsync();

            SystemRoleTemplate agencyRoot = new SystemRoleTemplate(agencyApp.Id, "Administrador", "Papel raiz", true, true);
            SystemRoleTemplate integrationRoot = new SystemRoleTemplate(integrationApp.Id, "Administrador", "Papel raiz", true, true);

            await dbContext.Set<SystemRoleTemplate>().AddRangeAsync(agencyRoot, integrationRoot);
            await dbContext.SaveChangesAsync();

            return (agencyApp.Id, integrationApp.Id);
        }

        private sealed class FailingTenantProvisioner : ITenantProvisioner
        {
            public List<string> CreatedDatabases { get; } = new();

            public List<string> DroppedDatabases { get; } = new();

            public string BuildTenantConnectionString(string databaseName, string audience)
            {
                return $"Host=localhost;Database={databaseName}";
            }

            public string GenerateApiKey()
            {
                return Guid.NewGuid().ToString("N");
            }

            public Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default)
            {
                return Task.FromResult(CreatedDatabases.Contains(databaseName));
            }

            public Task CreateDatabaseAsync(string databaseName, string audience, CancellationToken ct = default)
            {
                if (CreatedDatabases.Count >= 1)
                {
                    throw new InvalidOperationException("provisioning.failed");
                }

                CreatedDatabases.Add(databaseName);
                return Task.CompletedTask;
            }

            public Task DropDatabaseAsync(string databaseName, CancellationToken ct = default)
            {
                DroppedDatabases.Add(databaseName);
                return Task.CompletedTask;
            }
        }

        private sealed class NoOpEmailSender : IEmailSender
        {
            public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendAdminInvitationEmailAsync(string toEmail, string companyName, string systemApplicationName, string setupLink, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
