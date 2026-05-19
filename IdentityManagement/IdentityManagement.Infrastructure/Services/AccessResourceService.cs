using Archon.Core.Access;
using Archon.Core.Exceptions;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Responses.AccessResources;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AccessResourceService : IAccessResourceService
    {
        private readonly DbContext dbContext;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AccessResourceService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.dbContext = dbContext;
            this.Localizer = Localizer;
        }

        public async Task<IReadOnlyCollection<AccessResourceResponse>> GetActiveResources(CancellationToken cancellationToken = default)
        {
            List<AccessResourceResponse> resources = await (
                from accessResource in dbContext.Set<AccessResource>().AsNoTracking()
                where accessResource.IsActive
                orderby accessResource.SystemApplicationId, accessResource.Controller, accessResource.Action, accessResource.HttpMethod
                select new AccessResourceResponse
                {
                    Id = accessResource.Id,
                    SystemApplicationId = accessResource.SystemApplicationId,
                    Name = accessResource.Name,
                    Description = accessResource.Description,
                    Area = accessResource.Area,
                    Controller = accessResource.Controller,
                    Action = accessResource.Action,
                    HttpMethod = accessResource.HttpMethod,
                    Route = accessResource.Route
                })
                .ToListAsync(cancellationToken);

            return resources;
        }

        public async Task<IReadOnlyCollection<AccessResourceResponse>> GetActiveResourcesByContract(long contractId, CancellationToken cancellationToken = default)
        {
            long systemApplicationId = await (
                from contract in dbContext.Set<Contract>().AsNoTracking()
                where contract.Id == contractId
                select contract.SystemApplicationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemApplicationId <= 0)
            {
                return Array.Empty<AccessResourceResponse>();
            }

            return await (
                from accessResource in dbContext.Set<AccessResource>().AsNoTracking()
                where accessResource.IsActive && accessResource.SystemApplicationId == systemApplicationId
                orderby accessResource.Controller, accessResource.Action, accessResource.HttpMethod
                select new AccessResourceResponse
                {
                    Id = accessResource.Id,
                    SystemApplicationId = accessResource.SystemApplicationId,
                    Name = accessResource.Name,
                    Description = accessResource.Description,
                    Area = accessResource.Area,
                    Controller = accessResource.Controller,
                    Action = accessResource.Action,
                    HttpMethod = accessResource.HttpMethod,
                    Route = accessResource.Route
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AccessResourceSyncResponse> SyncResources(IReadOnlyCollection<AccessResourceModel> resources, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(resources);

            List<AccessResourceModel> normalizedResources = resources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.SystemAudience) && !string.IsNullOrWhiteSpace(resource.Name))
                .GroupBy(resource => new
                {
                    Audience = resource.SystemAudience.Trim(),
                    Name = resource.Name.Trim()
                })
                .Select(group => group.First())
                .ToList();

            if (normalizedResources.Count == 0)
            {
                return new AccessResourceSyncResponse();
            }

            Dictionary<string, long> systemApplicationIdsByAudience = await (
                from systemApplication in dbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.IsActive
                select new
                {
                    systemApplication.Audience,
                    systemApplication.Id
                })
                .ToDictionaryAsync(item => item.Audience, item => item.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

            List<AccessResource> existingResources = await (
                from accessResource in dbContext.Set<AccessResource>().AsTracking()
                    .Include(item => item.SystemApplication)
                select accessResource)
                .ToListAsync(cancellationToken);

            Dictionary<string, AccessResource> existingByName = existingResources
                .ToDictionary(resource => CreateResourceKey(resource.SystemApplication.Audience, resource.Name), StringComparer.OrdinalIgnoreCase);

            int createdCount = 0;
            int updatedCount = 0;

            foreach (AccessResourceModel resource in normalizedResources)
            {
                if (!systemApplicationIdsByAudience.TryGetValue(resource.SystemAudience.Trim(), out long systemApplicationId))
                {
                    throw new BusinessRuleException("systemApplication.audience.notFound", resource.SystemAudience);
                }

                string resourceKey = CreateResourceKey(resource.SystemAudience, resource.Name);

                if (existingByName.TryGetValue(resourceKey, out AccessResource? existingResource))
                {
                    bool changed =
                        existingResource.SystemApplicationId != systemApplicationId ||
                        !string.Equals(existingResource.Description, resource.Description, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.Area, resource.Area, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.Controller, resource.Controller, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.Action, resource.Action, StringComparison.Ordinal) ||
                        !string.Equals(existingResource.HttpMethod, resource.HttpMethod, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(existingResource.Route, resource.Route, StringComparison.Ordinal) ||
                        !existingResource.IsActive;

                    if (!changed)
                    {
                        continue;
                    }

                    existingResource.Update(systemApplicationId, resource.Description, resource.Area, resource.Controller, resource.Action, resource.HttpMethod, resource.Route);
                    existingResource.Activate();
                    updatedCount++;
                    continue;
                }

                AccessResource accessResource = new AccessResource(systemApplicationId, resource.Name, resource.Description, resource.Area, resource.Controller, resource.Action, resource.HttpMethod, resource.Route);
                await dbContext.Set<AccessResource>().AddAsync(accessResource, cancellationToken);
                createdCount++;
            }

            HashSet<string> incomingNames = normalizedResources
                .Select(resource => CreateResourceKey(resource.SystemAudience, resource.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int deactivatedCount = 0;

            foreach (AccessResource existingResource in existingResources)
            {
                string existingKey = CreateResourceKey(existingResource.SystemApplication.Audience, existingResource.Name);

                if (!existingResource.IsActive || incomingNames.Contains(existingKey))
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

        private static string CreateResourceKey(string systemAudience, string resourceName)
        {
            return $"{systemAudience.Trim()}::{resourceName.Trim()}";
        }
    }
}
