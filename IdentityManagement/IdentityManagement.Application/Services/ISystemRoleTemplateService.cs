using Archon.Application.Services;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Responses.SystemRoleTemplates;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface ISystemRoleTemplateService : ICrudService<SystemRoleTemplate>
    {
        Task<SystemRoleTemplateResponse> CreateSystemRoleTemplate(CreateSystemRoleTemplateRequest request, CancellationToken cancellationToken = default);

        Task<SystemRoleTemplateResponse> UpdateSystemRoleTemplate(long id, UpdateSystemRoleTemplateRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SystemRoleTemplateResponse>> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken = default);

        Task<SystemRoleTemplateResponse?> GetById(long id, CancellationToken cancellationToken = default);
    }
}
