using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.Security;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SetupAdminExistingUserIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task SetupAdminExistingUser_vincula_conta_existente_sem_criar_usuario_novo()
        {
            List<string> createdDatabases = new();
            string invitationToken = string.Empty;
            int userCountAfterRegister = 0;

            // Etapa 1: registrar o usuario existente
            await InScopeAsync(async sp =>
            {
                IUserService userService = sp.GetRequiredService<IUserService>();
                await userService.Register("existing", "existing@x.test", "Secret123!", "Existing");

                DbContext dbContext = sp.GetRequiredService<DbContext>();
                userCountAfterRegister = await dbContext.Set<User>().CountAsync();
            });

            // Etapa 2: onboarding da empresa com 2 sistemas
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);

                IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();

                OnboardClientRequest request = new OnboardClientRequest
                {
                    LegalName = "Empresa Existing Admin LTDA",
                    TradeName = "ExistingAdmin",
                    Document = "33444555000166",
                    Email = "admin@existingadmin.example",
                    Systems = new List<OnboardClientSystemItem>
                    {
                        new OnboardClientSystemItem { SystemApplicationId = agencyAppId, StartDate = DateTimeOffset.UtcNow },
                        new OnboardClientSystemItem { SystemApplicationId = integrationAppId, StartDate = DateTimeOffset.UtcNow }
                    }
                };

                var response = await onboarding.OnboardClient(request, "https://auth.mainstay.com.br");
                createdDatabases.AddRange(response.DatabaseNames);

                // O token e guardado como hash (IDM-013): o valor em claro so existe no link enviado.
                invitationToken = NoOpEmailSender.ExtractToken(NoOpEmailSender.LastSetupLink);
            });

            // Etapa 3: remover bancos provisionados (cleanup)
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();
                foreach (string db in createdDatabases)
                {
                    await provisioner.DropDatabaseAsync(db);
                }
            });

            // Etapa 4: executar SetupAdminExistingUser
            await InScopeAsync(async sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();
                DbContext dbContext = sp.GetRequiredService<DbContext>();

                SetupAdminExistingUserRequest setupRequest = new SetupAdminExistingUserRequest(
                    Token: invitationToken,
                    UsernameOrEmail: "existing",
                    Password: "Secret123!"
                );

                bool result = await authService.SetupAdminExistingUser(setupRequest);

                result.Should().BeTrue();

                // Nenhum usuario novo deve ter sido criado
                int userCountAfterSetup = await dbContext.Set<User>().CountAsync();
                userCountAfterSetup.Should().Be(userCountAfterRegister);

                // Recuperar o usuario existente
                User existingUser = await dbContext.Set<User>()
                    .AsNoTracking()
                    .FirstAsync(u => u.Username == "existing");

                // O usuario deve ter UserRole para as root roles dos 2 contratos
                List<UserRole> userRoles = await dbContext.Set<UserRole>()
                    .AsNoTracking()
                    .Where(ur => ur.UserId == existingUser.Id)
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

                // O convite deve estar marcado como usado (invalido)
                ContractAdminInvitation usedInvitation = await dbContext.Set<ContractAdminInvitation>()
                    .AsNoTracking()
                    .FirstAsync(i => i.Token == TokenHasher.Hash(invitationToken));

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
    }
}
