using IdentityManagement.Infrastructure.Services;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    [TestFixture]
    public class ResolveGeneratedSecretTests
    {
        [Test]
        public void SameKey_ReusesGeneratedValue_AndGeneratesOnce()
        {
            Dictionary<string, string> cache = new();
            int calls = 0;
            string Generator()
            {
                calls++;
                return $"secret-{calls}";
            }

            string first = ClientOnboardingService.ResolveGeneratedSecret(cache, "CallbackSecret", Generator);
            string second = ClientOnboardingService.ResolveGeneratedSecret(cache, "CallbackSecret", Generator);

            Assert.That(first, Is.EqualTo("secret-1"));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DifferentKeys_GenerateDistinctValues()
        {
            Dictionary<string, string> cache = new();
            int calls = 0;
            string Generator()
            {
                calls++;
                return $"secret-{calls}";
            }

            string a = ClientOnboardingService.ResolveGeneratedSecret(cache, "CallbackSecret", Generator);
            string b = ClientOnboardingService.ResolveGeneratedSecret(cache, "OtherSecret", Generator);

            Assert.That(a, Is.EqualTo("secret-1"));
            Assert.That(b, Is.EqualTo("secret-2"));
            Assert.That(calls, Is.EqualTo(2));
        }
    }
}
