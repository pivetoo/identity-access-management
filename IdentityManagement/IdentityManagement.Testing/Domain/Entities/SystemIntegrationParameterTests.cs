using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class SystemIntegrationParameterTests
    {
        [Test]
        public void Constructor_WithGeneratedSecret_ShouldStoreNullValueAndNoSourceAudience()
        {
            SystemIntegrationParameter parameter = new SystemIntegrationParameter(1, "CallbackSecret", null, true, SystemIntegrationParameterSource.GeneratedSecret, null);

            Assert.That(parameter.ValueSource, Is.EqualTo(SystemIntegrationParameterSource.GeneratedSecret));
            Assert.That(parameter.Value, Is.Null);
            Assert.That(parameter.SourceAudience, Is.Null);
            Assert.That(parameter.IsSecret, Is.True);
            Assert.That(parameter.Key, Is.EqualTo("CallbackSecret"));
        }

        [Test]
        public void Constructor_WithStaticParameter_ShouldStoreValue()
        {
            SystemIntegrationParameter parameter = new SystemIntegrationParameter(1, "Mode", "production", false, SystemIntegrationParameterSource.Static, null);

            Assert.That(parameter.Value, Is.EqualTo("production"));
            Assert.That(parameter.SourceAudience, Is.Null);
        }

        [Test]
        public void Constructor_WithStaticAndNullValue_ShouldThrow()
        {
            Assert.That(() => new SystemIntegrationParameter(1, "Mode", null, false, SystemIntegrationParameterSource.Static, null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_WithTenantApiKeyAndNoSourceAudience_ShouldThrow()
        {
            Assert.That(() => new SystemIntegrationParameter(1, "ApiKey", null, true, SystemIntegrationParameterSource.TenantApiKey, null), Throws.TypeOf<ArgumentException>());
        }
    }
}
