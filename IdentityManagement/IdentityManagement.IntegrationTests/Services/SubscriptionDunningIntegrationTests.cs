using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SubscriptionDunningIntegrationTests : IntegrationTestBase
    {
        private const int GraceDays = 7;

        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company(
                legalName: "Empresa Dunning LTDA",
                tradeName: "Empresa Dunning",
                document: document,
                email: email,
                phoneNumber: "11999990001");

            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static async Task<Plan> SeedPlanAsync(DbContext dbContext, string name)
        {
            Plan plan = new Plan(name, 99.00m, BillingPeriod.Monthly, trialDays: 0);
            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            return plan;
        }

        private static async Task<Subscription> SeedPastDueSubscriptionAsync(
            DbContext dbContext,
            long companyId,
            long planId,
            DateTimeOffset now,
            string externalSubscriptionId)
        {
            Subscription subscription = Subscription.StartActive(companyId, planId, now.AddMonths(-1), now.AddDays(-2));
            subscription.MarkPastDue();
            subscription.LinkGateway("asaas", "cus_dunning", externalSubscriptionId);

            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();

            return subscription;
        }

        private static async Task SeedOverduePaymentAsync(
            DbContext dbContext,
            Subscription subscription,
            string externalPaymentId,
            DateTimeOffset dueDate)
        {
            Payment payment = new Payment(externalPaymentId, 99.00m, PaymentStatus.Overdue);
            payment.SetSubscription(subscription.Id, subscription.CompanyId, subscription.ExternalSubscriptionId);
            payment.SetDetails("BOLETO", dueDate);

            await dbContext.Set<Payment>().AddAsync(payment);
            await dbContext.SaveChangesAsync();
        }

        private static async Task<Subscription> ReloadAsync(DbContext dbContext, long subscriptionId)
        {
            return await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstAsync(s => s.Id == subscriptionId);
        }

        [Test]
        public async Task Overdue_payment_past_grace_blocks_without_changing_status()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "11111111000111", "block@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Block Plan");
                Subscription subscription = await SeedPastDueSubscriptionAsync(dbContext, company.Id, plan.Id, now, "sub_block");
                await SeedOverduePaymentAsync(dbContext, subscription, "pay_block", now.AddDays(-8));

                int blocked = await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                blocked.Should().Be(1);

                Subscription reloaded = await ReloadAsync(dbContext, subscription.Id);
                reloaded.IsBlocked.Should().BeTrue();
                reloaded.BlockReason.Should().Be(SubscriptionBlockReason.PaymentOverdue);
                reloaded.BlockedAt.Should().NotBeNull();
                reloaded.Status.Should().Be(SubscriptionStatus.PastDue);
            });
        }

        [Test]
        public async Task Overdue_payment_within_grace_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "22222222000122", "withingrace@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Within Grace Plan");
                Subscription subscription = await SeedPastDueSubscriptionAsync(dbContext, company.Id, plan.Id, now, "sub_grace");
                await SeedOverduePaymentAsync(dbContext, subscription, "pay_grace", now.AddDays(-3));

                int blocked = await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                blocked.Should().Be(0);

                Subscription reloaded = await ReloadAsync(dbContext, subscription.Id);
                reloaded.IsBlocked.Should().BeFalse();
                reloaded.Status.Should().Be(SubscriptionStatus.PastDue);
            });
        }

        [Test]
        public async Task Active_subscription_is_not_blocked()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "33333333000133", "active-dunning@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Active Dunning Plan");

                Subscription subscription = Subscription.StartActive(company.Id, plan.Id, now, now.AddMonths(1));
                await dbContext.Set<Subscription>().AddAsync(subscription);
                await dbContext.SaveChangesAsync();

                int blocked = await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                blocked.Should().Be(0);

                Subscription reloaded = await ReloadAsync(dbContext, subscription.Id);
                reloaded.IsBlocked.Should().BeFalse();
            });
        }

        [Test]
        public async Task Already_blocked_subscription_is_not_double_counted()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "44444444000144", "already@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Already Blocked Plan");
                Subscription subscription = await SeedPastDueSubscriptionAsync(dbContext, company.Id, plan.Id, now, "sub_already");
                await SeedOverduePaymentAsync(dbContext, subscription, "pay_already", now.AddDays(-8));

                int first = await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);
                int second = await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                first.Should().Be(1);
                second.Should().Be(0);
            });
        }

        [Test]
        public async Task IsCompanyBlocked_is_true_when_latest_subscription_is_blocked_even_if_grants_access()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "55555555000155", "gate-block@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Gate Block Plan");
                Subscription subscription = await SeedPastDueSubscriptionAsync(dbContext, company.Id, plan.Id, now, "sub_gate");
                await SeedOverduePaymentAsync(dbContext, subscription, "pay_gate", now.AddDays(-8));

                // PastDue concede acesso (GrantsAccess == true), mas o bloqueio deve barrar mesmo assim.
                await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                bool blocked = await subscriptions.IsCompanyBlockedAsync(company.Id, now);

                blocked.Should().BeTrue();
            });
        }

        [Test]
        public async Task Payment_received_unblocks_and_activates()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISubscriptionService subscriptions = sp.GetRequiredService<ISubscriptionService>();
                IBillingWebhookService webhook = sp.GetRequiredService<IBillingWebhookService>();

                DateTimeOffset now = DateTimeOffset.UtcNow;
                Company company = await SeedCompanyAsync(dbContext, "66666666000166", "unblock@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Unblock Plan");
                Subscription subscription = await SeedPastDueSubscriptionAsync(dbContext, company.Id, plan.Id, now, "sub_unblock");
                await SeedOverduePaymentAsync(dbContext, subscription, "pay_unblock", now.AddDays(-8));

                await subscriptions.BlockOverduePastGraceAsync(GraceDays, now);

                AsaasWebhookPayload payload = new AsaasWebhookPayload
                {
                    Id = "evt_unblock",
                    Event = "PAYMENT_RECEIVED",
                    Payment = new AsaasPaymentInfo
                    {
                        Id = "pay_unblock_received",
                        Subscription = "sub_unblock",
                        Status = "RECEIVED"
                    }
                };

                bool applied = await webhook.ProcessAsaasEventAsync(payload);

                applied.Should().BeTrue();

                Subscription reloaded = await ReloadAsync(dbContext, subscription.Id);
                reloaded.IsBlocked.Should().BeFalse();
                reloaded.BlockReason.Should().Be(SubscriptionBlockReason.None);
                reloaded.BlockedAt.Should().BeNull();
                reloaded.Status.Should().Be(SubscriptionStatus.Active);
            });
        }
    }
}
