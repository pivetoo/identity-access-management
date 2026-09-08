using Archon.Infrastructure.Services;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Responses.SystemRoleTemplates;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class SystemRoleTemplateService : CrudService<SystemRoleTemplate>, ISystemRoleTemplateService
    {
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public SystemRoleTemplateService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer) : base(dbContext)
        {
            this.Localizer = Localizer;
        }

        public async Task<SystemRoleTemplateResponse> CreateSystemRoleTemplate(CreateSystemRoleTemplateRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureSystemApplication(request.SystemApplicationId, cancellationToken);
            await EnsureUniqueTemplateName(request.SystemApplicationId, request.Name, null, cancellationToken);

            if (request.IsDefault)
            {
                await ClearDefaultTemplate(request.SystemApplicationId, cancellationToken);
            }

            SystemRoleTemplate template = new(request.SystemApplicationId, request.Name, request.Description, request.IsRoot, request.IsDefault);
            bool success = await Insert(cancellationToken, template);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncAccessResources(template.Id, request.SystemApplicationId, request.AccessResourceIds, cancellationToken);
            await SyncCapabilities(template.Id, request.SystemApplicationId, request.CapabilityKeys, cancellationToken);

            return await GetRequiredResponse(template.Id, cancellationToken);
        }

        public async Task<SystemRoleTemplateResponse> UpdateSystemRoleTemplate(long id, UpdateSystemRoleTemplateRequest request, CancellationToken cancellationToken = default)
        {
            SystemRoleTemplate? template = await (
                from item in DbContext.Set<SystemRoleTemplate>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (template is null)
            {
                throw new InvalidOperationException("systemRoleTemplate.notFound");
            }

            await EnsureUniqueTemplateName(template.SystemApplicationId, request.Name, id, cancellationToken);

            if (request.IsDefault)
            {
                await ClearDefaultTemplate(template.SystemApplicationId, cancellationToken, id);
            }

            template.Update(request.Name, request.Description, request.IsRoot, request.IsDefault, request.IsActive);

            SystemRoleTemplate? result = await Update(template, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncAccessResources(result.Id, result.SystemApplicationId, request.AccessResourceIds, cancellationToken);
            await SyncCapabilities(result.Id, result.SystemApplicationId, request.CapabilityKeys, cancellationToken);

            return await GetRequiredResponse(result.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<SystemRoleTemplateResponse>> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken = default)
        {
            List<SystemRoleTemplate> templates = await (
                from template in DbContext.Set<SystemRoleTemplate>().AsNoTracking()
                where template.SystemApplicationId == systemApplicationId
                orderby template.Name
                select template)
                .ToListAsync(cancellationToken);

            return await ToResponses(templates, cancellationToken);
        }

        public async Task<SystemRoleTemplateResponse?> GetById(long id, CancellationToken cancellationToken = default)
        {
            SystemRoleTemplate? template = await DbContext.Set<SystemRoleTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (template is null)
            {
                return null;
            }

            return (await ToResponses([template], cancellationToken)).Single();
        }

        private async Task<List<SystemRoleTemplateResponse>> ToResponses(IReadOnlyCollection<SystemRoleTemplate> templates, CancellationToken cancellationToken)
        {
            if (templates.Count == 0)
            {
                return [];
            }

            List<long> templateIds = templates.Select(template => template.Id).ToList();

            List<SystemRoleTemplateAccessResource> resourceLinks = await DbContext.Set<SystemRoleTemplateAccessResource>()
                .AsNoTracking()
                .Where(link => templateIds.Contains(link.SystemRoleTemplateId) && link.IsActive)
                .ToListAsync(cancellationToken);

            List<SystemRoleTemplateCapability> capabilityLinks = await DbContext.Set<SystemRoleTemplateCapability>()
                .AsNoTracking()
                .Where(link => templateIds.Contains(link.SystemRoleTemplateId) && link.IsActive)
                .ToListAsync(cancellationToken);

            return templates
                .Select(template => new SystemRoleTemplateResponse
                {
                    Id = template.Id,
                    SystemApplicationId = template.SystemApplicationId,
                    Name = template.Name,
                    Description = template.Description,
                    IsRoot = template.IsRoot,
                    IsDefault = template.IsDefault,
                    IsActive = template.IsActive,
                    AccessResourceIds = resourceLinks
                        .Where(link => link.SystemRoleTemplateId == template.Id)
                        .Select(link => link.AccessResourceId)
                        .OrderBy(item => item)
                        .ToList(),
                    CapabilityKeys = capabilityLinks
                        .Where(link => link.SystemRoleTemplateId == template.Id)
                        .Select(link => link.CapabilityKey)
                        .OrderBy(item => item, StringComparer.Ordinal)
                        .ToList(),
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                })
                .ToList();
        }

        private async Task<SystemRoleTemplateResponse> GetRequiredResponse(long id, CancellationToken cancellationToken)
        {
            return await GetById(id, cancellationToken)
                ?? throw new InvalidOperationException("systemRoleTemplate.load.failed");
        }

        private async Task EnsureSystemApplication(long systemApplicationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<SystemApplication>()
                .AnyAsync(item => item.Id == systemApplicationId && item.IsActive, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("systemApplication.notFoundOrInactive");
            }
        }

        private async Task EnsureUniqueTemplateName(long systemApplicationId, string name, long? currentTemplateId, CancellationToken cancellationToken)
        {
            bool exists = await (
                from template in DbContext.Set<SystemRoleTemplate>().AsNoTracking()
                where template.SystemApplicationId == systemApplicationId &&
                      template.Name == name &&
                      (!currentTemplateId.HasValue || template.Id != currentTemplateId.Value)
                select template.Id)
                .AnyAsync(cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("systemRoleTemplate.name.alreadyExists");
            }
        }

        private async Task ClearDefaultTemplate(long systemApplicationId, CancellationToken cancellationToken, long? ignoredTemplateId = null)
        {
            List<SystemRoleTemplate> templates = await (
                from template in DbContext.Set<SystemRoleTemplate>().AsTracking()
                where template.SystemApplicationId == systemApplicationId &&
                      template.IsDefault &&
                      (!ignoredTemplateId.HasValue || template.Id != ignoredTemplateId.Value)
                select template)
                .ToListAsync(cancellationToken);

            foreach (SystemRoleTemplate template in templates)
            {
                template.SetDefault(false);
            }

            if (templates.Count > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task SyncAccessResources(long templateId, long systemApplicationId, IReadOnlyCollection<long> accessResourceIds, CancellationToken cancellationToken)
        {
            List<long> normalizedIds = accessResourceIds
                .Where(item => item > 0)
                .Distinct()
                .ToList();

            List<long> validResourceIds = await (
                from accessResource in DbContext.Set<AccessResource>().AsNoTracking()
                where accessResource.IsActive &&
                      accessResource.SystemApplicationId == systemApplicationId &&
                      normalizedIds.Contains(accessResource.Id)
                select accessResource.Id)
                .ToListAsync(cancellationToken);

            if (validResourceIds.Count != normalizedIds.Count)
            {
                throw new InvalidOperationException("systemRoleTemplate.accessResource.invalid");
            }

            List<SystemRoleTemplateAccessResource> existingLinks = await (
                from link in DbContext.Set<SystemRoleTemplateAccessResource>().AsTracking()
                where link.SystemRoleTemplateId == templateId
                select link)
                .ToListAsync(cancellationToken);

            foreach (SystemRoleTemplateAccessResource existingLink in existingLinks)
            {
                if (normalizedIds.Contains(existingLink.AccessResourceId))
                {
                    existingLink.Activate();
                    continue;
                }

                existingLink.Deactivate();
            }

            HashSet<long> existingResourceIds = existingLinks
                .Select(item => item.AccessResourceId)
                .ToHashSet();

            List<SystemRoleTemplateAccessResource> newLinks = normalizedIds
                .Where(item => !existingResourceIds.Contains(item))
                .Select(item => new SystemRoleTemplateAccessResource(templateId, item))
                .ToList();

            if (newLinks.Count > 0)
            {
                await DbContext.Set<SystemRoleTemplateAccessResource>().AddRangeAsync(newLinks, cancellationToken);
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }

        // Nulo = o cliente nao trata capacidades (mantem o que existe); lista = estado final desejado.
        private async Task SyncCapabilities(long templateId, long systemApplicationId, IReadOnlyCollection<string>? capabilityKeys, CancellationToken cancellationToken)
        {
            if (capabilityKeys is null)
            {
                return;
            }

            List<string> normalizedKeys = capabilityKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedKeys.Count > 0)
            {
                List<string> validKeys = await (
                    from capability in DbContext.Set<AccessCapability>().AsNoTracking()
                    where capability.IsActive &&
                          capability.SystemApplicationId == systemApplicationId &&
                          normalizedKeys.Contains(capability.CapabilityKey)
                    select capability.CapabilityKey)
                    .ToListAsync(cancellationToken);

                if (validKeys.Count != normalizedKeys.Count)
                {
                    throw new InvalidOperationException("accessCapability.invalid");
                }
            }

            List<SystemRoleTemplateCapability> existingLinks = await (
                from link in DbContext.Set<SystemRoleTemplateCapability>().AsTracking()
                where link.SystemRoleTemplateId == templateId
                select link)
                .ToListAsync(cancellationToken);

            HashSet<string> wanted = normalizedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (SystemRoleTemplateCapability existingLink in existingLinks)
            {
                if (wanted.Contains(existingLink.CapabilityKey))
                {
                    existingLink.Activate();
                    continue;
                }

                existingLink.Deactivate();
            }

            HashSet<string> existingKeys = existingLinks
                .Select(item => item.CapabilityKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<SystemRoleTemplateCapability> newLinks = normalizedKeys
                .Where(key => !existingKeys.Contains(key))
                .Select(key => new SystemRoleTemplateCapability(templateId, key))
                .ToList();

            if (newLinks.Count > 0)
            {
                await DbContext.Set<SystemRoleTemplateCapability>().AddRangeAsync(newLinks, cancellationToken);
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
