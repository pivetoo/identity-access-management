using Archon.Application.Services;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface ISubscriptionService : ICrudService<Subscription>
    {
        Task<SubscriptionResponse> AssignAsync(AssignSubscriptionRequest request, CancellationToken cancellationToken = default);

        Task<SubscriptionResponse> ChangePlanAsync(long companyId, ChangePlanRequest request, CancellationToken cancellationToken = default);

        Task<SubscriptionResponse> CancelAsync(long companyId, CancellationToken cancellationToken = default);

        Task<SubscriptionResponse> SuspendAsync(long companyId, CancellationToken cancellationToken = default);

        Task<SubscriptionResponse> ActivateAsync(long companyId, CancellationToken cancellationToken = default);

        Task<SubscriptionResponse?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

        Task<bool> IsCompanyBlockedAsync(long companyId, DateTimeOffset now, CancellationToken cancellationToken = default);
    }
}
