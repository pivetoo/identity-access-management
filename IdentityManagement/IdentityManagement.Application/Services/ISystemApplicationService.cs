using Archon.Application.Services;
using IdentityManagement.Application.Requests.SystemApplications;
using IdentityManagement.Application.Responses.SystemApplications;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface ISystemApplicationService : ICrudService<SystemApplication>
    {
        Task<SystemApplicationResponse> CreateSystemApplication(CreateSystemApplicationRequest request, CancellationToken cancellationToken = default);

        Task<SystemApplicationResponse> UpdateSystemApplication(long id, UpdateSystemApplicationRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SystemApplicationResponse>> GetActiveSystemApplications(CancellationToken cancellationToken = default);
    }
}
