using Archon.Application.Services;
using IdentityManagement.Application.Requests.Applications;
using IdentityManagement.Application.Responses.Applications;
using ApplicationEntity = IdentityManagement.Domain.Entities.Application;

namespace IdentityManagement.Application.Services
{
    public interface IApplicationService : ICrudService<ApplicationEntity>
    {
        Task<ApplicationResponse> CreateApplication(CreateApplicationRequest request, CancellationToken cancellationToken = default);

        Task<ApplicationResponse> UpdateApplication(long id, UpdateApplicationRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ApplicationResponse>> GetActiveApplications(CancellationToken cancellationToken = default);
    }
}
