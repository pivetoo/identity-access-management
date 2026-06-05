using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class BillingWebhookServiceIntegrationTests : IntegrationTestBase
    {
        private const string ExternalSubscriptionId = "sub_test";

        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company(
                legalName: "Empresa Webhook LTDA",
                tradeName: "Empresa Webhook",
                document: document,
                email: email,
                phoneNumber: "11999990001");

            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static async Task<Plan> SeedPlanAsync(DbContext dbContext, string name, BillingPeriod period = BillingPeriod.Monthly)
        {
            Plan plan = new Plan(name, 99.00m, period, "BRL", 0, null);

            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            return plan;
        }

        // Semeia uma Subscription diretamente no DbContext com um ExternalSubscriptionId conhecido,
        // usando as fabricas de dominio + LinkGateway.
        private static async Task<Subscription> SeedSubscriptionAsync(
            DbContext dbContext,
            long companyId,
            long planId,
            SubscriptionStatus status,
            int trialDays = 14)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription;

            switch (status)
            {
                case SubscriptionStatus.Trialing:
                    subscription = Subscription.StartTrialing(companyId, planId, now, trialDays);
                    break;
                case SubscriptionStatus.Active:
                    subscription = Subscription.StartActive(companyId, planId, now, now.AddMonths(1));
                    break;
                case SubscriptionStatus.PastDue:
                    subscription = Subscription.StartActive(companyId, planId, now, now.AddMonths(1));
                    subscription.MarkPastDue();
                    break;
                case SubscriptionStatus.Suspended:
                    subscription = Subscription.StartActive(companyId, planId, now, now.AddMonths(1));
                    subscription.Suspend();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }

            subscription.LinkGateway("asaas", "cus_test", ExternalSubscriptionId);

            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();

            return subscription;
        }

        private static AsaasWebhookPayload BuildPayload(string eventId, string eventType, string? subscriptionId = ExternalSubscriptionId)
        {
            return new AsaasWebhookPayload
            {
                Id = eventId,
                Event = eventType,
                Payment = new AsaasPaymentInfo
                {
                    Id = "pay_" + eventId,
                    Subscription = subscriptionId,
                    Status = "RECEIVED"
                }
            };
        }

        private static async Task<SubscriptionStatus> ReadStatusAsync(DbContext dbContext, long subscriptionId)
        {
            Subscription subscription = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstAsync(s => s.Id == subscriptionId);

            return subscription.Status;
        }

        [Test]
        public async Task PaymentReceived_on_trialing_becomes_active()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "11111111000111", "trial-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Trialing Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.Trialing);

                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_1", "PAYMENT_RECEIVED"));

                applied.Should().BeTrue();
                (await ReadStatusAsync(dbContext, subscription.Id)).Should().Be(SubscriptionStatus.Active);
            });
        }

        [Test]
        public async Task PaymentReceived_on_pastdue_becomes_active()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "22222222000122", "pastdue-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "PastDue Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.PastDue);

                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_2", "PAYMENT_RECEIVED"));

                applied.Should().BeTrue();
                (await ReadStatusAsync(dbContext, subscription.Id)).Should().Be(SubscriptionStatus.Active);
            });
        }

        [Test]
        public async Task PaymentOverdue_on_active_becomes_pastdue()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "33333333000133", "overdue-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Overdue Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.Active);

                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_3", "PAYMENT_OVERDUE"));

                applied.Should().BeTrue();
                (await ReadStatusAsync(dbContext, subscription.Id)).Should().Be(SubscriptionStatus.PastDue);
            });
        }

        [Test]
        public async Task PaymentRefunded_suspends()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "44444444000144", "refunded-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Refunded Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.Active);

                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_4", "PAYMENT_REFUNDED"));

                applied.Should().BeTrue();
                (await ReadStatusAsync(dbContext, subscription.Id)).Should().Be(SubscriptionStatus.Suspended);
            });
        }

        [Test]
        public async Task Duplicate_event_id_is_ignored_on_second_call()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "55555555000155", "dup-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Dup Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.Trialing);

                bool first = await service.ProcessAsaasEventAsync(BuildPayload("evt_dup", "PAYMENT_RECEIVED"));
                bool second = await service.ProcessAsaasEventAsync(BuildPayload("evt_dup", "PAYMENT_RECEIVED"));

                first.Should().BeTrue();
                second.Should().BeFalse();
                (await ReadStatusAsync(dbContext, subscription.Id)).Should().Be(SubscriptionStatus.Active);
            });
        }

        [Test]
        public async Task Unknown_subscription_id_returns_false_without_throwing()
        {
            await InScopeAsync(async sp =>
            {
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_unknown", "PAYMENT_RECEIVED", "sub_does_not_exist"));

                applied.Should().BeFalse();
            });
        }

        [Test]
        public async Task PaymentReceived_on_active_renews_period_without_throwing()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "66666666000166", "renew-wh@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Renew Plan");
                Subscription subscription = await SeedSubscriptionAsync(dbContext, company.Id, plan.Id, SubscriptionStatus.Active);

                DateTimeOffset before = DateTimeOffset.UtcNow;
                bool applied = await service.ProcessAsaasEventAsync(BuildPayload("evt_renew", "PAYMENT_RECEIVED"));

                applied.Should().BeTrue();

                Subscription renewed = await dbContext.Set<Subscription>()
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == subscription.Id);

                renewed.Status.Should().Be(SubscriptionStatus.Active);
                renewed.CurrentPeriodEnd.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(10));
            });
        }
    }
}
