using Archon.Infrastructure.Services;
using IdentityManagement.Application.Responses.UserRoles;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class UserRoleService : CrudService<UserRole>, IUserRoleService
    {
        public UserRoleService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<object> AssignUserToRole(long userId, long roleId, CancellationToken cancellationToken = default)
        {
            await EnsureDependencies(userId, roleId, cancellationToken);

            UserRole? existingUserRole = await GetCurrentUserRole(userId, roleId, cancellationToken);
            if (existingUserRole is not null)
            {
                throw new InvalidOperationException("User is already assigned to this role.");
            }

            UserRole userRole = new UserRole(userId, roleId);
            bool success = await Insert(cancellationToken, userRole);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return new
            {
                Message = "User role assigned successfully.",
                UserId = userId,
                RoleId = roleId
            };
        }

        public async Task<object> RevokeUserFromRole(long userId, long roleId, CancellationToken cancellationToken = default)
        {
            UserRole? userRole = await GetCurrentUserRole(userId, roleId, cancellationToken);
            if (userRole is null)
            {
                throw new InvalidOperationException("User role assignment was not found.");
            }

            userRole.Revoke();
            UserRole? result = await Update(userRole, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return new
            {
                Message = "User role revoked successfully.",
                UserId = userId,
                RoleId = roleId
            };
        }

        public async Task<object> ReactivateUserRole(long userId, long roleId, CancellationToken cancellationToken = default)
        {
            UserRole? userRole = await (
                from item in DbContext.Set<UserRole>().AsTracking()
                where item.UserId == userId && item.RoleId == roleId && !item.IsActive && item.RevokedAt.HasValue
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (userRole is null)
            {
                throw new InvalidOperationException("Inactive user role assignment was not found.");
            }

            userRole.Reactivate();
            UserRole? result = await Update(userRole, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return new
            {
                Message = "User role reactivated successfully.",
                UserId = userId,
                RoleId = roleId
            };
        }

        public async Task<IReadOnlyCollection<UserRoleResponse>> GetUserRoles(long userId, CancellationToken cancellationToken = default)
        {
            List<UserRoleResponse> userRoles = await BuildUserRoleQuery(userId: userId).ToListAsync(cancellationToken);
            return userRoles;
        }

        public async Task<IReadOnlyCollection<UserRoleResponse>> GetActiveUserRoles(long userId, CancellationToken cancellationToken = default)
        {
            List<UserRoleResponse> userRoles = await BuildUserRoleQuery(userId: userId, onlyActive: true).ToListAsync(cancellationToken);
            return userRoles;
        }

        public async Task<IReadOnlyCollection<UserRoleResponse>> GetRoleUsers(long roleId, CancellationToken cancellationToken = default)
        {
            List<UserRoleResponse> userRoles = await BuildUserRoleQuery(roleId: roleId, onlyActive: true).ToListAsync(cancellationToken);
            return userRoles;
        }

        public async Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesByContract(long contractId, CancellationToken cancellationToken = default)
        {
            List<UserRoleResponse> userRoles = await BuildUserRoleQuery(contractId: contractId).ToListAsync(cancellationToken);
            return userRoles;
        }

        public Task<bool> HasUserAccess(long userId, long roleId, CancellationToken cancellationToken = default)
        {
            return (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                where userRole.UserId == userId &&
                      userRole.RoleId == roleId &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue
                select userRole.Id)
                .AnyAsync(cancellationToken);
        }

        private async Task EnsureDependencies(long userId, long roleId, CancellationToken cancellationToken)
        {
            bool userExists = await DbContext.Set<User>().AnyAsync(item => item.Id == userId && item.IsActive, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException("User not found or inactive.");
            }

            bool roleExists = await DbContext.Set<Role>().AnyAsync(item => item.Id == roleId, cancellationToken);
            if (!roleExists)
            {
                throw new InvalidOperationException("Role not found.");
            }
        }

        private IQueryable<UserRoleResponse> BuildUserRoleQuery(long? userId = null, long? roleId = null, long? contractId = null, bool onlyActive = false)
        {
            return
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join user in DbContext.Set<User>().AsNoTracking() on userRole.UserId equals user.Id
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join contract in DbContext.Set<Contract>().AsNoTracking() on role.ContractId equals contract.Id
                join company in DbContext.Set<Company>().AsNoTracking() on contract.CompanyId equals company.Id
                where (!userId.HasValue || userRole.UserId == userId.Value) &&
                      (!roleId.HasValue || userRole.RoleId == roleId.Value) &&
                      (!contractId.HasValue || role.ContractId == contractId.Value) &&
                      (!onlyActive || (userRole.IsActive && !userRole.RevokedAt.HasValue))
                orderby user.Name, role.Name
                select new UserRoleResponse
                {
                    Id = userRole.Id,
                    UserId = user.Id,
                    Username = user.Username,
                    UserEmail = user.Email,
                    RoleId = role.Id,
                    RoleName = role.Name,
                    ContractId = role.ContractId,
                    IsRoot = role.IsRoot,
                    CompanyName = company.LegalName,
                    AssignedAt = userRole.AssignedAt,
                    RevokedAt = userRole.RevokedAt,
                    IsActive = userRole.IsActive
                };
        }

        private Task<UserRole?> GetCurrentUserRole(long userId, long roleId, CancellationToken cancellationToken)
        {
            return (
                from item in DbContext.Set<UserRole>().AsTracking()
                where item.UserId == userId &&
                      item.RoleId == roleId &&
                      item.IsActive &&
                      !item.RevokedAt.HasValue
                select item)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
