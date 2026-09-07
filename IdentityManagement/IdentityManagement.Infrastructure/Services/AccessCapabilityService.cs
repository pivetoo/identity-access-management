using Archon.Core.Access;
using Archon.Core.Exceptions;
using IdentityManagement.Application.Responses.AccessCapabilities;
using IdentityManagement.Application.Responses.AccessResources;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AccessCapabilityService : IAccessCapabilityService
    {
        private readonly DbContext dbContext;

        public AccessCapabilityService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<AccessResourceSyncResponse> SyncCapabilities(AccessCapabilitySyncRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            string audience = request.SystemAudience?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new BusinessRuleException("systemApplication.audience.notFound", audience);
            }

            long systemApplicationId = await (
                from systemApplication in dbContext.Set<SystemApplication>().AsNoTracking()
                where systemApplication.IsActive && systemApplication.Audience == audience
                select systemApplication.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemApplicationId <= 0)
            {
                throw new BusinessRuleException("systemApplication.audience.notFound", audience);
            }

            List<AccessCapabilityModel> normalized = (request.Capabilities ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .GroupBy(item => item.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            // So o catalogo do sistema que enviou e tocado: os outros sistemas do mesmo IdM nao entram.
            List<AccessCapability> existing = await (
                from capability in dbContext.Set<AccessCapability>().AsTracking()
                where capability.SystemApplicationId == systemApplicationId
                select capability)
                .ToListAsync(cancellationToken);

            Dictionary<string, AccessCapability> existingByKey = existing.ToDictionary(item => item.CapabilityKey, StringComparer.OrdinalIgnoreCase);

            int createdCount = 0;
            int updatedCount = 0;

            foreach (AccessCapabilityModel model in normalized)
            {
                string key = model.Key.Trim();
                string module = string.IsNullOrWhiteSpace(model.Module) ? key : model.Module;

                if (existingByKey.TryGetValue(key, out AccessCapability? current))
                {
                    bool unchanged = current.IsActive && current.Matches(module, model.ModuleLabel, model.ModuleOrder, model.Label, model.Description, model.Order, model.IsBaseline);
                    if (unchanged)
                    {
                        continue;
                    }

                    current.Update(module, model.ModuleLabel, model.ModuleOrder, model.Label, model.Description, model.Order, model.IsBaseline);
                    current.Activate();
                    updatedCount++;
                    continue;
                }

                AccessCapability created = new AccessCapability(systemApplicationId, key, module, model.ModuleLabel, model.ModuleOrder, model.Label, model.Description, model.Order, model.IsBaseline);
                await dbContext.Set<AccessCapability>().AddAsync(created, cancellationToken);
                createdCount++;
            }

            HashSet<string> incomingKeys = normalized.Select(item => item.Key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int deactivatedCount = 0;

            foreach (AccessCapability current in existing)
            {
                if (!current.IsActive || incomingKeys.Contains(current.CapabilityKey))
                {
                    continue;
                }

                current.Deactivate();
                deactivatedCount++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new AccessResourceSyncResponse
            {
                CreatedCount = createdCount,
                UpdatedCount = updatedCount,
                DeactivatedCount = deactivatedCount,
                TotalCount = normalized.Count
            };
        }

        public async Task<IReadOnlyCollection<AccessCapabilityResponse>> GetActiveByContract(long contractId, CancellationToken cancellationToken = default)
        {
            long systemApplicationId = await (
                from contract in dbContext.Set<Contract>().AsNoTracking()
                where contract.Id == contractId
                select contract.SystemApplicationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemApplicationId <= 0)
            {
                return Array.Empty<AccessCapabilityResponse>();
            }

            List<AccessCapability> capabilities = await (
                from capability in dbContext.Set<AccessCapability>().AsNoTracking()
                where capability.SystemApplicationId == systemApplicationId && capability.IsActive
                orderby capability.ModuleOrder, capability.Module, capability.SortOrder, capability.CapabilityKey
                select capability)
                .ToListAsync(cancellationToken);

            if (capabilities.Count == 0)
            {
                return Array.Empty<AccessCapabilityResponse>();
            }

            Dictionary<string, int> resourceCounts = await CountResourcesByCapability(systemApplicationId, cancellationToken);

            return capabilities
                .Select(capability => new AccessCapabilityResponse
                {
                    Key = capability.CapabilityKey,
                    Module = capability.Module,
                    ModuleLabel = capability.ModuleLabel,
                    ModuleOrder = capability.ModuleOrder,
                    Label = capability.Label,
                    Description = capability.Description,
                    Order = capability.SortOrder,
                    IsBaseline = capability.IsBaseline,
                    ResourceCount = resourceCounts.GetValueOrDefault(capability.CapabilityKey)
                })
                .ToList();
        }

        public async Task<IReadOnlyCollection<string>> ExpandResourceNames(long systemApplicationId, IReadOnlyCollection<string> capabilityKeys, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(capabilityKeys);

            HashSet<string> keys = capabilityKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (keys.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<(string Name, string Capabilities)> resources = await LoadResourcesWithCapabilities(systemApplicationId, cancellationToken);

            return resources
                .Where(resource => AccessResource.SplitCapabilities(resource.Capabilities).Any(keys.Contains))
                .Select(resource => resource.Name)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private async Task<Dictionary<string, int>> CountResourcesByCapability(long systemApplicationId, CancellationToken cancellationToken)
        {
            List<(string Name, string Capabilities)> resources = await LoadResourcesWithCapabilities(systemApplicationId, cancellationToken);
            Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

            foreach ((string _, string capabilities) in resources)
            {
                foreach (string key in AccessResource.SplitCapabilities(capabilities))
                {
                    counts[key] = counts.GetValueOrDefault(key) + 1;
                }
            }

            return counts;
        }

        // As chaves ficam concatenadas na coluna; a lista e pequena (centenas de endpoints), entao
        // filtrar em memoria e mais simples e portavel do que LIKE por chave.
        private async Task<List<(string Name, string Capabilities)>> LoadResourcesWithCapabilities(long systemApplicationId, CancellationToken cancellationToken)
        {
            var rows = await (
                from resource in dbContext.Set<AccessResource>().AsNoTracking()
                where resource.SystemApplicationId == systemApplicationId && resource.IsActive && resource.Capabilities != string.Empty
                select new { resource.Name, resource.Capabilities })
                .ToListAsync(cancellationToken);

            return rows.Select(row => (row.Name, row.Capabilities)).ToList();
        }
    }
}
