using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
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
    /// Cobranca na visao do tenant. O caso do endereco existe porque o DbContext do Archon e
    /// NoTracking por padrao: sem AsTracking o salvamento vira no-op silencioso — a tela mostra
    /// sucesso e o banco nao muda.
    /// </summary>
    [TestFixture]
    public sealed class TenantBillingServiceIntegrationTests : IntegrationTestBase
    {
        private sealed class StubGateway : IBillingGateway
        {
            public Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default) => Task.FromResult<string?>("cus_1");

            public Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
            {
                Canceled.Add(externalSubscriptionId);
                return Task.CompletedTask;
            }

            public Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, string? externalCustomerId, CancellationToken ct = default)
                => Task.FromResult(CreatedSubscription);

            public GatewaySubscriptionResult? CreatedSubscription { get; set; } = new GatewaySubscriptionResult("sub_pix_novo", "cus_1", "PIX");

            public List<string> Canceled { get; } = [];

            public Task<GatewayCheckoutResult> CreateRecurringCardCheckoutAsync(long companyId, long planId, CancellationToken ct = default)
                => Task.FromResult(new GatewayCheckoutResult("chk_1", "https://provedor.example/chk_1", DateTimeOffset.UtcNow.AddHours(1)));

            public Task<IReadOnlyList<GatewaySubscriptionSummary>> ListActiveCardSubscriptionsAsync(string externalCustomerId, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<GatewaySubscriptionSummary>>([]);

            public Task<GatewayPendingCharge?> GetPendingChargeAsync(string externalSubscriptionId, CancellationToken ct = default)
                => Task.FromResult(PendingCharge);

            public GatewayPendingCharge? PendingCharge { get; set; }

        }

        private static UpdateBillingAddressRequest ValidAddress() => new()
        {
            PostalCode = "01310100",
            Street = "Avenida Paulista",
            Number = "1000",
            District = "Bela Vista",
            City = "Sao Paulo",
            State = "SP"
        };

        private static async Task<Company> SeedAsync(DbContext dbContext, string document, string email)
        {
            Company company = new("Empresa Billing LTDA", "Empresa Billing", document, email, "11999990001");
            await dbContext.Set<Company>().AddAsync(company);

            Plan plan = new("Essencial Billing", 497m, BillingPeriod.Monthly, "BRL", 14, null);
            await dbContext.Set<Plan>().AddAsync(plan);
            await dbContext.SaveChangesAsync();

            Subscription subscription = Subscription.StartTrialing(company.Id, plan.Id, DateTimeOffset.UtcNow, 14);
            subscription.LinkGateway("asaas", "cus_1", "sub_pix");
            subscription.SetPaymentMethod("PIX");
            await dbContext.Set<Subscription>().AddAsync(subscription);
            await dbContext.SaveChangesAsync();

            return company;
        }

        private static TenantBillingService CreateSubject(DbContext dbContext) =>
            new(dbContext, new StubGateway(), NullLogger<TenantBillingService>.Instance);

        [Test]
        public async Task Endereco_de_cobranca_e_realmente_persistido()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11444777000161", "billing1@example.com");

                TenantBillingService subject = CreateSubject(dbContext);
                TenantSubscriptionResponse response = await subject.UpdateBillingAddressAsync(company.TenantId, ValidAddress());

                response.HasBillingAddress.Should().BeTrue();

                Company reloaded = await dbContext.Set<Company>().AsNoTracking().FirstAsync(item => item.Id == company.Id);
                reloaded.BillingPostalCode.Should().Be("01310100");
                reloaded.BillingCity.Should().Be("Sao Paulo");
                reloaded.BillingState.Should().Be("SP");
            });
        }

        [Test]
        public async Task Teste_vencido_aparece_como_bloqueado_mesmo_sem_a_flag()
        {
            // O gate de acesso recusa trial vencido sem ligar `IsBlocked`. Se a tela olhasse so a
            // flag, o cliente veria "tudo certo" com o sistema inteiro respondendo 402.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11777888000145", "billing6@example.com");

                Subscription subscription = await dbContext.Set<Subscription>()
                    .AsTracking()
                    .FirstAsync(item => item.CompanyId == company.Id);

                typeof(Subscription)
                    .GetProperty(nameof(Subscription.TrialEndsAt))!
                    .SetValue(subscription, DateTimeOffset.UtcNow.AddDays(-1));

                await dbContext.SaveChangesAsync();

                TenantBillingService subject = CreateSubject(dbContext);
                TenantSubscriptionResponse response = await subject.GetSubscriptionAsync(company.TenantId);

                response.Status.Should().Be(nameof(SubscriptionStatus.Trialing));
                response.IsBlocked.Should().BeTrue();
            });
        }

        [Test]
        public async Task Endereco_incompleto_e_recusado()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11222333000181", "billing2@example.com");

                TenantBillingService subject = CreateSubject(dbContext);
                UpdateBillingAddressRequest request = ValidAddress();
                request.PostalCode = "123";

                Func<Task> act = () => subject.UpdateBillingAddressAsync(company.TenantId, request);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Checkout_de_cartao_exige_endereco_antes()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11333444000172", "billing3@example.com");

                TenantBillingService subject = CreateSubject(dbContext);

                Func<Task> act = () => subject.StartCardCheckoutAsync(company.TenantId);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Checkout_de_cartao_devolve_a_url_do_provedor_com_endereco_salvo()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11555666000163", "billing4@example.com");

                TenantBillingService subject = CreateSubject(dbContext);
                await subject.UpdateBillingAddressAsync(company.TenantId, ValidAddress());

                TenantCheckoutResponse checkout = await subject.StartCardCheckoutAsync(company.TenantId);

                checkout.CheckoutUrl.Should().Be("https://provedor.example/chk_1");
            });
        }

        [Test]
        public async Task Voltar_para_pix_troca_a_assinatura_e_cancela_a_de_cartao()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11888999000136", "billing7@example.com");

                Subscription tracked = await dbContext.Set<Subscription>().AsTracking().FirstAsync(item => item.CompanyId == company.Id);
                tracked.LinkGateway("asaas", "cus_1", "sub_cartao");
                tracked.SetPaymentMethod("CREDIT_CARD");
                await dbContext.SaveChangesAsync();

                StubGateway gateway = new();
                TenantBillingService subject = new(dbContext, gateway, NullLogger<TenantBillingService>.Instance);

                TenantSubscriptionResponse response = await subject.SwitchToPixAsync(company.TenantId);

                response.PaymentMethod.Should().Be("PIX");
                gateway.Canceled.Should().ContainSingle().Which.Should().Be("sub_cartao");

                Subscription reloaded = await dbContext.Set<Subscription>().AsNoTracking().FirstAsync(item => item.CompanyId == company.Id);
                reloaded.ExternalSubscriptionId.Should().Be("sub_pix_novo");
            });
        }

        [Test]
        public async Task Voltar_para_pix_e_recusado_quando_ja_esta_em_pix()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11999000000127", "billing8@example.com");

                StubGateway gateway = new();
                TenantBillingService subject = new(dbContext, gateway, NullLogger<TenantBillingService>.Instance);

                Func<Task> act = () => subject.SwitchToPixAsync(company.TenantId);

                await act.Should().ThrowAsync<BusinessRuleException>();
                gateway.Canceled.Should().BeEmpty();
            });
        }

        [Test]
        public async Task Voltar_para_pix_nao_cancela_o_cartao_se_a_nova_nao_for_criada()
        {
            // Cancelar antes de ter substituta deixaria o tenant sem forma de pagamento nenhuma.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Company company = await SeedAsync(dbContext, "11000111000118", "billing9@example.com");

                Subscription tracked = await dbContext.Set<Subscription>().AsTracking().FirstAsync(item => item.CompanyId == company.Id);
                tracked.LinkGateway("asaas", "cus_1", "sub_cartao");
                tracked.SetPaymentMethod("CREDIT_CARD");
                await dbContext.SaveChangesAsync();

                StubGateway gateway = new() { CreatedSubscription = null };
                TenantBillingService subject = new(dbContext, gateway, NullLogger<TenantBillingService>.Instance);

                Func<Task> act = () => subject.SwitchToPixAsync(company.TenantId);

                await act.Should().ThrowAsync<BusinessRuleException>();
                gateway.Canceled.Should().BeEmpty();

                Subscription reloaded = await dbContext.Set<Subscription>().AsNoTracking().FirstAsync(item => item.CompanyId == company.Id);
                reloaded.PaymentMethod.Should().Be("CREDIT_CARD");
            });
        }

        [Test]
        public async Task Tenant_desconhecido_nao_devolve_assinatura_de_ninguem()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedAsync(dbContext, "11666777000154", "billing5@example.com");

                TenantBillingService subject = CreateSubject(dbContext);

                Func<Task> act = () => subject.GetSubscriptionAsync(Guid.NewGuid());

                await act.Should().ThrowAsync<NotFoundException>();
            });
        }
    }
}
