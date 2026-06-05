using System.Text.Json;
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
    public sealed class BillingPaymentsIntegrationTests : IntegrationTestBase
    {
        private const string ExternalSubscriptionId = "sub_pay_test";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static async Task<Company> SeedCompanyAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company(
                legalName: "Empresa Pagamentos LTDA",
                tradeName: "Empresa Pagamentos",
                document: document,
                email: email,
                phoneNumber: "11999990002");

            await dbContext.Set<Company>().AddAsync(company);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static async Task<Plan> SeedPlanAsync(DbContext dbContext, string name)
        {
            Plan plan = new Plan(name, 99.00m, BillingPeriod.Monthly, "BRL", 0, null);

            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            return plan;
        }

        private static async Task<Subscription> SeedTrialingSubscriptionAsync(
            DbContext dbContext,
            long companyId,
            long planId,
            string externalSubscriptionId = ExternalSubscriptionId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Subscription subscription = Subscription.StartTrialing(companyId, planId, now, 14);
            subscription.LinkGateway("asaas", "cus_pay_test", externalSubscriptionId);

            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();

            return subscription;
        }

        private static AsaasWebhookPayload BuildPayload(
            string eventId,
            string eventType,
            string paymentId,
            string? subscriptionId = ExternalSubscriptionId,
            decimal value = 99.00m,
            string? paymentDate = null)
        {
            return new AsaasWebhookPayload
            {
                Id = eventId,
                Event = eventType,
                Payment = new AsaasPaymentInfo
                {
                    Id = paymentId,
                    Subscription = subscriptionId,
                    Status = eventType,
                    Value = value,
                    BillingType = "PIX",
                    DueDate = "2026-06-10",
                    PaymentDate = paymentDate,
                    Customer = "cus_pay_test"
                }
            };
        }

        private static string Raw(AsaasWebhookPayload payload)
        {
            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        private static async Task<Payment?> ReadPaymentAsync(DbContext dbContext, string externalPaymentId)
        {
            return await dbContext.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId);
        }

        [Test]
        public async Task PaymentCreated_persists_pending_payment_linked_and_records_event()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "10000000000110", "created-pay@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Created Plan");
                Subscription subscription = await SeedTrialingSubscriptionAsync(dbContext, company.Id, plan.Id);

                AsaasWebhookPayload payload = BuildPayload("evt_created", "PAYMENT_CREATED", "pay_created");
                await service.ProcessAsaasEventAsync(payload, Raw(payload));

                Payment? payment = await ReadPaymentAsync(dbContext, "pay_created");
                payment.Should().NotBeNull();
                payment!.Status.Should().Be(PaymentStatus.Pending);
                payment.CompanyId.Should().Be(company.Id);
                payment.SubscriptionId.Should().Be(subscription.Id);
                payment.ExternalSubscriptionId.Should().Be(ExternalSubscriptionId);
                payment.Value.Should().Be(99.00m);
                payment.BillingType.Should().Be("PIX");

                BillingWebhookEvent webhookEvent = await dbContext.Set<BillingWebhookEvent>()
                    .AsNoTracking()
                    .FirstAsync(e => e.ExternalEventId == "evt_created");

                webhookEvent.ExternalPaymentId.Should().Be("pay_created");
                webhookEvent.RawPayload.Should().NotBeNullOrEmpty();
                webhookEvent.Outcome.Should().Contain("payment.created");
            });
        }

        [Test]
        public async Task PaymentReceived_updates_same_payment_row_and_activates_subscription()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "20000000000120", "received-pay@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Received Plan");
                Subscription subscription = await SeedTrialingSubscriptionAsync(dbContext, company.Id, plan.Id);

                AsaasWebhookPayload created = BuildPayload("evt_c1", "PAYMENT_CREATED", "pay_shared");
                await service.ProcessAsaasEventAsync(created, Raw(created));

                AsaasWebhookPayload received = BuildPayload("evt_r1", "PAYMENT_RECEIVED", "pay_shared", paymentDate: "2026-06-04");
                bool applied = await service.ProcessAsaasEventAsync(received, Raw(received));

                applied.Should().BeTrue();

                int paymentCount = await dbContext.Set<Payment>()
                    .AsNoTracking()
                    .CountAsync(p => p.ExternalPaymentId == "pay_shared");
                paymentCount.Should().Be(1);

                Payment? payment = await ReadPaymentAsync(dbContext, "pay_shared");
                payment!.Status.Should().Be(PaymentStatus.Received);
                payment.PaidDate.Should().NotBeNull();

                Subscription refreshed = await dbContext.Set<Subscription>()
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == subscription.Id);
                refreshed.Status.Should().Be(SubscriptionStatus.Active);
            });
        }

        [Test]
        public async Task PaymentOverdue_marks_payment_overdue_and_subscription_pastdue()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "30000000000130", "overdue-pay@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Overdue Plan");
                Subscription subscription = await SeedTrialingSubscriptionAsync(dbContext, company.Id, plan.Id);

                AsaasWebhookPayload payload = BuildPayload("evt_overdue", "PAYMENT_OVERDUE", "pay_overdue");
                bool applied = await service.ProcessAsaasEventAsync(payload, Raw(payload));

                applied.Should().BeTrue();

                Payment? payment = await ReadPaymentAsync(dbContext, "pay_overdue");
                payment!.Status.Should().Be(PaymentStatus.Overdue);

                Subscription refreshed = await dbContext.Set<Subscription>()
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == subscription.Id);
                refreshed.Status.Should().Be(SubscriptionStatus.PastDue);
            });
        }

        [Test]
        public async Task Duplicate_event_id_does_not_double_process()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                Company company = await SeedCompanyAsync(dbContext, "40000000000140", "dup-pay@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "Dup Plan");
                await SeedTrialingSubscriptionAsync(dbContext, company.Id, plan.Id);

                AsaasWebhookPayload payload = BuildPayload("evt_dup_pay", "PAYMENT_RECEIVED", "pay_dup", paymentDate: "2026-06-04");
                bool first = await service.ProcessAsaasEventAsync(payload, Raw(payload));
                bool second = await service.ProcessAsaasEventAsync(payload, Raw(payload));

                first.Should().BeTrue();
                second.Should().BeFalse();

                int eventCount = await dbContext.Set<BillingWebhookEvent>()
                    .AsNoTracking()
                    .CountAsync(e => e.ExternalEventId == "evt_dup_pay");
                eventCount.Should().Be(1);
            });
        }

        [Test]
        public async Task Unknown_subscription_still_records_payment_without_company_and_notes_outcome()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();

                AsaasWebhookPayload payload = BuildPayload(
                    "evt_orphan",
                    "PAYMENT_RECEIVED",
                    "pay_orphan",
                    subscriptionId: "sub_does_not_exist",
                    paymentDate: "2026-06-04");

                bool applied = await service.ProcessAsaasEventAsync(payload, Raw(payload));

                applied.Should().BeFalse();

                Payment? payment = await ReadPaymentAsync(dbContext, "pay_orphan");
                payment.Should().NotBeNull();
                payment!.CompanyId.Should().BeNull();
                payment.SubscriptionId.Should().BeNull();
                payment.Status.Should().Be(PaymentStatus.Received);

                BillingWebhookEvent webhookEvent = await dbContext.Set<BillingWebhookEvent>()
                    .AsNoTracking()
                    .FirstAsync(e => e.ExternalEventId == "evt_orphan");
                webhookEvent.Outcome.Should().Contain("unknown_subscription");
            });
        }

        [Test]
        public async Task PaymentService_lists_payments_and_webhook_events()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                IBillingWebhookService service = sp.GetRequiredService<IBillingWebhookService>();
                IPaymentService paymentService = sp.GetRequiredService<IPaymentService>();

                Company company = await SeedCompanyAsync(dbContext, "50000000000150", "list-pay@example.com");
                Plan plan = await SeedPlanAsync(dbContext, "List Plan");
                await SeedTrialingSubscriptionAsync(dbContext, company.Id, plan.Id);

                AsaasWebhookPayload payload = BuildPayload("evt_list", "PAYMENT_CREATED", "pay_list");
                await service.ProcessAsaasEventAsync(payload, Raw(payload));

                IReadOnlyCollection<PaymentResponse> all = await paymentService.ListAsync();
                all.Should().ContainSingle(p => p.ExternalPaymentId == "pay_list");
                all.First(p => p.ExternalPaymentId == "pay_list").CompanyName.Should().Be("Empresa Pagamentos");

                IReadOnlyCollection<PaymentResponse> byCompany = await paymentService.GetByCompanyAsync(company.Id);
                byCompany.Should().ContainSingle(p => p.ExternalPaymentId == "pay_list");

                IReadOnlyCollection<PaymentResponse> byStatus = await paymentService.ListAsync(status: PaymentStatus.Pending);
                byStatus.Should().Contain(p => p.ExternalPaymentId == "pay_list");

                IReadOnlyCollection<WebhookEventResponse> events = await paymentService.ListWebhookEventsAsync();
                events.Should().Contain(e => e.ExternalEventId == "evt_list" && e.ExternalPaymentId == "pay_list");
            });
        }
    }
}
