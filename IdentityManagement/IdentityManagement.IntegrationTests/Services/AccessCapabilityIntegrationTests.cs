using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Archon.Core.Access;
using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Roles;
using IdentityManagement.Application.Responses.AccessCapabilities;
using IdentityManagement.Application.Responses.AccessResources;
using IdentityManagement.Application.Responses.Roles;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class AccessCapabilityIntegrationTests : IntegrationTestBase
    {
        private const string AudienceA = "capability-system-a";
        private const string AudienceB = "capability-system-b";

        private static async Task<SystemApplication> SeedSystemApplicationAsync(DbContext dbContext, string name, string audience)
        {
            SystemApplication application = new SystemApplication(name, $"Test application: {name}", audience, ApplicationType.External);
            await dbContext.Set<SystemApplication>().AddAsync(application);
            await dbContext.SaveChangesAsync();
            return application;
        }

        private static async Task<Contract> SeedContractAsync(DbContext dbContext, SystemApplication application, string document)
        {
            Company company = new Company("Agencia Capacidades LTDA", "Agencia Capacidades", document, "contato@capacidades.example", "11999990003");
            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            Contract contract = new Contract(company.Id, application.Id);
            await dbContext.Set<Contract>().AddAsync(contract);
            await dbContext.SaveChangesAsync();
            return contract;
        }

        private static AccessResourceModel Resource(string audience, string controller, string action, params string[] capabilities)
        {
            return new AccessResourceModel
            {
                SystemAudience = audience,
                Name = $"{controller}.{action}",
                Description = $"{controller} {action}",
                Area = controller,
                Controller = controller,
                Action = action,
                HttpMethod = "GET",
                Route = $"/api/{controller}/{action}",
                Capabilities = capabilities.ToList()
            };
        }

        private static AccessCapabilityModel Capability(string key, string label, int moduleOrder = 1, int order = 1, bool isBaseline = false)
        {
            return new AccessCapabilityModel
            {
                Key = key,
                Module = key.Split('.')[0],
                ModuleLabel = key.Split('.')[0].ToUpperInvariant(),
                ModuleOrder = moduleOrder,
                Label = label,
                Description = string.Empty,
                Order = order,
                IsBaseline = isBaseline
            };
        }

        [Test]
        public async Task Sync_touches_only_the_catalog_of_the_sending_system_and_counts_resources()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();
                IAccessResourceService resources = sp.GetRequiredService<IAccessResourceService>();

                SystemApplication systemA = await SeedSystemApplicationAsync(dbContext, "System A", AudienceA);
                SystemApplication systemB = await SeedSystemApplicationAsync(dbContext, "System B", AudienceB);
                Contract contractA = await SeedContractAsync(dbContext, systemA, "44555666000177");

                await resources.SyncResources([
                    Resource(AudienceA, "brands", "get", "comercial.ver", "producao.ver"),
                    Resource(AudienceA, "brands", "create", "comercial.editar"),
                    Resource(AudienceA, "campaigns", "get", "producao.ver")
                ]);

                await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = AudienceA,
                    Capabilities = [Capability("comercial.ver", "Ver comercial", 1, 1), Capability("comercial.editar", "Editar comercial", 1, 2), Capability("producao.ver", "Ver producao", 2, 1)]
                });

                await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = AudienceB,
                    Capabilities = [Capability("tickets.ver", "Ver tickets")]
                });

                // Segundo sync de A sem "comercial.editar" e com rotulo novo em "comercial.ver".
                AccessResourceSyncResponse response = await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = AudienceA,
                    Capabilities = [Capability("comercial.ver", "Ver funil e marcas", 1, 1), Capability("producao.ver", "Ver producao", 2, 1)]
                });

                response.UpdatedCount.Should().Be(1);
                response.DeactivatedCount.Should().Be(1);
                response.CreatedCount.Should().Be(0);

                IReadOnlyCollection<AccessCapabilityResponse> catalogA = await capabilities.GetActiveByContract(contractA.Id);
                catalogA.Select(item => item.Key).Should().Equal("comercial.ver", "producao.ver");
                catalogA.Single(item => item.Key == "comercial.ver").Label.Should().Be("Ver funil e marcas");
                catalogA.Single(item => item.Key == "comercial.ver").ResourceCount.Should().Be(1);
                catalogA.Single(item => item.Key == "producao.ver").ResourceCount.Should().Be(2);

                List<AccessCapability> catalogB = await dbContext.Set<AccessCapability>().AsNoTracking()
                    .Where(item => item.SystemApplicationId == systemB.Id)
                    .ToListAsync();
                catalogB.Should().ContainSingle();
                catalogB.Single().IsActive.Should().BeTrue();
            });
        }

        [Test]
        public async Task Sync_rejects_unknown_audience()
        {
            await InScopeAsync(async sp =>
            {
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();

                Func<Task> act = () => capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = "audience-that-does-not-exist",
                    Capabilities = [Capability("x.ver", "X")]
                });

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Role_capabilities_are_validated_persisted_and_kept_when_the_client_omits_them()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();
                IRoleService roles = sp.GetRequiredService<IRoleService>();

                SystemApplication systemA = await SeedSystemApplicationAsync(dbContext, "System A", AudienceA);
                Contract contract = await SeedContractAsync(dbContext, systemA, "44555666000177");

                await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = AudienceA,
                    Capabilities = [Capability("financeiro.ver", "Ver financeiro"), Capability("financeiro.aprovar", "Aprovar", 1, 2)]
                });

                RoleResponse created = await roles.CreateRole(new CreateRoleRequest
                {
                    Name = "Financeiro",
                    Description = "Perfil financeiro",
                    ContractId = contract.Id,
                    CapabilityKeys = ["financeiro.ver", "financeiro.aprovar"]
                });

                created.CapabilityKeys.Should().Equal("financeiro.aprovar", "financeiro.ver");

                Func<Task> invalid = () => roles.CreateRole(new CreateRoleRequest
                {
                    Name = "Quebrado",
                    Description = "Chave inexistente",
                    ContractId = contract.Id,
                    CapabilityKeys = ["financeiro.inexistente"]
                });
                await invalid.Should().ThrowAsync<InvalidOperationException>().WithMessage("accessCapability.invalid");

                // Cliente antigo (sem o campo) nao apaga as capacidades; lista vazia limpa.
                RoleResponse untouched = await roles.UpdateRole(created.Id, new UpdateRoleRequest { Name = "Financeiro", Description = "Perfil financeiro", CapabilityKeys = null });
                untouched.CapabilityKeys.Should().HaveCount(2);

                RoleResponse reduced = await roles.UpdateRole(created.Id, new UpdateRoleRequest { Name = "Financeiro", Description = "Perfil financeiro", CapabilityKeys = ["financeiro.ver"] });
                reduced.CapabilityKeys.Should().Equal("financeiro.ver");

                IReadOnlyCollection<RoleResponse> byContract = await roles.GetRolesByContract(contract.Id);
                byContract.Single(item => item.Id == created.Id).CapabilityKeys.Should().Equal("financeiro.ver");

                RoleResponse cleared = await roles.UpdateRole(created.Id, new UpdateRoleRequest { Name = "Financeiro", Description = "Perfil financeiro", CapabilityKeys = [] });
                cleared.CapabilityKeys.Should().BeEmpty();
            });
        }

        [Test]
        public async Task Access_token_expands_role_capabilities_and_baseline_into_permissions()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessCapabilityService capabilities = sp.GetRequiredService<IAccessCapabilityService>();
                IAccessResourceService resources = sp.GetRequiredService<IAccessResourceService>();
                IRoleService roles = sp.GetRequiredService<IRoleService>();
                IJwtService jwt = sp.GetRequiredService<IJwtService>();

                SystemApplication systemA = await SeedSystemApplicationAsync(dbContext, "System A", AudienceA);
                Contract contract = await SeedContractAsync(dbContext, systemA, "44555666000177");

                await resources.SyncResources([
                    Resource(AudienceA, "financialEntries", "get", "financeiro.ver"),
                    Resource(AudienceA, "financialEntries", "approve", "financeiro.aprovar"),
                    Resource(AudienceA, "campaigns", "get", "producao.ver", "financeiro.ver"),
                    Resource(AudienceA, "notifications", "get", "geral.basico"),
                    Resource(AudienceA, "brands", "create", "comercial.editar"),
                    Resource(AudienceA, "legacy", "get")
                ]);

                await capabilities.SyncCapabilities(new AccessCapabilitySyncRequest
                {
                    SystemAudience = AudienceA,
                    Capabilities =
                    [
                        Capability("financeiro.ver", "Ver financeiro"),
                        Capability("financeiro.aprovar", "Aprovar", 1, 2),
                        Capability("producao.ver", "Ver producao", 2),
                        Capability("comercial.editar", "Editar comercial", 3),
                        Capability("geral.basico", "Basico", 9, 1, isBaseline: true)
                    ]
                });

                long legacyId = await dbContext.Set<AccessResource>().AsNoTracking()
                    .Where(item => item.SystemApplicationId == systemA.Id && item.Name == "legacy.get")
                    .Select(item => item.Id)
                    .SingleAsync();

                RoleResponse role = await roles.CreateRole(new CreateRoleRequest
                {
                    Name = "Financeiro",
                    Description = "Perfil financeiro",
                    ContractId = contract.Id,
                    AccessResourceIds = [legacyId],
                    CapabilityKeys = ["financeiro.ver"]
                });

                User user = new User("fin.user", "fin@capacidades.example", "hash", "Usuario Financeiro");
                await dbContext.Set<User>().AddAsync(user);
                await dbContext.SaveChangesAsync();
                await dbContext.Set<UserRole>().AddAsync(new UserRole(user.Id, role.Id));
                await dbContext.Set<SigningKey>().AddAsync(CreateSigningKey());
                await dbContext.SaveChangesAsync();

                Contract loadedContract = await dbContext.Set<Contract>().AsNoTracking()
                    .Include(item => item.Company)
                    .Include(item => item.SystemApplication)
                    .SingleAsync(item => item.Id == contract.Id);

                string token = await jwt.GenerateAccessToken(user, loadedContract, 600, null!, null!, false);
                JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

                List<string> permissions = parsed.Claims.Where(claim => claim.Type == "permission").Select(claim => claim.Value).ToList();
                permissions.Should().BeEquivalentTo("legacy.get", "financialEntries.get", "campaigns.get", "notifications.get");

                List<string> capabilityClaims = parsed.Claims.Where(claim => claim.Type == "capability").Select(claim => claim.Value).ToList();
                capabilityClaims.Should().BeEquivalentTo("financeiro.ver", "geral.basico");
                parsed.Claims.Should().NotContain(claim => claim.Type == "root");
            });
        }

        private static SigningKey CreateSigningKey()
        {
            using RSA rsa = RSA.Create(2048);
            return new SigningKey(
                keyId: $"test-{Guid.NewGuid():N}",
                algorithm: SecurityAlgorithms.RsaSha256,
                publicKeyPem: rsa.ExportSubjectPublicKeyInfoPem(),
                privateKeyEncrypted: rsa.ExportRSAPrivateKeyPem(),
                notBefore: DateTimeOffset.UtcNow.AddMinutes(-1));
        }
    }
}
