using Archon.Application.Services;
using IdentityManagement.Application.Requests.Applications;
using IdentityManagement.Application.Responses.Applications;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IApplicationService : ICrudService<Application>
    {
        Task<ApplicationResponse> CreateApplication(CreateApplicationRequest request, CancellationToken cancellationToken = default);

        Task<ApplicationResponse> UpdateApplication(long id, UpdateApplicationRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ApplicationResponse>> GetActiveApplications(CancellationToken cancellationToken = default);
    }
}
