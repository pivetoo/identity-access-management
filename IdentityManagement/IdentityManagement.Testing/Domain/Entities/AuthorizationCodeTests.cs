using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class AuthorizationCodeTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateAuthorizationCode()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);

            Assert.That(code.Code, Is.EqualTo("auth_code"));
            Assert.That(code.UserId, Is.EqualTo(1));
            Assert.That(code.Scopes, Is.EqualTo("read write"));
            Assert.That(code.RedirectUri, Is.EqualTo("https://app.com/callback"));
            Assert.That(code.SessionId, Is.EqualTo("session_id"));
            Assert.That(code.ContractId, Is.Null);
            Assert.That(code.IsUsed, Is.False);
            Assert.That(code.IsRevoked, Is.False);
            Assert.That(code.Success, Is.False);
            Assert.That(code.TokenExpiration, Is.Null);
            Assert.That(code.UsedAt, Is.Null);
        }

        [Test]
        public void Constructor_WithContractId_ShouldSetContractId()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10, 2);

            Assert.That(code.ContractId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithNullCode_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorizationCode(null!, 1, "read write", "https://app.com/callback", "session_id", 10));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidCode_ShouldThrowArgumentException(string code)
        {
            Assert.Throws<ArgumentException>(() => new AuthorizationCode(code, 1, "read write", "https://app.com/callback", "session_id", 10));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AuthorizationCode("auth_code", userId, "read write", "https://app.com/callback", "session_id", 10));
        }

        [Test]
        public void Constructor_WithNullScopes_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorizationCode("auth_code", 1, null!, "https://app.com/callback", "session_id", 10));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidScopes_ShouldThrowArgumentException(string scopes)
        {
            Assert.Throws<ArgumentException>(() => new AuthorizationCode("auth_code", 1, scopes, "https://app.com/callback", "session_id", 10));
        }

        [Test]
        public void Constructor_WithNullRedirectUri_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorizationCode("auth_code", 1, "read write", null!, "session_id", 10));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidRedirectUri_ShouldThrowArgumentException(string redirectUri)
        {
            Assert.Throws<ArgumentException>(() => new AuthorizationCode("auth_code", 1, "read write", redirectUri, "session_id", 10));
        }

        [Test]
        public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", null!, 10));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidSessionId_ShouldThrowArgumentException(string sessionId)
        {
            Assert.Throws<ArgumentException>(() => new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", sessionId, 10));
        }

        [Test]
        public void Constructor_ShouldSetExpiresAtBasedOnExpirationMinutes()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(9);
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);
            DateTimeOffset after = DateTimeOffset.UtcNow.AddMinutes(11);

            Assert.That(code.ExpiresAt, Is.GreaterThan(before));
            Assert.That(code.ExpiresAt, Is.LessThan(after));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            AuthorizationCode code = new AuthorizationCode("  auth_code  ", 1, "  read write  ", "  https://app.com/callback  ", "  session_id  ", 10);

            Assert.That(code.Code, Is.EqualTo("auth_code"));
            Assert.That(code.Scopes, Is.EqualTo("read write"));
            Assert.That(code.RedirectUri, Is.EqualTo("https://app.com/callback"));
            Assert.That(code.SessionId, Is.EqualTo("session_id"));
        }

        [Test]
        public void IsValid_WhenNotUsedNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);

            Assert.That(code.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenUsed_ShouldReturnFalse()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);
            code.MarkAsUsed(DateTimeOffset.UtcNow.AddHours(1));

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);
            code.Revoke();

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenExpired_ShouldReturnFalse()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", -1);

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void MarkAsUsed_ShouldSetIsUsedAndSuccessAndUsedAt()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);
            DateTimeOffset tokenExpiration = DateTimeOffset.UtcNow.AddHours(1);
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            code.MarkAsUsed(tokenExpiration);

            Assert.That(code.IsUsed, Is.True);
            Assert.That(code.Success, Is.True);
            Assert.That(code.UsedAt, Is.Not.Null);
            Assert.That(code.UsedAt.Value, Is.GreaterThan(before));
            Assert.That(code.UsedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
            Assert.That(code.TokenExpiration, Is.EqualTo(tokenExpiration));
        }

        [Test]
        public void Revoke_ShouldSetIsRevokedToTrue()
        {
            AuthorizationCode code = new AuthorizationCode("auth_code", 1, "read write", "https://app.com/callback", "session_id", 10);

            code.Revoke();

            Assert.That(code.IsRevoked, Is.True);
        }
    }
}
