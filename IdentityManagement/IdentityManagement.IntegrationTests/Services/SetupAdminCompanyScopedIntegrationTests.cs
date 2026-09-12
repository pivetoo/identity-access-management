using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.Security;
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
        public async Task SetupAdmin_company_scoped_creates_one_user_with_root_role_only_for_systems_that_grant_admin()
        {
            List<string> createdDatabases = new();
            string invitationToken = string.Empty;
            long agencyAppIdForAssert = 0;

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);
                agencyAppIdForAssert = agencyAppId;

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

                // O token e guardado como hash (IDM-013): o valor em claro so existe no link enviado.
                invitationToken = NoOpEmailSender.ExtractToken(NoOpEmailSender.LastSetupLink);
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
                info!.SystemApplicationNames.Should().HaveCount(1, "o convite so pode prometer o que concede");
                info.SystemApplicationNames.Should().Contain("AgencyCampaign");
                info.SystemApplicationNames.Should().NotContain("IntegrationPlatform");

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

                userRoles.Should().HaveCount(1, "o IntegrationPlatform e o motor de integracoes: acesso so por concessao manual");

                // Os dois papeis raiz continuam existindo — o provisionamento nao mudou. O que mudou
                // e quem recebe: o do IntegrationPlatform fica sem dono ate alguem conceder.
                List<Role> rootRoles = await dbContext.Set<Role>()
                    .AsNoTracking()
                    .Where(r => r.IsRoot)
                    .ToListAsync();

                rootRoles.Should().HaveCount(2);

                long agencyContractId = await dbContext.Set<Contract>()
                    .AsNoTracking()
                    .Where(contract => contract.SystemApplicationId == agencyAppIdForAssert)
                    .Select(contract => contract.Id)
                    .FirstAsync();

                Role agencyRootRole = rootRoles.Single(role => role.ContractId == agencyContractId);
                userRoles.Should().ContainSingle(ur => ur.RoleId == agencyRootRole.Id);

                ContractAdminInvitation usedInvitation = await dbContext.Set<ContractAdminInvitation>()
                    .AsNoTracking()
                    .FirstAsync(i => i.Token == TokenHasher.Hash(invitationToken));

                usedInvitation.IsValid().Should().BeFalse();
            });
        }

        [Test]
        public async Task SetupAdmin_gives_the_second_company_to_the_account_that_already_runs_the_first()
        {
            // Uma pessoa, duas agencias, o mesmo e-mail. O convite da segunda NAO cria identidade
            // nova: reaproveita a conta existente, que passa a escolher a empresa no login.
            const string emailCompartilhado = "admin@duastenants.example";
            const string senha = "Senha@123456";

            List<string> createdDatabases = new();
            string primeiroToken = string.Empty;
            string segundoToken = string.Empty;

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                (long agencyAppId, long integrationAppId) = await SeedSystemApplications(dbContext);

                IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();

                var primeira = await onboarding.OnboardClient(
                    BuildOnboardRequest("Primeira Agencia LTDA", "Primeira", "11222333000144", emailCompartilhado, agencyAppId, integrationAppId),
                    "https://auth.mainstay.com.br");
                createdDatabases.AddRange(primeira.DatabaseNames);
                primeiroToken = NoOpEmailSender.ExtractToken(NoOpEmailSender.LastSetupLink);

                var segunda = await onboarding.OnboardClient(
                    BuildOnboardRequest("Segunda Agencia LTDA", "Segunda", "11222333000181", emailCompartilhado, agencyAppId, integrationAppId),
                    "https://auth.mainstay.com.br");
                createdDatabases.AddRange(segunda.DatabaseNames);
                segundoToken = NoOpEmailSender.ExtractToken(NoOpEmailSender.LastSetupLink);
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

                AdminInvitationInfoResponse? primeiraInfo = await authService.ValidateAdminInvitation(primeiroToken);
                primeiraInfo.Should().NotBeNull();
                primeiraInfo!.UserExists.Should().BeFalse("nenhuma conta usa esse e-mail ainda");

                bool criou = await authService.SetupAdmin(
                    new SetupAdminRequest(primeiroToken, "Admin Duas Tenants", "admin.duastenants", emailCompartilhado, senha));
                criou.Should().BeTrue();

                AdminInvitationInfoResponse? segundaInfo = await authService.ValidateAdminInvitation(segundoToken);
                segundaInfo.Should().NotBeNull();
                segundaInfo!.UserExists.Should().BeTrue("a tela precisa abrir direto no modo 'ja tenho conta'");

                bool vinculou = await authService.SetupAdminExistingUser(
                    new SetupAdminExistingUserRequest(segundoToken, emailCompartilhado, senha));
                vinculou.Should().BeTrue();

                List<User> usuarios = await dbContext.Set<User>().AsNoTracking().ToListAsync();
                usuarios.Should().HaveCount(1, "a segunda agencia reaproveita a conta em vez de duplicar identidade");

                List<UserRole> vinculos = await dbContext.Set<UserRole>()
                    .AsNoTracking()
                    .Where(item => item.UserId == usuarios[0].Id)
                    .ToListAsync();

                vinculos.Should().HaveCount(2, "uma empresa, um vinculo: o IntegrationPlatform nao entra no convite");

                // Os papeis raiz do IntegrationPlatform existem nos dois contratos — o provisionamento
                // continua igual. O que mudou e quem recebe: ninguem, ate um administrador conceder.
                List<Role> raizes = await dbContext.Set<Role>().AsNoTracking().Where(item => item.IsRoot).ToListAsync();
                raizes.Should().HaveCount(4);

                List<string> audiencesConcedidas = await (
                    from vinculo in dbContext.Set<UserRole>().AsNoTracking()
                    join role in dbContext.Set<Role>().AsNoTracking() on vinculo.RoleId equals role.Id
                    join contract in dbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                    join app in dbContext.Set<SystemApplication>().AsNoTracking() on contract.SystemApplicationId equals app.Id
                    where vinculo.UserId == usuarios[0].Id
                    select app.Audience).ToListAsync();

                audiencesConcedidas.Should().OnlyContain(audience => audience == "agency-campaign");
            });
        }

        private static OnboardClientRequest BuildOnboardRequest(string legalName, string tradeName, string document, string email, long agencyAppId, long integrationAppId)
        {
            return new OnboardClientRequest
            {
                LegalName = legalName,
                TradeName = tradeName,
                Document = document,
                Email = email,
                Systems = new List<OnboardClientSystemItem>
                {
                    new OnboardClientSystemItem { SystemApplicationId = agencyAppId, StartDate = DateTimeOffset.UtcNow },
                    new OnboardClientSystemItem { SystemApplicationId = integrationAppId, StartDate = DateTimeOffset.UtcNow }
                }
            };
        }

        private static async Task<(long agencyAppId, long integrationAppId)> SeedSystemApplications(DbContext dbContext)
        {
            SystemApplication agencyApp = new SystemApplication("AgencyCampaign", "Mainstay", "agency-campaign", ApplicationType.External);

            // Motor de integracoes: provisionado junto (o AgencyCampaign depende da API key do
            // tenant), mas fora do convite de administrador.
            SystemApplication integrationApp = new SystemApplication(
                "IntegrationPlatform", "Plataforma de Integracoes", "integration-platform", ApplicationType.External, grantsAdminOnSetup: false);

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
