using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class SubscriptionService : CrudService<Subscription>, ISubscriptionService
    {
        private readonly IBillingGateway gateway;

        public SubscriptionService(DbContext dbContext, IBillingGateway gateway) : base(dbContext)
        {
            this.gateway = gateway;
        }

        public async Task<SubscriptionResponse> AssignAsync(AssignSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            bool companyExists = await DbContext.Set<Company>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.CompanyId, cancellationToken);

            if (!companyExists)
            {
                throw new InvalidOperationException("company.notFound");
            }

            Plan? plan = await DbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            if (!plan.IsActive)
            {
                throw new InvalidOperationException("plan.inactive");
            }

            bool hasActiveSubscription = await DbContext.Set<Subscription>()
                .AsNoTracking()
                .AnyAsync(s => s.CompanyId == request.CompanyId && s.Status != SubscriptionStatus.Canceled, cancellationToken);

            if (hasActiveSubscription)
            {
                throw new InvalidOperationException("subscription.alreadyExists");
            }

            string? externalCustomerId = await gateway.CreateCustomerAsync(request.CompanyId, cancellationToken);
            GatewaySubscriptionResult? gatewayResult = await gateway.CreateSubscriptionAsync(request.CompanyId, request.PlanId, externalCustomerId, cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription;

            if (plan.TrialDays > 0)
            {
                subscription = Subscription.StartTrialing(request.CompanyId, request.PlanId, now, plan.TrialDays);
            }
            else
            {
                DateTimeOffset periodEnd = ComputePeriodEnd(now, plan.BillingPeriod);
                subscription = Subscription.StartActive(request.CompanyId, request.PlanId, now, periodEnd);
            }

            if (gatewayResult is not null)
            {
                subscription.LinkGateway(
                    "noop",
                    gatewayResult.ExternalCustomerId ?? string.Empty,
                    gatewayResult.ExternalSubscriptionId);
            }

            bool success = await Insert(cancellationToken, subscription);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(subscription, plan.Name);
        }

        public async Task<SubscriptionResponse> ChangePlanAsync(long companyId, ChangePlanRequest request, CancellationToken cancellationToken = default)
        {
            Subscription? subscription = await (
                from s in DbContext.Set<Subscription>().AsTracking()
                where s.CompanyId == companyId && s.Status != SubscriptionStatus.Canceled
                select s)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                throw new InvalidOperationException("subscription.notFound");
            }

            Plan? newPlan = await DbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

            if (newPlan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            if (!newPlan.IsActive)
            {
                throw new InvalidOperationException("plan.inactive");
            }

            subscription.ChangePlan(request.PlanId);

            Subscription? result = await Update(subscription, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result, newPlan.Name);
        }

        public async Task<SubscriptionResponse> CancelAsync(long companyId, CancellationToken cancellationToken = default)
        {
            Subscription? subscription = await (
                from s in DbContext.Set<Subscription>().AsTracking()
                where s.CompanyId == companyId && s.Status != SubscriptionStatus.Canceled
                select s)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                throw new InvalidOperationException("subscription.notFound");
            }

            if (!string.IsNullOrWhiteSpace(subscription.ExternalSubscriptionId))
            {
                await gateway.CancelSubscriptionAsync(subscription.ExternalSubscriptionId, cancellationToken);
            }

            subscription.Cancel(DateTimeOffset.UtcNow);

            Subscription? result = await Update(subscription, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            string planName = await ResolvePlanNameAsync(result.PlanId, cancellationToken);
            return ToResponse(result, planName);
        }

        public async Task<SubscriptionResponse> SuspendAsync(long companyId, CancellationToken cancellationToken = default)
        {
            Subscription? subscription = await (
                from s in DbContext.Set<Subscription>().AsTracking()
                where s.CompanyId == companyId && s.Status != SubscriptionStatus.Canceled
                select s)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                throw new InvalidOperationException("subscription.notFound");
            }

            subscription.Suspend();

            Subscription? result = await Update(subscription, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            string planName = await ResolvePlanNameAsync(result.PlanId, cancellationToken);
            return ToResponse(result, planName);
        }

        public async Task<SubscriptionResponse> ActivateAsync(long companyId, CancellationToken cancellationToken = default)
        {
            Subscription? subscription = await (
                from s in DbContext.Set<Subscription>().AsTracking()
                where s.CompanyId == companyId && s.Status != SubscriptionStatus.Canceled
                select s)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                throw new InvalidOperationException("subscription.notFound");
            }

            Plan? plan = await DbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset periodEnd = ComputePeriodEnd(now, plan.BillingPeriod);
            subscription.Activate(now, periodEnd);

            Subscription? result = await Update(subscription, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result, plan.Name);
        }

        public async Task<SubscriptionResponse?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        {
            SubscriptionResponse? response = await (
                from s in DbContext.Set<Subscription>().AsNoTracking()
                join p in DbContext.Set<Plan>().AsNoTracking() on s.PlanId equals p.Id
                where s.CompanyId == companyId
                orderby s.StartedAt descending
                select new SubscriptionResponse
                {
                    Id = s.Id,
                    CompanyId = s.CompanyId,
                    PlanId = s.PlanId,
                    PlanName = p.Name,
                    Status = s.Status,
                    StartedAt = s.StartedAt,
                    TrialEndsAt = s.TrialEndsAt,
                    CurrentPeriodStart = s.CurrentPeriodStart,
                    CurrentPeriodEnd = s.CurrentPeriodEnd,
                    CanceledAt = s.CanceledAt,
                    ProviderName = s.ProviderName
                })
                .FirstOrDefaultAsync(cancellationToken);

            return response;
        }

        public async Task<bool> IsCompanyBlockedAsync(long companyId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            Subscription? subscription = await DbContext.Set<Subscription>()
                .AsNoTracking()
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null)
            {
                return false;
            }

            return !subscription.GrantsAccess(now);
        }

        private async Task<string> ResolvePlanNameAsync(long planId, CancellationToken cancellationToken)
        {
            string? name = await DbContext.Set<Plan>()
                .AsNoTracking()
                .Where(p => p.Id == planId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return name ?? string.Empty;
        }

        private static DateTimeOffset ComputePeriodEnd(DateTimeOffset start, BillingPeriod period)
        {
            if (period == BillingPeriod.Yearly)
            {
                return start.AddYears(1);
            }

            return start.AddMonths(1);
        }

        private static SubscriptionResponse ToResponse(Subscription subscription, string planName)
        {
            return new SubscriptionResponse
            {
                Id = subscription.Id,
                CompanyId = subscription.CompanyId,
                PlanId = subscription.PlanId,
                PlanName = planName,
                Status = subscription.Status,
                StartedAt = subscription.StartedAt,
                TrialEndsAt = subscription.TrialEndsAt,
                CurrentPeriodStart = subscription.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                CanceledAt = subscription.CanceledAt,
                ProviderName = subscription.ProviderName
            };
        }
    }
}
