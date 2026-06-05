using Archon.Application.Services;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IPlanService : ICrudService<Plan>
    {
        Task<PlanResponse> CreateAsync(CreatePlanRequest request, CancellationToken cancellationToken = default);

        Task<PlanResponse> UpdateAsync(long id, UpdatePlanRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PlanResponse>> GetActiveAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PlanResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PlanResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        Task ActivateAsync(long id, CancellationToken cancellationToken = default);

        Task DeactivateAsync(long id, CancellationToken cancellationToken = default);
    }
}
