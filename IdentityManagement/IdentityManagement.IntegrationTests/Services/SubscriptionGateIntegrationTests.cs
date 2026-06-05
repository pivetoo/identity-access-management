using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SubscriptionGateIntegrationTests : IntegrationTestBase
    {
        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company(
                legalName: "Empresa Gate LTDA",
                tradeName: "Empresa Gate",
                document: document,
                email: email,
                phoneNumber: "11999990001");

            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static async Task<Plan> SeedPlanAsync(DbContext dbContext, string name, int trialDays)
        {
            Plan plan = new Plan(name, 99.00m, BillingPeriod.Monthly, trialDays: trialDays);
            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            return plan;
        }

        private static async Task AddSubscriptionAsync(DbContext dbContext, Subscription subscription)
        {
            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();
        }

        [Test]
        public async Task Company_with_no_subscription_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "10000000000110", "nosub@example.com");

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, DateTimeOffset.UtcNow);

                blocked.Should().BeFalse();
            });
        }

        [Test]
        public async Task Active_subscription_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "20000000000120", "active@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Active Plan", trialDays: 0);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeFalse();
            });
        }

        [Test]
        public async Task PastDue_subscription_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "30000000000130", "pastdue@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "PastDue Plan", trialDays: 0);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                subscription.MarkPastDue();
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeFalse();
            });
        }

        [Test]
        public async Task Trialing_within_trial_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "40000000000140", "trial@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Trial Plan", trialDays: 14);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartTrialing(company.Id, plan.Id, now, 14);
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeFalse();
            });
        }

        [Test]
        public async Task Trialing_expired_is_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "50000000000150", "expired@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Expired Trial Plan", trialDays: 14);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartTrialing(company.Id, plan.Id, now.AddDays(-15), 14);
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeTrue();
            });
        }

        [Test]
        public async Task Suspended_subscription_is_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "60000000000160", "suspended@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Suspended Plan", trialDays: 0);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                subscription.Suspend();
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeTrue();
            });
        }

        [Test]
        public async Task Canceled_subscription_is_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "70000000000170", "canceled@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Canceled Plan", trialDays: 0);

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Subscription subscription = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                subscription.Cancel(now);
                await AddSubscriptionAsync(dbContext, subscription);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeTrue();
            });
        }

        [Test]
        public async Task Latest_subscription_determines_block_state()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptionService = sp.GetRequiredService<ISubscriptionService>();

                Company company = await SeedCompanyAsync(dbContext, "80000000000180", "latest@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Latest Plan", trialDays: 0);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                Subscription canceled = Subscription.StartActive(company.Id, plan.Id, now.AddMonths(-2), now.AddMonths(-1));
                canceled.Cancel(now.AddMonths(-1));
                await AddSubscriptionAsync(dbContext, canceled);

                Subscription active = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                await AddSubscriptionAsync(dbContext, active);

                bool blocked = await subscriptionService.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeFalse();
            });
        }
    }
}
