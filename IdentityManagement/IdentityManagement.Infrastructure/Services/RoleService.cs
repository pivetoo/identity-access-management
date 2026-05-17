using Archon.Infrastructure.Services;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Roles;
using IdentityManagement.Application.Responses.Roles;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class RoleService : CrudService<Role>, IRoleService
    {
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public RoleService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer) : base(dbContext)
        {
            this.Localizer = Localizer;
        }

        public async Task<RoleResponse> CreateRole(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureContract(request.ContractId, cancellationToken);

            if (request.IsDefault)
            {
                await ClearDefaultRole(request.ContractId, cancellationToken);
            }

            Role role = new Role(request.Name, request.Description, request.ContractId, request.IsRoot, request.IsDefault);
            bool success = await Insert(cancellationToken, role);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncAccessResources(role.Id, request.AccessResourceIds, cancellationToken);

            return await GetRoleResponse(role.Id, cancellationToken);
        }

        public async Task<RoleResponse> UpdateRole(long id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            Role? role = await (
                from item in DbContext.Set<Role>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException(Localizer["role.notFound"]);
            }

            if (request.IsDefault)
            {
                await ClearDefaultRole(role.ContractId, cancellationToken, role.Id);
            }

            role.Update(request.Name, request.Description, request.IsRoot, request.IsDefault);

            Role? result = await Update(role, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await SyncAccessResources(result.Id, request.AccessResourceIds, cancellationToken);

            return await GetRoleResponse(result.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<RoleResponse>> GetActiveRoles(CancellationToken cancellationToken = default)
        {
            List<RoleResponse> roles = await (
                from role in DbContext.Set<Role>().AsNoTracking()
                join contract in DbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                where contract.IsActive
                orderby role.Name
                select new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    ContractId = role.ContractId,
                    IsRoot = role.IsRoot,
                    IsDefault = role.IsDefault,
                    AccessResourceIds = DbContext.Set<RoleAccessResource>()
                        .Where(item => item.RoleId == role.Id && item.IsActive)
                        .OrderBy(item => item.AccessResourceId)
                        .Select(item => item.AccessResourceId)
                        .ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return roles;
        }

        public async Task<IReadOnlyCollection<RoleResponse>> GetRolesByContract(long contractId, CancellationToken cancellationToken = default)
        {
            List<RoleResponse> roles = await (
                from role in DbContext.Set<Role>().AsNoTracking()
                where role.ContractId == contractId
                orderby role.Id
                select new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    ContractId = role.ContractId,
                    IsRoot = role.IsRoot,
                    IsDefault = role.IsDefault,
                    AccessResourceIds = DbContext.Set<RoleAccessResource>()
                        .Where(item => item.RoleId == role.Id && item.IsActive)
                        .OrderBy(item => item.AccessResourceId)
                        .Select(item => item.AccessResourceId)
                        .ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return roles;
        }

        public async Task<RoleResponse?> GetRoleById(long id, CancellationToken cancellationToken = default)
        {
            return await (
                from item in DbContext.Set<Role>().AsNoTracking()
                where item.Id == id
                select new RoleResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    ContractId = item.ContractId,
                    IsRoot = item.IsRoot,
                    IsDefault = item.IsDefault,
                    AccessResourceIds = DbContext.Set<RoleAccessResource>()
                        .Where(link => link.RoleId == item.Id && link.IsActive)
                        .OrderBy(link => link.AccessResourceId)
                        .Select(link => link.AccessResourceId)
                        .ToList(),
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task DeleteRole(long id, CancellationToken cancellationToken = default)
        {
            Role? role = await DbContext.Set<Role>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException(Localizer["role.notFound"]);
            }

            bool hasActiveUsers = await DbContext.Set<UserRole>()
                .AsNoTracking()
                .AnyAsync(item => item.RoleId == id && item.IsActive && !item.RevokedAt.HasValue, cancellationToken);

            if (hasActiveUsers)
            {
                throw new InvalidOperationException(Localizer["role.delete.hasActiveUsers"]);
            }

            List<RoleAccessResource> links = await DbContext.Set<RoleAccessResource>()
                .AsTracking()
                .Where(item => item.RoleId == id)
                .ToListAsync(cancellationToken);

            if (links.Count > 0)
            {
                DbContext.Set<RoleAccessResource>().RemoveRange(links);
            }

            List<UserRole> revokedAssignments = await DbContext.Set<UserRole>()
                .AsTracking()
                .Where(item => item.RoleId == id)
                .ToListAsync(cancellationToken);

            if (revokedAssignments.Count > 0)
            {
                DbContext.Set<UserRole>().RemoveRange(revokedAssignments);
            }

            DbContext.Set<Role>().Remove(role);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<RoleResponse?> GetDefaultRoleByContract(long contractId, CancellationToken cancellationToken = default)
        {
            return await (
                from role in DbContext.Set<Role>().AsNoTracking()
                where role.ContractId == contractId && role.IsDefault
                select new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    ContractId = role.ContractId,
                    IsRoot = role.IsRoot,
                    IsDefault = role.IsDefault,
                    AccessResourceIds = DbContext.Set<RoleAccessResource>()
                        .Where(item => item.RoleId == role.Id && item.IsActive)
                        .OrderBy(item => item.AccessResourceId)
                        .Select(item => item.AccessResourceId)
                        .ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task EnsureContract(long contractId, CancellationToken cancellationToken)
        {
            bool contractExists = await DbContext.Set<Contract>().AnyAsync(item => item.Id == contractId && item.IsActive, cancellationToken);
            if (!contractExists)
            {
                throw new InvalidOperationException(Localizer["contract.notFoundOrInactive"]);
            }
        }

        private async Task ClearDefaultRole(long contractId, CancellationToken cancellationToken, long? ignoredRoleId = null)
        {
            List<Role> roles = await (
                from role in DbContext.Set<Role>().AsTracking()
                where role.ContractId == contractId && role.IsDefault && (!ignoredRoleId.HasValue || role.Id != ignoredRoleId.Value)
                select role)
                .ToListAsync(cancellationToken);

            foreach (Role role in roles)
            {
                role.SetDefault(false);
            }

            if (roles.Count > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private static RoleResponse ToResponse(Role role)
        {
            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                ContractId = role.ContractId,
                IsRoot = role.IsRoot,
                IsDefault = role.IsDefault,
                AccessResourceIds = [],
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }

        private async Task<RoleResponse> GetRoleResponse(long id, CancellationToken cancellationToken)
        {
            RoleResponse? role = await (
                from item in DbContext.Set<Role>().AsNoTracking()
                where item.Id == id
                select new RoleResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    ContractId = item.ContractId,
                    IsRoot = item.IsRoot,
                    IsDefault = item.IsDefault,
                    AccessResourceIds = DbContext.Set<RoleAccessResource>()
                        .Where(link => link.RoleId == item.Id && link.IsActive)
                        .OrderBy(link => link.AccessResourceId)
                        .Select(link => link.AccessResourceId)
                        .ToList(),
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException(Localizer["role.notFound"]);
            }

            return role;
        }

        private async Task SyncAccessResources(long roleId, IReadOnlyCollection<long> accessResourceIds, CancellationToken cancellationToken)
        {
            long systemApplicationId = await (
                from role in DbContext.Set<Role>().AsNoTracking()
                join contract in DbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                where role.Id == roleId
                select contract.SystemApplicationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (systemApplicationId <= 0)
            {
                throw new InvalidOperationException(Localizer["role.contract.systemApplication.notFound"]);
            }

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
                throw new InvalidOperationException(Localizer["accessResource.invalid"]);
            }

            List<RoleAccessResource> existingLinks = await (
                from link in DbContext.Set<RoleAccessResource>().AsTracking()
                where link.RoleId == roleId
                select link)
                .ToListAsync(cancellationToken);

            foreach (RoleAccessResource existingLink in existingLinks)
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

            List<RoleAccessResource> newLinks = normalizedIds
                .Where(item => !existingResourceIds.Contains(item))
                .Select(item => new RoleAccessResource(roleId, item))
                .ToList();

            if (newLinks.Count > 0)
            {
                await DbContext.Set<RoleAccessResource>().AddRangeAsync(newLinks, cancellationToken);
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
