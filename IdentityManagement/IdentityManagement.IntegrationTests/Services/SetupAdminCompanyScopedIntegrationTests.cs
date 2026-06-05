using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Responses.Auth;
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
    public sealed class SetupAdminCompanyScopedIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task SetupAdmin_company_scoped_creates_one_user_with_root_role_for_each_contract()
        {
            List<string> createdDatabases = new();
            string invitationToken = string.Empty;

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);

                IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();

                OnboardClientRequest request = new OnboardClientRequest
                {
                    LegalName = "Empresa Admin Consolidado LTDA",
                    TradeName = "AdminConsolidado",
                    Document = "11222333000144",
                    Email = "admin@consolidado.example",
                    Systems = new List<OnboardClientSystemItem>
                    {
                        new OnboardClientSystemItem { SystemApplicationId = agencyAppId, StartDate = DateTimeOffset.UtcNow },
                        new OnboardClientSystemItem { SystemApplicationId = integrationAppId, StartDate = DateTimeOffset.UtcNow }
                    }
                };

                var response = await onboarding.OnboardClient(request, "https://auth.mainstay.com.br");
                createdDatabases.AddRange(response.DatabaseNames);

                ContractAdminInvitation invitation = await dbContext.Set<ContractAdminInvitation>()
                    .AsNoTracking()
                    .FirstAsync(i => i.CompanyId == response.CompanyId);

                invitationToken = invitation.Token;
            });

            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                foreach (string db in createdDatabases)
                {
                    await provisioner.DropDatabaseAsync(db);
                }
            });

            await InScopeAsync(async sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();
                DbContext dbContext = sp.GetRequiredService<DbContext>();

                AdminInvitationInfoResponse? info = await authService.ValidateAdminInvitation(invitationToken);

                info.Should().NotBeNull();
                info!.SystemApplicationNames.Should().HaveCount(2);
                info.SystemApplicationNames.Should().Contain("Kanvas");
                info.SystemApplicationNames.Should().Contain("Plataforma de Integracoes");

                SetupAdminRequest setupRequest = new SetupAdminRequest(
                    Token: invitationToken,
                    Name: "Admin Consolidado",
                    Username: "admin.consolidado",
                    Email: "admin@consolidado.example",
                    Password: "Senha@123456"
                );

                bool result = await authService.SetupAdmin(setupRequest);

                result.Should().BeTrue();

                List<User> users = await dbContext.Set<User>()
                    .AsNoTracking()
                    .ToListAsync();

                users.Should().HaveCount(1);

                User createdUser = users[0];
                createdUser.Username.Should().Be("admin.consolidado");

                List<UserRole> userRoles = await dbContext.Set<UserRole>()
                    .AsNoTracking()
                    .Where(ur => ur.UserId == createdUser.Id)
                    .ToListAsync();

                userRoles.Should().HaveCount(2);

                List<Role> rootRoles = await dbContext.Set<Role>()
                    .AsNoTracking()
                    .Where(r => r.IsRoot)
                    .ToListAsync();

                rootRoles.Should().HaveCount(2);

                foreach (Role rootRole in rootRoles)
                {
                    userRoles.Should().Contain(ur => ur.RoleId == rootRole.Id);
                }

                ContractAdminInvitation usedInvitation = await dbContext.Set<ContractAdminInvitation>()
                    .AsNoTracking()
                    .FirstAsync(i => i.Token == invitationToken);

                usedInvitation.IsValid().Should().BeFalse();
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
