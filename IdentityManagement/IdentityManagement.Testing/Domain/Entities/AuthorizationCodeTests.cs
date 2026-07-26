using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.Security;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class AuthorizationCodeTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateAuthorizationCode()
        {
            AuthorizationCode code = CreateCode();

            Assert.That(code.Code, Is.EqualTo(TokenHasher.Hash("auth_code")));
            Assert.That(code.UserId, Is.EqualTo(1));
            Assert.That(code.ClientId, Is.EqualTo("client_id"));
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
            AuthorizationCode code = CreateCode(contractId: 2);

            Assert.That(code.ContractId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithNullCode_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateCode(code: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidCode_ShouldThrowArgumentException(string code)
        {
            Assert.Throws<ArgumentException>(() => CreateCode(code: code));
        }

        [Test]
        public void Constructor_WithNullClientId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateCode(clientId: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidClientId_ShouldThrowArgumentException(string clientId)
        {
            Assert.Throws<ArgumentException>(() => CreateCode(clientId: clientId));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateCode(userId: userId));
        }

        [Test]
        public void Constructor_WithNullScopes_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateCode(scopes: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidScopes_ShouldThrowArgumentException(string scopes)
        {
            Assert.Throws<ArgumentException>(() => CreateCode(scopes: scopes));
        }

        [Test]
        public void Constructor_WithNullRedirectUri_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateCode(redirectUri: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidRedirectUri_ShouldThrowArgumentException(string redirectUri)
        {
            Assert.Throws<ArgumentException>(() => CreateCode(redirectUri: redirectUri));
        }

        [Test]
        public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateCode(sessionId: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidSessionId_ShouldThrowArgumentException(string sessionId)
        {
            Assert.Throws<ArgumentException>(() => CreateCode(sessionId: sessionId));
        }

        [Test]
        public void Constructor_ShouldSetExpiresAtBasedOnExpirationMinutes()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(9);
            AuthorizationCode code = CreateCode();
            DateTimeOffset after = DateTimeOffset.UtcNow.AddMinutes(11);

            Assert.That(code.ExpiresAt, Is.GreaterThan(before));
            Assert.That(code.ExpiresAt, Is.LessThan(after));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            AuthorizationCode code = CreateCode(
                code: "  auth_code  ",
                clientId: "  client_id  ",
                scopes: "  read write  ",
                redirectUri: "  https://app.com/callback  ",
                sessionId: "  session_id  ");

            Assert.That(code.Code, Is.EqualTo(TokenHasher.Hash("auth_code")));
            Assert.That(code.ClientId, Is.EqualTo("client_id"));
            Assert.That(code.Scopes, Is.EqualTo("read write"));
            Assert.That(code.RedirectUri, Is.EqualTo("https://app.com/callback"));
            Assert.That(code.SessionId, Is.EqualTo("session_id"));
        }

        [Test]
        public void IsValid_WhenNotUsedNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            Assert.That(CreateCode().IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenUsed_ShouldReturnFalse()
        {
            AuthorizationCode code = CreateCode();
            code.MarkAsUsed(DateTimeOffset.UtcNow.AddHours(1));

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            AuthorizationCode code = CreateCode();
            code.Revoke();

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenExpired_ShouldReturnFalse()
        {
            AuthorizationCode code = CreateCode(expirationMinutes: -1);

            Assert.That(code.IsValid(), Is.False);
        }

        [Test]
        public void MarkAsUsed_ShouldSetIsUsedAndSuccessAndUsedAt()
        {
            AuthorizationCode code = CreateCode();
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
            AuthorizationCode code = CreateCode();

            code.Revoke();

            Assert.That(code.IsRevoked, Is.True);
        }

        private static AuthorizationCode CreateCode(
            string code = "auth_code",
            long userId = 1,
            string clientId = "client_id",
            string scopes = "read write",
            string redirectUri = "https://app.com/callback",
            string sessionId = "session_id",
            int expirationMinutes = 10,
            long? contractId = null)
        {
            return new AuthorizationCode(code, userId, clientId, scopes, redirectUri, sessionId, expirationMinutes, contractId);
        }
    }
}
