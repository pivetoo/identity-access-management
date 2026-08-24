using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityManagement.IntegrationTests.Services
{
    /// <summary>
    /// Migracao de PIX para cartao pelo checkout hospedado.
    ///
    /// O que estes casos protegem e a cobranca dupla: o checkout cria uma assinatura NOVA no
    /// provedor, e se a antiga (PIX) continuar ativa a empresa paga duas vezes por ciclo.
    /// </summary>
    [TestFixture]
    public sealed class BillingCheckoutWebhookIntegrationTests : IntegrationTestBase
    {
        private const string PixSubscriptionId = "sub_pix";
        private const string CardSubscriptionId = "sub_card";
        private const string CustomerId = "cus_checkout";

        private sealed class FakeGateway : IBillingGateway
        {
            private readonly List<GatewaySubscriptionSummary> cardSubscriptions;

            public FakeGateway(params GatewaySubscriptionSummary[] cardSubscriptions)
            {
                this.cardSubscriptions = [.. cardSubscriptions];
            }

            public List<string> CanceledSubscriptions { get; } = [];

            public Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default) => Task.FromResult<string?>(CustomerId);

            public Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
            {
                CanceledSubscriptions.Add(externalSubscriptionId);
                return Task.CompletedTask;
            }

            public Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, decimal priceAmount, string? externalCustomerId, CancellationToken ct = default)
                => Task.FromResult<GatewaySubscriptionResult?>(new GatewaySubscriptionResult(PixSubscriptionId, CustomerId, "PIX"));

            public Task<GatewayCheckoutResult> CreateRecurringCardCheckoutAsync(long companyId, long planId, decimal priceAmount, CancellationToken ct = default)
                => Task.FromResult(new GatewayCheckoutResult("chk_1", "https://provedor.example/checkout/chk_1", DateTimeOffset.UtcNow.AddHours(1)));

            public Task<IReadOnlyList<GatewaySubscriptionSummary>> ListActiveCardSubscriptionsAsync(string externalCustomerId, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<GatewaySubscriptionSummary>>(cardSubscriptions);

            public Task<GatewayPendingCharge?> GetPendingChargeAsync(string externalSubscriptionId, CancellationToken ct = default)
                => Task.FromResult(PendingCharge);

            public GatewayPendingCharge? PendingCharge { get; set; }

        }

        private static async Task<Subscription> SeedPixSubscriptionAsync(DbContext dbContext, string document, string email)
        {
            Company company = new Company("Empresa Checkout LTDA", "Empresa Checkout", document, email, "11999990001");
            await dbContext.Set<Company>().AddAsync(company);

            Plan plan = new Plan("Essencial Checkout", 497m, BillingPeriod.Monthly, "BRL", 14, null);
            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            Subscription subscription = Subscription.StartTrialing(company.Id, plan.Id, DateTimeOffset.UtcNow, 14);
            subscription.LinkGateway("asaas", CustomerId, PixSubscriptionId);
            subscription.SetPaymentMethod("PIX");

            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();

            return subscription;
        }

        private static AsaasWebhookPayload CheckoutPaid(string eventId, long companyId) => new()
        {
            Id = eventId,
            Event = "CHECKOUT_PAID",
            Checkout = new AsaasCheckoutInfo
            {
                Id = "chk_1",
                Status = "PAID",
                Customer = CustomerId,
                ExternalReference = $"company:{companyId}"
            }
        };

        [Test]
        public async Task Checkout_pago_troca_a_assinatura_e_cancela_a_de_pix()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Subscription subscription = await SeedPixSubscriptionAsync(dbContext, "11444777000161", "checkout1@example.com");

                FakeGateway gateway = new(new GatewaySubscriptionSummary(CardSubscriptionId, "CREDIT_CARD", "ACTIVE", DateTimeOffset.UtcNow));
                BillingWebhookService service = new(dbContext, gateway, NullLogger<BillingWebhookService>.Instance);

                bool applied = await service.ProcessAsaasEventAsync(CheckoutPaid("evt_1", subscription.CompanyId));

                applied.Should().BeTrue();

                Subscription reloaded = await dbContext.Set<Subscription>().AsNoTracking().FirstAsync(item => item.Id == subscription.Id);
                reloaded.ExternalSubscriptionId.Should().Be(CardSubscriptionId);
                reloaded.PaymentMethod.Should().Be("CREDIT_CARD");

                gateway.CanceledSubscriptions.Should().ContainSingle().Which.Should().Be(PixSubscriptionId);
            });
        }

        [Test]
        public async Task Checkout_pago_sem_assinatura_de_cartao_visivel_falha_para_o_provedor_reenviar()
        {
            // Engolir o evento aqui deixaria a empresa com cartao cobrando E o PIX ativo.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Subscription subscription = await SeedPixSubscriptionAsync(dbContext, "11222333000181", "checkout2@example.com");

                FakeGateway gateway = new();
                BillingWebhookService service = new(dbContext, gateway, NullLogger<BillingWebhookService>.Instance);

                Func<Task> act = () => service.ProcessAsaasEventAsync(CheckoutPaid("evt_2", subscription.CompanyId));

                await act.Should().ThrowAsync<InvalidOperationException>();

                Subscription reloaded = await dbContext.Set<Subscription>().AsNoTracking().FirstAsync(item => item.Id == subscription.Id);
                reloaded.ExternalSubscriptionId.Should().Be(PixSubscriptionId);
                reloaded.PaymentMethod.Should().Be("PIX");
                gateway.CanceledSubscriptions.Should().BeEmpty();

                bool recorded = await dbContext.Set<BillingWebhookEvent>().AsNoTracking().AnyAsync(item => item.ExternalEventId == "evt_2");
                recorded.Should().BeFalse("sem registro o provedor reenvia o evento");
            });
        }

        [Test]
        public async Task Checkout_pago_ignora_a_propria_assinatura_ja_vinculada()
        {
            // O provedor pode reenviar depois da troca. Reprocessar nao pode cancelar o cartao novo.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Subscription subscription = await SeedPixSubscriptionAsync(dbContext, "11333444000172", "checkout3@example.com");

                FakeGateway gateway = new(new GatewaySubscriptionSummary(PixSubscriptionId, "CREDIT_CARD", "ACTIVE", DateTimeOffset.UtcNow));
                BillingWebhookService service = new(dbContext, gateway, NullLogger<BillingWebhookService>.Instance);

                Func<Task> act = () => service.ProcessAsaasEventAsync(CheckoutPaid("evt_3", subscription.CompanyId));

                await act.Should().ThrowAsync<InvalidOperationException>();
                gateway.CanceledSubscriptions.Should().BeEmpty();
            });
        }

        [Test]
        public async Task Eventos_de_checkout_que_nao_sao_pagamento_sao_ignorados()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Subscription subscription = await SeedPixSubscriptionAsync(dbContext, "11555666000163", "checkout4@example.com");

                FakeGateway gateway = new();
                BillingWebhookService service = new(dbContext, gateway, NullLogger<BillingWebhookService>.Instance);

                AsaasWebhookPayload payload = CheckoutPaid("evt_4", subscription.CompanyId);
                payload.Event = "CHECKOUT_EXPIRED";

                bool applied = await service.ProcessAsaasEventAsync(payload);

                applied.Should().BeFalse();

                Subscription reloaded = await dbContext.Set<Subscription>().AsNoTracking().FirstAsync(item => item.Id == subscription.Id);
                reloaded.PaymentMethod.Should().Be("PIX");
                gateway.CanceledSubscriptions.Should().BeEmpty();
            });
        }
    }
}
