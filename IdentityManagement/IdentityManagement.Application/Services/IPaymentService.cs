using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Services
{
    public interface IPaymentService
    {
        Task<IReadOnlyCollection<PaymentResponse>> ListAsync(
            long? companyId = null,
            PaymentStatus? status = null,
            int? take = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PaymentResponse>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<WebhookEventResponse>> ListWebhookEventsAsync(
            int? take = null,
            CancellationToken cancellationToken = default);
    }
}
