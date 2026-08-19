using IdentityManagement.Infrastructure.Services;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    [TestFixture]
    public class BootstrapSkipReasonTests
    {
        [Test]
        public void BaseUrlEmpty_SkipsBootstrap()
        {
            Assert.That(ClientOnboardingService.ResolveBootstrapSkipReason(string.Empty, true), Is.EqualTo("baseUrl.notConfigured"));
            Assert.That(ClientOnboardingService.ResolveBootstrapSkipReason("   ", true), Is.EqualTo("baseUrl.notConfigured"));
            Assert.That(ClientOnboardingService.ResolveBootstrapSkipReason(null, true), Is.EqualTo("baseUrl.notConfigured"));
        }

        [Test]
        public void ApiKeyNotProvisioned_SkipsBootstrap()
        {
            Assert.That(
                ClientOnboardingService.ResolveBootstrapSkipReason("https://integrations.example.com", false),
                Is.EqualTo("apiKey.notProvisioned"));
        }

        // Regressao: sem integracao no blueprint o bootstrap AINDA precisa rodar, porque e ele que
        // executa a migration do banco do tenant. Pular deixava o tenant com banco vazio.
        [Test]
        public void BaseUrlAndApiKeyPresent_RunsBootstrapEvenWithoutBlueprint()
        {
            Assert.That(ClientOnboardingService.ResolveBootstrapSkipReason("https://integrations.example.com", true), Is.Null);
        }
    }
}
