using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Responses.SystemRoleTemplates;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class SystemRoleTemplateService : CrudService<SystemRoleTemplate>, ISystemRoleTemplateService
    {
        public SystemRoleTemplateService(DbContext dbContext) : base(dbContext)
        {
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
                throw new InvalidOperationException("System role template not found.");
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

            return await GetRequiredResponse(result.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<SystemRoleTemplateResponse>> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken = default)
        {
            List<SystemRoleTemplateResponse> templates = await (
                from template in DbContext.Set<SystemRoleTemplate>().AsNoTracking()
                where template.SystemApplicationId == systemApplicationId
                orderby template.Name
                select new SystemRoleTemplateResponse
                {
                    Id = template.Id,
                    SystemApplicationId = template.SystemApplicationId,
                    Name = template.Name,
                    Description = template.Description,
                    IsRoot = template.IsRoot,
                    IsDefault = template.IsDefault,
                    IsActive = template.IsActive,
                    AccessResourceIds = DbContext.Set<SystemRoleTemplateAccessResource>()
                        .Where(link => link.SystemRoleTemplateId == template.Id && link.IsActive)
                        .OrderBy(link => link.AccessResourceId)
                        .Select(link => link.AccessResourceId)
                        .ToList(),
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return templates;
        }

        public async Task<SystemRoleTemplateResponse?> GetById(long id, CancellationToken cancellationToken = default)
        {
            return await (
                from template in DbContext.Set<SystemRoleTemplate>().AsNoTracking()
                where template.Id == id
                select new SystemRoleTemplateResponse
                {
                    Id = template.Id,
                    SystemApplicationId = template.SystemApplicationId,
                    Name = template.Name,
                    Description = template.Description,
                    IsRoot = template.IsRoot,
                    IsDefault = template.IsDefault,
                    IsActive = template.IsActive,
                    AccessResourceIds = DbContext.Set<SystemRoleTemplateAccessResource>()
                        .Where(link => link.SystemRoleTemplateId == template.Id && link.IsActive)
                        .OrderBy(link => link.AccessResourceId)
                        .Select(link => link.AccessResourceId)
                        .ToList(),
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<SystemRoleTemplateResponse> GetRequiredResponse(long id, CancellationToken cancellationToken)
        {
            return await GetById(id, cancellationToken)
                ?? throw new InvalidOperationException("System role template could not be loaded.");
        }

        private async Task EnsureSystemApplication(long systemApplicationId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<SystemApplication>()
                .AnyAsync(item => item.Id == systemApplicationId && item.IsActive, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("System application not found or inactive.");
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
                throw new InvalidOperationException("System role template name already exists for this application.");
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
                throw new InvalidOperationException("One or more access resources are invalid for this system application.");
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
    }
}
