using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class PlanServiceIntegrationTests : IntegrationTestBase
    {
        [Test]
        public async Task Create_then_GetById_returns_the_plan()
        {
            await InScopeAsync(async sp =>
            {
                IPlanService planService = sp.GetRequiredService<IPlanService>();

                CreatePlanRequest request = new CreatePlanRequest
                {
                    Name = "Starter",
                    PriceAmount = 99.90m,
                    BillingPeriod = BillingPeriod.Monthly,
                    Currency = "BRL",
                    TrialDays = 7,
                    Description = "Plano inicial para novos clientes"
                };

                PlanResponse created = await planService.CreateAsync(request);

                created.Id.Should().BeGreaterThan(0);
                created.Name.Should().Be("Starter");
                created.PriceAmount.Should().Be(99.90m);
                created.BillingPeriod.Should().Be(BillingPeriod.Monthly);
                created.Currency.Should().Be("BRL");
                created.TrialDays.Should().Be(7);
                created.Description.Should().Be("Plano inicial para novos clientes");
                created.IsActive.Should().BeTrue();

                PlanResponse fetched = await planService.GetByIdAsync(created.Id);

                fetched.Id.Should().Be(created.Id);
                fetched.Name.Should().Be("Starter");
                fetched.PriceAmount.Should().Be(99.90m);
                fetched.BillingPeriod.Should().Be(BillingPeriod.Monthly);
                fetched.TrialDays.Should().Be(7);
                fetched.IsActive.Should().BeTrue();
            });
        }

        [Test]
        public async Task Deactivate_removes_plan_from_GetActive()
        {
            await InScopeAsync(async sp =>
            {
                IPlanService planService = sp.GetRequiredService<IPlanService>();

                PlanResponse created = await planService.CreateAsync(new CreatePlanRequest
                {
                    Name = "Pro",
                    PriceAmount = 199.00m,
                    BillingPeriod = BillingPeriod.Monthly
                });

                IReadOnlyCollection<PlanResponse> beforeDeactivation = await planService.GetActiveAsync();
                beforeDeactivation.Should().Contain(p => p.Id == created.Id);

                await planService.DeactivateAsync(created.Id);

                IReadOnlyCollection<PlanResponse> afterDeactivation = await planService.GetActiveAsync();
                afterDeactivation.Should().NotContain(p => p.Id == created.Id);

                PlanResponse byId = await planService.GetByIdAsync(created.Id);
                byId.IsActive.Should().BeFalse();
            });
        }

        [Test]
        public async Task Update_changes_fields()
        {
            await InScopeAsync(async sp =>
            {
                IPlanService planService = sp.GetRequiredService<IPlanService>();

                PlanResponse created = await planService.CreateAsync(new CreatePlanRequest
                {
                    Name = "Basic",
                    PriceAmount = 49.00m,
                    BillingPeriod = BillingPeriod.Monthly,
                    TrialDays = 0
                });

                UpdatePlanRequest updateRequest = new UpdatePlanRequest
                {
                    Name = "Basic Plus",
                    PriceAmount = 79.00m,
                    BillingPeriod = BillingPeriod.Yearly,
                    Currency = "BRL",
                    TrialDays = 14,
                    Description = "Versao atualizada do plano basico"
                };

                PlanResponse updated = await planService.UpdateAsync(created.Id, updateRequest);

                updated.Name.Should().Be("Basic Plus");
                updated.PriceAmount.Should().Be(79.00m);
                updated.BillingPeriod.Should().Be(BillingPeriod.Yearly);
                updated.TrialDays.Should().Be(14);
                updated.Description.Should().Be("Versao atualizada do plano basico");

                PlanResponse persisted = await planService.GetByIdAsync(created.Id);
                persisted.Name.Should().Be("Basic Plus");
                persisted.PriceAmount.Should().Be(79.00m);
                persisted.BillingPeriod.Should().Be(BillingPeriod.Yearly);
                persisted.TrialDays.Should().Be(14);
            });
        }
    }
}
