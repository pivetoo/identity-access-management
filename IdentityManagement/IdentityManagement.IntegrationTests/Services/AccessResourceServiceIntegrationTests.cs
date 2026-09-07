using Archon.Core.Access;
using IdentityManagement.Application.Responses.AccessResources;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class AccessResourceServiceIntegrationTests : IntegrationTestBase
    {
        private const string AudienceA = "access-sync-system-a";
        private const string AudienceB = "access-sync-system-b";

        private static async Task<SystemApplication> SeedSystemApplicationAsync(DbContext dbContext, string name, string audience)
        {
            SystemApplication application = new SystemApplication(name, $"Test application: {name}", audience);

            await dbContext.Set<SystemApplication>().AddAsync(application);
            await dbContext.SaveChangesAsync();

            return application;
        }

        private static AccessResourceModel Resource(string audience, string controller, string action)
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
                Route = $"/api/{controller}/{action}"
            };
        }

        [Test]
        public async Task Sync_of_one_system_does_not_deactivate_resources_of_other_systems()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessResourceService service = sp.GetRequiredService<IAccessResourceService>();

                SystemApplication systemA = await SeedSystemApplicationAsync(dbContext, "System A", AudienceA);
                SystemApplication systemB = await SeedSystemApplicationAsync(dbContext, "System B", AudienceB);

                await service.SyncResources([Resource(AudienceA, "brands", "get"), Resource(AudienceA, "brands", "create")]);
                await service.SyncResources([Resource(AudienceB, "tickets", "get")]);

                // Segundo sync do sistema A sem "brands.create": so esse recurso pode ser desativado.
                AccessResourceSyncResponse response = await service.SyncResources([Resource(AudienceA, "brands", "get")]);

                response.DeactivatedCount.Should().Be(1);
                response.CreatedCount.Should().Be(0);

                List<AccessResource> resourcesA = await dbContext.Set<AccessResource>().AsNoTracking()
                    .Where(resource => resource.SystemApplicationId == systemA.Id)
                    .ToListAsync();
                resourcesA.Single(resource => resource.Name == "brands.get").IsActive.Should().BeTrue();
                resourcesA.Single(resource => resource.Name == "brands.create").IsActive.Should().BeFalse();

                List<AccessResource> resourcesB = await dbContext.Set<AccessResource>().AsNoTracking()
                    .Where(resource => resource.SystemApplicationId == systemB.Id)
                    .ToListAsync();
                resourcesB.Should().ContainSingle();
                resourcesB.Single().IsActive.Should().BeTrue();
            });
        }

        [Test]
        public async Task Sync_reactivates_a_resource_that_comes_back_in_the_payload()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IAccessResourceService service = sp.GetRequiredService<IAccessResourceService>();

                SystemApplication systemA = await SeedSystemApplicationAsync(dbContext, "System A", AudienceA);

                await service.SyncResources([Resource(AudienceA, "brands", "get"), Resource(AudienceA, "brands", "create")]);
                await service.SyncResources([Resource(AudienceA, "brands", "get")]);

                AccessResourceSyncResponse response = await service.SyncResources([Resource(AudienceA, "brands", "get"), Resource(AudienceA, "brands", "create")]);

                response.UpdatedCount.Should().Be(1);
                response.DeactivatedCount.Should().Be(0);

                List<AccessResource> resources = await dbContext.Set<AccessResource>().AsNoTracking()
                    .Where(resource => resource.SystemApplicationId == systemA.Id)
                    .ToListAsync();
                resources.Should().HaveCount(2);
                resources.Should().OnlyContain(resource => resource.IsActive);
            });
        }

        [Test]
        public async Task Sync_rejects_payload_with_unknown_audience()
        {
            await InScopeAsync(async sp =>
            {
                IAccessResourceService service = sp.GetRequiredService<IAccessResourceService>();

                Func<Task> act = () => service.SyncResources([Resource("audience-that-does-not-exist", "brands", "get")]);

                await act.Should().ThrowAsync<Archon.Core.Exceptions.BusinessRuleException>();
            });
        }
    }
}
