using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class PlanTests
    {
        [Test]
        public void EffectivePrice_uses_the_launch_price_only_inside_the_window()
        {
            Plan plan = new("Essencial Mensal", 997m, BillingPeriod.Monthly, trialDays: 14);
            DateTimeOffset until = new(2027, 1, 1, 2, 59, 59, TimeSpan.Zero);
            plan.SetLaunchPrice(497m, until);

            Assert.That(plan.EffectivePriceAt(until.AddDays(-30)), Is.EqualTo(497m), "dentro da janela vale a condicao de lancamento");
            Assert.That(plan.EffectivePriceAt(until), Is.EqualTo(497m), "o ultimo instante ainda vale");
            Assert.That(plan.EffectivePriceAt(until.AddSeconds(1)), Is.EqualTo(997m), "depois da janela vale a tabela");

            plan.SetLaunchPrice(null, null);
            Assert.That(plan.EffectivePriceAt(until.AddDays(-30)), Is.EqualTo(997m), "sem condicao vale sempre a tabela");
        }

        [Test]
        public void Launch_price_cannot_exceed_the_list_price()
        {
            Plan plan = new("Essencial Mensal", 997m, BillingPeriod.Monthly);
            Assert.Throws<ArgumentOutOfRangeException>(() => plan.SetLaunchPrice(1200m, DateTimeOffset.UtcNow.AddMonths(1)));
        }
    }
}
