using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class IdentifySessionIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task IdentifyUserBySession_devolve_contratos_e_abre_sessao_pendente_sem_pedir_senha()
        {
            long userId = 0;

            await InScopeAsync(async sp =>
            {
                IUserService userService = sp.GetRequiredService<IUserService>();
                User user = await userService.Register("sso.user", "sso@x.test", "Secret123!", "Usuario SSO");
                userId = user.Id;

                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedContractFor(dbContext, user.Id);
            });

            await InScopeAsync(async sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();
                DbContext dbContext = sp.GetRequiredService<DbContext>();

                ContractSelectionResponse response = await authService.IdentifyUserBySession(userId, null);

                response.AuthenticationStep.Should().Be("contractSelection");
                response.UserId.Should().Be(userId);
                response.AuthorizationSessionToken.Should().NotBeNullOrWhiteSpace();
                response.AvailableContracts.Should().ContainSingle()
                    .Which.Audience.Should().Be("agency-campaign");

                int pendingSessions = await dbContext.Set<PendingAuthorizationSession>().CountAsync(item => item.UserId == userId);
                pendingSessions.Should().Be(1);
            });
        }

        [Test]
        public async Task IdentifyUserBySession_recusa_usuario_inexistente()
        {
            await InScopeAsync(async sp =>
            {
                IAuthService authService = sp.GetRequiredService<IAuthService>();

                Func<Task> act = () => authService.IdentifyUserBySession(999_999, null);

                await act.Should().ThrowAsync<UnauthorizedAccessException>();
            });
        }

        private static async Task SeedContractFor(DbContext dbContext, long userId)
        {
            SystemApplication agencyApp = new SystemApplication("AgencyCampaign", "Mainstay", "agency-campaign", ApplicationType.External);
            Company company = new Company("Agencia SSO LTDA", "Agencia SSO", "44555666000177", "contato@sso.example", "11999990002");
            await dbContext.Set<SystemApplication>().AddAsync(agencyApp);
            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            Contract contract = new Contract(company.Id, agencyApp.Id);
            await dbContext.Set<Contract>().AddAsync(contract);
            await dbContext.SaveChangesAsync();

            Role role = new Role("Administrador", "Papel raiz", contract.Id, isRoot: true, isDefault: true);
            await dbContext.Set<Role>().AddAsync(role);
            await dbContext.SaveChangesAsync();

            await dbContext.Set<UserRole>().AddAsync(new UserRole(userId, role.Id));
            await dbContext.SaveChangesAsync();
        }
    }
}
