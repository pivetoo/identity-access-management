using Archon.Core.Access;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Responses.SystemRoleTemplates;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SystemRoleTemplateCapabilityIntegrationTests : IntegrationTestBase
    {
        private const string Audience = "template-capability-system";

        private static async Task<SystemApplication> SeedSystemApplicationAsync(DbContext dbContext)
        {
            SystemApplication application = new SystemApplication("Template Capabilities", "Test application", Audience, ApplicationType.External);
            await dbContext.Set<SystemApplication>().AddAsync(application);
            await dbContext.SaveChangesAsync();
            return application;
        }

        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document)
        {
            Company company = new Company("Agencia Templates LTDA", "Agencia Templates", document, "contato@templates.example", "11999990004");
            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();
            return company;
        }

        private static AccessCapabilityModel Capability(string key, int order)
        {
            return new AccessCapabilityModel
            {
                Key = key,
                Module = key.Split('.')[0],
                ModuleLabel = key.Split('.')[0],
                ModuleOrder = 1,
                Label = key,
                Description = string.Empty,
                Order = order,
                IsBaseline = false
            };
        }

        private static async Task SeedCatalogAsync(IAccessCapabilityService capabilities, params string[] keys)
        {
            await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
            {
                SystemAudience = Audience,
                Capabilities = keys.Select((key, index) => Capability(key, index + 1)).ToList()
            });
        }

        [Test]
        public async Task Template_capabilities_are_validated_persisted_and_kept_when_the_client_omits_them()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();
                ISystemRoleTemplateService templates = sp.GetRequiredService<ISystemRoleTemplateService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext);
                await SeedCatalogAsync(capabilities, "financeiro.ver", "financeiro.editar", "comercial.ver");

                SystemRoleTemplateResponse created = await templates.CreateSystemRoleTemplate(new CreateSystemRoleTemplateRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "Financeiro",
                    Description = "Perfil financeiro padrao",
                    CapabilityKeys = ["financeiro.ver", "financeiro.editar"]
                });

                created.CapabilityKeys.Should().Equal("financeiro.editar", "financeiro.ver");

                Func<Task> invalid = () => templates.CreateSystemRoleTemplate(new CreateSystemRoleTemplateRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "Quebrado",
                    Description = "Chave inexistente",
                    CapabilityKeys = ["financeiro.inexistente"]
                });
                await invalid.Should().ThrowAsync<InvalidOperationException>().WithMessage("accessCapability.invalid");

                // Cliente antigo (sem o campo) nao apaga as capacidades; lista vazia limpa.
                SystemRoleTemplateResponse untouched = await templates.UpdateSystemRoleTemplate(created.Id, new UpdateSystemRoleTemplateRequest
                {
                    Name = "Financeiro",
                    Description = "Perfil financeiro padrao",
                    CapabilityKeys = null
                });
                untouched.CapabilityKeys.Should().HaveCount(2);

                SystemRoleTemplateResponse reduced = await templates.UpdateSystemRoleTemplate(created.Id, new UpdateSystemRoleTemplateRequest
                {
                    Name = "Financeiro",
                    Description = "Perfil financeiro padrao",
                    CapabilityKeys = ["financeiro.ver"]
                });
                reduced.CapabilityKeys.Should().Equal("financeiro.ver");

                IReadOnlyCollection<SystemRoleTemplateResponse> bySystem = await templates.GetBySystemApplicationId(application.Id);
                bySystem.Single(item => item.Id == created.Id).CapabilityKeys.Should().Equal("financeiro.ver");

                SystemRoleTemplateResponse cleared = await templates.UpdateSystemRoleTemplate(created.Id, new UpdateSystemRoleTemplateRequest
                {
                    Name = "Financeiro",
                    Description = "Perfil financeiro padrao",
                    CapabilityKeys = []
                });
                cleared.CapabilityKeys.Should().BeEmpty();
            });
        }

        [Test]
        public async Task Provisioning_a_contract_copies_the_template_capabilities_into_the_new_roles()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();
                ISystemRoleTemplateService templates = sp.GetRequiredService<ISystemRoleTemplateService>();
                IContractService contracts = sp.GetRequiredService<IContractService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext);
                Company company = await SeedCompanyAsync(dbContext, "44555666000177");
                await SeedCatalogAsync(capabilities, "financeiro.ver", "financeiro.editar", "comercial.ver");

                await templates.CreateSystemRoleTemplate(new CreateSystemRoleTemplateRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "Administrador",
                    Description = "Acesso total",
                    IsRoot = true
                });

                await templates.CreateSystemRoleTemplate(new CreateSystemRoleTemplateRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "Financeiro",
                    Description = "Perfil financeiro padrao",
                    IsDefault = true,
                    CapabilityKeys = ["financeiro.ver", "financeiro.editar"]
                });

                Contract contract = await contracts.CreateContractCore(new CreateContractRequest
                {
                    CompanyId = company.Id,
                    SystemApplicationId = application.Id,
                    StartDate = DateTimeOffset.UtcNow
                });

                List<Role> roles = await dbContext.Set<Role>().AsNoTracking()
                    .Where(role => role.ContractId == contract.Id)
                    .OrderBy(role => role.Name)
                    .ToListAsync();

                roles.Select(role => role.Name).Should().Equal("Administrador", "Financeiro");
                roles.Single(role => role.Name == "Administrador").IsRoot.Should().BeTrue();
                roles.Single(role => role.Name == "Financeiro").IsDefault.Should().BeTrue();

                long financeiroRoleId = roles.Single(role => role.Name == "Financeiro").Id;
                long adminRoleId = roles.Single(role => role.Name == "Administrador").Id;

                List<string> financeiroCapabilities = await dbContext.Set<RoleCapability>().AsNoTracking()
                    .Where(link => link.RoleId == financeiroRoleId && link.IsActive)
                    .Select(link => link.CapabilityKey)
                    .OrderBy(key => key)
                    .ToListAsync();

                financeiroCapabilities.Should().Equal("financeiro.editar", "financeiro.ver");

                bool adminHasCapabilities = await dbContext.Set<RoleCapability>().AsNoTracking()
                    .AnyAsync(link => link.RoleId == adminRoleId);

                adminHasCapabilities.Should().BeFalse("perfil root nao precisa de capacidades");
            });
        }
    }
}
