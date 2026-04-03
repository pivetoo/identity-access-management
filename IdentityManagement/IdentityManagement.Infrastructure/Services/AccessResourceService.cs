using Archon.Core.Access;
using IdentityManagement.Application.Responses.AccessResources;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AccessResourceService : IAccessResourceService
    {
        private readonly DbContext dbContext;

        public AccessResourceService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<AccessResourceSyncResponse> SyncResources(IReadOnlyCollection<AccessResourceModel> resources, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resources);

            List<AccessResourceModel> normalizedResources = resources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
                .GroupBy(resource => resource.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            List<AccessResource> existingResources = await (
                from accessResource in dbContext.Set<AccessResource>().AsTracking()
                select accessResource)
                .ToListAsync(cancellationToken);

            Dictionary<string, AccessResource> existingByName = existingResources
                .ToDictionary(resource => resource.Name, StringComparer.OrdinalIgnoreCase);

            int createdCount = 0;
            int updatedCount = 0;

            foreach (AccessResourceModel resource in normalizedResources)
            {
                if (existingByName.TryGetValue(resource.Name, out AccessResource? existingResource))
                {
                    bool changed =
                        !string.Equals(existingResource.Controller, resource.Controller, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.Action, resource.Action, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.HttpMethod, resource.HttpMethod, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(existingResource.Route, resource.Route, StringComparison.Ordinal) ||
                        !existingResource.IsActive;

                    if (!changed)
                    {
                        continue;
                    }

                    existingResource.Update(resource.Controller, resource.Action, resource.HttpMethod, resource.Route);
                    existingResource.Activate();
                    updatedCount++;
                    continue;
                }

                AccessResource accessResource = new AccessResource(resource.Name, resource.Controller, resource.Action, resource.HttpMethod, resource.Route);
                await dbContext.Set<AccessResource>().AddAsync(accessResource, cancellationToken);
                createdCount++;
            }

            HashSet<string> incomingNames = normalizedResources
                .Select(resource => resource.Name.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int deactivatedCount = 0;

            foreach (AccessResource existingResource in existingResources)
            {
                if (!existingResource.IsActive || incomingNames.Contains(existingResource.Name))
                {
                    continue;
                }

                existingResource.Deactivate();
                deactivatedCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new AccessResourceSyncResponse
            {
                CreatedCount = createdCount,
                UpdatedCount = updatedCount,
                DeactivatedCount = deactivatedCount,
                TotalCount = normalizedResources.Count
            };
        }
    }
}
