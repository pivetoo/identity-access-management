using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class RefreshTokenTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateRefreshToken()
        {
            RefreshToken token = CreateToken();

            Assert.That(token.Token, Is.EqualTo("token_string"));
            Assert.That(token.UserId, Is.EqualTo(1));
            Assert.That(token.SessionId, Is.EqualTo("session_id"));
            Assert.That(token.Scopes, Is.EqualTo("read write"));
            Assert.That(token.ClientId, Is.EqualTo("client_id"));
            Assert.That(token.ContractId, Is.Null);
            Assert.That(token.IsRevoked, Is.False);
            Assert.That(token.RevokedAt, Is.Null);
            Assert.That(token.LastUsedAt, Is.Null);
        }

        [Test]
        public void Constructor_WithContractId_ShouldSetContractId()
        {
            RefreshToken token = CreateToken(contractId: 2);

            Assert.That(token.ContractId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithNullToken_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateToken(token: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidToken_ShouldThrowArgumentException(string token)
        {
            Assert.Throws<ArgumentException>(() => CreateToken(token: token));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateToken(userId: userId));
        }

        [Test]
        public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateToken(sessionId: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidSessionId_ShouldThrowArgumentException(string sessionId)
        {
            Assert.Throws<ArgumentException>(() => CreateToken(sessionId: sessionId));
        }

        [Test]
        public void Constructor_WithNullClientId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateToken(clientId: null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidClientId_ShouldThrowArgumentException(string clientId)
        {
            Assert.Throws<ArgumentException>(() => CreateToken(clientId: clientId));
        }

        [Test]
        public void Constructor_ShouldSetExpiresAtBasedOnExpirationDays()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow;
            RefreshToken token = CreateToken();
            DateTimeOffset after = DateTimeOffset.UtcNow;

            Assert.That(token.ExpiresAt, Is.GreaterThan(before.AddDays(29)));
            Assert.That(token.ExpiresAt, Is.LessThan(after.AddDays(31)));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            RefreshToken token = CreateToken(
                token: "  token_string  ",
                sessionId: "  session_id  ",
                scopes: "  read write  ",
                clientId: "  client_id  ");

            Assert.That(token.Token, Is.EqualTo("token_string"));
            Assert.That(token.SessionId, Is.EqualTo("session_id"));
            Assert.That(token.Scopes, Is.EqualTo("read write"));
            Assert.That(token.ClientId, Is.EqualTo("client_id"));
        }

        [Test]
        public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            Assert.That(CreateToken().IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            RefreshToken token = CreateToken();
            token.Revoke();

            Assert.That(token.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenExpired_ShouldReturnFalse()
        {
            RefreshToken token = CreateToken(expirationDays: -1);

            Assert.That(token.IsValid(), Is.False);
        }

        [Test]
        public void MarkAsUsed_ShouldSetLastUsedAt()
        {
            RefreshToken token = CreateToken();
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            token.MarkAsUsed();

            Assert.That(token.LastUsedAt, Is.Not.Null);
            Assert.That(token.LastUsedAt.Value, Is.GreaterThan(before));
            Assert.That(token.LastUsedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        [Test]
        public void Revoke_ShouldSetIsRevokedAndRevokedAt()
        {
            RefreshToken token = CreateToken();
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            token.Revoke();

            Assert.That(token.IsRevoked, Is.True);
            Assert.That(token.RevokedAt, Is.Not.Null);
            Assert.That(token.RevokedAt.Value, Is.GreaterThan(before));
            Assert.That(token.RevokedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        private static RefreshToken CreateToken(
            string token = "token_string",
            long userId = 1,
            string sessionId = "session_id",
            string scopes = "read write",
            int expirationDays = 30,
            long? contractId = null,
            string clientId = "client_id")
        {
            return new RefreshToken(token, userId, sessionId, scopes, expirationDays, contractId, clientId);
        }
    }
}
