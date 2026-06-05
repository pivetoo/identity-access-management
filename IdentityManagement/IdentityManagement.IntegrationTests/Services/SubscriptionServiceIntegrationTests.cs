using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SubscriptionServiceIntegrationTests : IntegrationTestBase
    {
        // Cria uma empresa diretamente no DbContext para isolar testes de subscription
        // sem depender do fluxo completo de onboarding.
        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company(
                legalName: "Empresa Teste LTDA",
                tradeName: "Empresa Teste",
                document: document,
                email: email,
                phoneNumber: "11999990001");

            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static async Task<PlanResponse> SeedPlanAsync(IPlanService planService, string name, int trialDays, BillingPeriod period = BillingPeriod.Monthly)
        {
            return await planService.CreateAsync(new CreatePlanRequest
            {
                Name = name,
                PriceAmount = 99.00m,
                BillingPeriod = period,
                TrialDays = trialDays
            });
        }

        [Test]
        public async Task Assign_with_trial_plan_starts_trialing()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "11111111000111", "trial@example.com");
                PlanResponse plan = await SeedPlanAsync(planService, "Trial Plan", trialDays: 14);

                DateTimeOffset before = DateTimeOffset.UtcNow;

                SubscriptionResponse response = await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                response.Status.Should().Be(SubscriptionStatus.Trialing);
                response.TrialEndsAt.Should().NotBeNull();
                response.TrialEndsAt!.Value.Should().BeCloseTo(before.AddDays(14), TimeSpan.FromSeconds(5));
                response.CompanyId.Should().Be(company.Id);
                response.PlanId.Should().Be(plan.Id);
                response.PlanName.Should().Be("Trial Plan");
            });
        }

        [Test]
        public async Task Assign_with_no_trial_plan_starts_active()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "22222222000122", "active@example.com");
                PlanResponse plan = await SeedPlanAsync(planService, "Active Monthly Plan", trialDays: 0, period: BillingPeriod.Monthly);

                DateTimeOffset before = DateTimeOffset.UtcNow;

                SubscriptionResponse response = await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                response.Status.Should().Be(SubscriptionStatus.Active);
                response.TrialEndsAt.Should().BeNull();
                response.CurrentPeriodEnd.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
                response.CompanyId.Should().Be(company.Id);
                response.PlanId.Should().Be(plan.Id);
            });
        }

        [Test]
        public async Task Assign_twice_for_same_company_is_rejected()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "33333333000133", "duplicate@example.com");
                PlanResponse plan = await SeedPlanAsync(planService, "Duplicate Test Plan", trialDays: 0);

                await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                Func<Task> secondAssign = () => subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                await secondAssign.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*subscription.alreadyExists*");
            });
        }

        [Test]
        public async Task Cancel_sets_status_canceled()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "44444444000144", "cancel@example.com");
                PlanResponse plan = await SeedPlanAsync(planService, "Cancel Test Plan", trialDays: 0);

                await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                DateTimeOffset beforeCancel = DateTimeOffset.UtcNow;

                SubscriptionResponse canceled = await subscriptionService.CancelAsync(company.Id);

                canceled.Status.Should().Be(SubscriptionStatus.Canceled);
                canceled.CanceledAt.Should().NotBeNull();
                canceled.CanceledAt!.Value.Should().BeCloseTo(beforeCancel, TimeSpan.FromSeconds(5));

                SubscriptionResponse? fetched = await subscriptionService.GetByCompanyAsync(company.Id);
                fetched.Should().NotBeNull();
                fetched!.Status.Should().Be(SubscriptionStatus.Canceled);
            });
        }

        [Test]
        public async Task Suspend_then_Activate_transitions()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "55555555000155", "suspend@example.com");
                PlanResponse plan = await SeedPlanAsync(planService, "Suspend Test Plan", trialDays: 0);

                await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = plan.Id
                });

                SubscriptionResponse suspended = await subscriptionService.SuspendAsync(company.Id);
                suspended.Status.Should().Be(SubscriptionStatus.Suspended);

                SubscriptionResponse activated = await subscriptionService.ActivateAsync(company.Id);
                activated.Status.Should().Be(SubscriptionStatus.Active);
            });
        }

        [Test]
        public async Task ChangePlan_updates_plan()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IPlanService planService = sp.GetRequiredService<IPlanService>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "66666666000166", "changeplan@example.com");
                PlanResponse planA = await SeedPlanAsync(planService, "Plan A", trialDays: 0);
                PlanResponse planB = await SeedPlanAsync(planService, "Plan B", trialDays: 0);

                await subscriptionService.AssignAsync(new AssignSubscriptionRequest
                {
                    CompanyId = company.Id,
                    PlanId = planA.Id
                });

                SubscriptionResponse changed = await subscriptionService.ChangePlanAsync(company.Id, new ChangePlanRequest
                {
                    PlanId = planB.Id
                });

                changed.PlanId.Should().Be(planB.Id);
                changed.PlanName.Should().Be("Plan B");

                SubscriptionResponse? fetched = await subscriptionService.GetByCompanyAsync(company.Id);
                fetched.Should().NotBeNull();
                fetched!.PlanId.Should().Be(planB.Id);
                fetched.PlanName.Should().Be("Plan B");
            });
        }
    }
}
