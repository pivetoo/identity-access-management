using Archon.Application.Services;
using IdentityManagement.Application.Requests.UserRoles;
using IdentityManagement.Application.Responses.UserRoles;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IUserRoleService : ICrudService<UserRole>
    {
        Task<object> AssignUserToRole(long userId, long roleId, CancellationToken cancellationToken = default);

        Task<object> RevokeUserFromRole(long userId, long roleId, CancellationToken cancellationToken = default);

        Task<object> ReactivateUserRole(long userId, long roleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserRoleResponse>> GetUserRoles(long userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserRoleResponse>> GetActiveUserRoles(long userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserRoleResponse>> GetRoleUsers(long roleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesByContract(long contractId, CancellationToken cancellationToken = default);

        Task<bool> HasUserAccess(long userId, long roleId, CancellationToken cancellationToken = default);
    }
}
