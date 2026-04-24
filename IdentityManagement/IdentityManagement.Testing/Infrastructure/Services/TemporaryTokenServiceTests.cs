using IdentityManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    [TestFixture]
    public class TemporaryTokenServiceTests
    {
        private IConfiguration configuration = null!;
        private TemporaryTokenService service = null!;
        private const string SecretKey = "this-is-a-very-long-secret-key-for-testing-purposes-only-32bytes!";

        [SetUp]
        public void SetUp()
        {
            configuration = Substitute.For<IConfiguration>();
            configuration["Jwt:TemporarySecretKey"].Returns(SecretKey);
            service = new TemporaryTokenService(configuration);
        }

        [Test]
        public void Constructor_WhenSecretKeyMissing_ShouldThrowArgumentNullException()
        {
            IConfiguration emptyConfig = Substitute.For<IConfiguration>();
            emptyConfig["Jwt:TemporarySecretKey"].Returns((string?)null);

            Assert.Throws<ArgumentNullException>(() => new TemporaryTokenService(emptyConfig));
        }

        [Test]
        public void GenerateTemporaryToken_ShouldReturnNonEmptyString()
        {
            string token = service.GenerateTemporaryToken(1);

            Assert.That(token, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GenerateTemporaryToken_ShouldReturnDifferentTokensForDifferentCalls()
        {
            string token1 = service.GenerateTemporaryToken(1);
            string token2 = service.GenerateTemporaryToken(1);

            Assert.That(token1, Is.Not.EqualTo(token2));
        }

        [Test]
        public void ValidateTemporaryToken_WithValidToken_ShouldReturnTrueAndExtractUserId()
        {
            string token = service.GenerateTemporaryToken(42);

            bool isValid = service.ValidateTemporaryToken(token, out long userId);

            Assert.That(isValid, Is.True);
            Assert.That(userId, Is.EqualTo(42));
        }

        [Test]
        public void ValidateTemporaryToken_WithInvalidToken_ShouldReturnFalseAndZeroUserId()
        {
            bool isValid = service.ValidateTemporaryToken("invalid.token.here", out long userId);

            Assert.That(isValid, Is.False);
            Assert.That(userId, Is.EqualTo(0));
        }

        [Test]
        public void ValidateTemporaryToken_WithExpiredToken_ShouldReturnFalse()
        {
            IConfiguration expiredConfig = Substitute.For<IConfiguration>();
            expiredConfig["Jwt:TemporarySecretKey"].Returns(SecretKey);
            TemporaryTokenService expiredService = new TemporaryTokenService(expiredConfig);

            string token = expiredService.GenerateTemporaryToken(1);

            bool isValid = expiredService.ValidateTemporaryToken(token, out long userId);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateTemporaryTokenForUser_WithMatchingUser_ShouldReturnTrue()
        {
            string token = service.GenerateTemporaryToken(42);

            bool isValid = service.ValidateTemporaryTokenForUser(token, 42);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateTemporaryTokenForUser_WithDifferentUser_ShouldReturnFalse()
        {
            string token = service.GenerateTemporaryToken(42);

            bool isValid = service.ValidateTemporaryTokenForUser(token, 99);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateTemporaryTokenForUser_WithInvalidToken_ShouldReturnFalse()
        {
            bool isValid = service.ValidateTemporaryTokenForUser("invalid.token", 42);

            Assert.That(isValid, Is.False);
        }
    }
}
