using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly DbContext dbContext;

        public PaymentService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<PaymentResponse>> ListAsync(
            long? companyId = null,
            PaymentStatus? status = null,
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Payment> query = dbContext.Set<Payment>().AsNoTracking();

            if (companyId.HasValue)
            {
                query = query.Where(payment => payment.CompanyId == companyId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(payment => payment.Status == status.Value);
            }

            return await ProjectAsync(query, take, cancellationToken);
        }

        public async Task<IReadOnlyCollection<PaymentResponse>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        {
            IQueryable<Payment> query = dbContext.Set<Payment>()
                .AsNoTracking()
                .Where(payment => payment.CompanyId == companyId);

            return await ProjectAsync(query, null, cancellationToken);
        }

        public async Task<IReadOnlyCollection<WebhookEventResponse>> ListWebhookEventsAsync(
            int? take = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<BillingWebhookEvent> query = dbContext.Set<BillingWebhookEvent>()
                .AsNoTracking()
                .OrderByDescending(item => item.Id);

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query
                .Select(item => new WebhookEventResponse
                {
                    Id = item.Id,
                    ExternalEventId = item.ExternalEventId,
                    EventType = item.EventType,
                    ExternalPaymentId = item.ExternalPaymentId,
                    Outcome = item.Outcome,
                    ProcessedAt = item.ProcessedAt,
                    RawPayload = item.RawPayload
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<IReadOnlyCollection<PaymentResponse>> ProjectAsync(IQueryable<Payment> query, int? take, CancellationToken cancellationToken)
        {
            IQueryable<PaymentResponse> projected =
                from payment in query
                join company in dbContext.Set<Company>().AsNoTracking()
                    on payment.CompanyId equals company.Id into companies
                from company in companies.DefaultIfEmpty()
                orderby payment.Id descending
                select new PaymentResponse
                {
                    Id = payment.Id,
                    ExternalPaymentId = payment.ExternalPaymentId,
                    CompanyId = payment.CompanyId,
                    CompanyName = company != null ? company.TradeName : null,
                    ExternalSubscriptionId = payment.ExternalSubscriptionId,
                    Value = payment.Value,
                    BillingType = payment.BillingType,
                    Status = payment.Status,
                    DueDate = payment.DueDate,
                    PaidDate = payment.PaidDate,
                    CreatedAt = payment.CreatedAt
                };

            if (take.HasValue)
            {
                projected = projected.Take(take.Value);
            }

            return await projected.ToListAsync(cancellationToken);
        }
    }
}
