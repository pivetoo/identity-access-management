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

            public Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default) => Task.CompletedTask;

            public Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, string? externalCustomerId, CancellationToken ct = default)
                => Task.FromResult<GatewaySubscriptionResult?>(null);

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
