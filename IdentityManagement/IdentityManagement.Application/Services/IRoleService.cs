using Archon.Application.Services;
using IdentityManagement.Application.Requests.Roles;
using IdentityManagement.Application.Responses.Roles;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IRoleService : ICrudService<Role>
    {
        Task<RoleResponse> CreateRole(CreateRoleRequest request, CancellationToken cancellationToken = default);

        Task<RoleResponse> UpdateRole(long id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<RoleResponse>> GetActiveRoles(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<RoleResponse>> GetRolesByContract(long contractId, CancellationToken cancellationToken = default);

        Task<RoleResponse?> GetDefaultRoleByContract(long contractId, CancellationToken cancellationToken = default);

        Task<RoleResponse?> GetRoleById(long id, CancellationToken cancellationToken = default);

        Task DeleteRole(long id, CancellationToken cancellationToken = default);
    }
}
