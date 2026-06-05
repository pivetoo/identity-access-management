using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class PlanService : CrudService<Plan>, IPlanService
    {
        public PlanService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PlanResponse> CreateAsync(CreatePlanRequest request, CancellationToken cancellationToken = default)
        {
            Plan plan = new Plan(
                request.Name,
                request.PriceAmount,
                request.BillingPeriod,
                request.Currency ?? "BRL",
                request.TrialDays ?? 0,
                request.Description);

            bool success = await Insert(cancellationToken, plan);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(plan);
        }

        public async Task<PlanResponse> UpdateAsync(long id, UpdatePlanRequest request, CancellationToken cancellationToken = default)
        {
            Plan? plan = await (
                from item in DbContext.Set<Plan>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            plan.Update(
                request.Name,
                request.PriceAmount,
                request.BillingPeriod,
                request.Currency,
                request.TrialDays,
                request.Description);

            Plan? result = await Update(plan, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result);
        }

        public async Task<IReadOnlyCollection<PlanResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            List<PlanResponse> plans = await (
                from plan in DbContext.Set<Plan>().AsNoTracking()
                where plan.IsActive
                orderby plan.Name
                select new PlanResponse
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    PriceAmount = plan.PriceAmount,
                    Currency = plan.Currency,
                    BillingPeriod = plan.BillingPeriod,
                    TrialDays = plan.TrialDays,
                    IsActive = plan.IsActive
                })
                .ToListAsync(cancellationToken);

            return plans;
        }

        public async Task<IReadOnlyCollection<PlanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            List<PlanResponse> plans = await (
                from plan in DbContext.Set<Plan>().AsNoTracking()
                orderby plan.Name
                select new PlanResponse
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description,
                    PriceAmount = plan.PriceAmount,
                    Currency = plan.Currency,
                    BillingPeriod = plan.BillingPeriod,
                    TrialDays = plan.TrialDays,
                    IsActive = plan.IsActive
                })
                .ToListAsync(cancellationToken);

            return plans;
        }

        public async Task<PlanResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            PlanResponse? plan = await (
                from item in DbContext.Set<Plan>().AsNoTracking()
                where item.Id == id
                select new PlanResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    PriceAmount = item.PriceAmount,
                    Currency = item.Currency,
                    BillingPeriod = item.BillingPeriod,
                    TrialDays = item.TrialDays,
                    IsActive = item.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            return plan;
        }

        public async Task ActivateAsync(long id, CancellationToken cancellationToken = default)
        {
            Plan? plan = await (
                from item in DbContext.Set<Plan>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            plan.Activate();

            Plan? result = await Update(plan, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }
        }

        public async Task DeactivateAsync(long id, CancellationToken cancellationToken = default)
        {
            Plan? plan = await (
                from item in DbContext.Set<Plan>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            plan.Deactivate();

            Plan? result = await Update(plan, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }
        }

        private static PlanResponse ToResponse(Plan plan)
        {
            return new PlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                PriceAmount = plan.PriceAmount,
                Currency = plan.Currency,
                BillingPeriod = plan.BillingPeriod,
                TrialDays = plan.TrialDays,
                IsActive = plan.IsActive
            };
        }
    }
}
