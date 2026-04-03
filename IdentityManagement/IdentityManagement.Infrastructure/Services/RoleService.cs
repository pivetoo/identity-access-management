using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Roles;
using IdentityManagement.Application.Responses.Roles;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class RoleService : CrudService<Role>, IRoleService
    {
        public RoleService(DbContext dbContext) : base(dbContext)
        {
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

            return ToResponse(role);
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
                throw new InvalidOperationException("Role not found.");
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

            return ToResponse(result);
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
                orderby role.Name
                select new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    ContractId = role.ContractId,
                    IsRoot = role.IsRoot,
                    IsDefault = role.IsDefault,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return roles;
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
                throw new InvalidOperationException("Contract not found or inactive.");
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
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }
    }
}
