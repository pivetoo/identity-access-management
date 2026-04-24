using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class RefreshTokenTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateRefreshToken()
        {
            RefreshToken token = new RefreshToken("token_string", 1, "session_id", "read write", 30);

            Assert.That(token.Token, Is.EqualTo("token_string"));
            Assert.That(token.UserId, Is.EqualTo(1));
            Assert.That(token.SessionId, Is.EqualTo("session_id"));
            Assert.That(token.Scopes, Is.EqualTo("read write"));
            Assert.That(token.ContractId, Is.Null);
            Assert.That(token.IsRevoked, Is.False);
            Assert.That(token.RevokedAt, Is.Null);
            Assert.That(token.LastUsedAt, Is.Null);
        }

        [Test]
        public void Constructor_WithContractId_ShouldSetContractId()
        {
            RefreshToken token = new RefreshToken("token_string", 1, "session_id", "read write", 30, 2);

            Assert.That(token.ContractId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithNullToken_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RefreshToken(null!, 1, "session_id", "read write", 30));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidToken_ShouldThrowArgumentException(string token)
        {
            Assert.Throws<ArgumentException>(() => new RefreshToken(token, 1, "session_id", "read write", 30));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RefreshToken("token", userId, "session_id", "read write", 30));
        }

        [Test]
        public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RefreshToken("token", 1, null!, "read write", 30));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidSessionId_ShouldThrowArgumentException(string sessionId)
        {
            Assert.Throws<ArgumentException>(() => new RefreshToken("token", 1, sessionId, "read write", 30));
        }

        [Test]
        public void Constructor_ShouldSetExpiresAtBasedOnExpirationDays()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow;
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", 30);
            DateTimeOffset after = DateTimeOffset.UtcNow;

            Assert.That(token.ExpiresAt, Is.GreaterThan(before.AddDays(29)));
            Assert.That(token.ExpiresAt, Is.LessThan(after.AddDays(31)));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            RefreshToken token = new RefreshToken("  token_string  ", 1, "  session_id  ", "  read write  ", 30);

            Assert.That(token.Token, Is.EqualTo("token_string"));
            Assert.That(token.SessionId, Is.EqualTo("session_id"));
            Assert.That(token.Scopes, Is.EqualTo("read write"));
        }

        [Test]
        public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", 30);

            Assert.That(token.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", 30);
            token.Revoke();

            Assert.That(token.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenExpired_ShouldReturnFalse()
        {
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", -1);

            Assert.That(token.IsValid(), Is.False);
        }

        [Test]
        public void MarkAsUsed_ShouldSetLastUsedAt()
        {
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", 30);
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            token.MarkAsUsed();

            Assert.That(token.LastUsedAt, Is.Not.Null);
            Assert.That(token.LastUsedAt.Value, Is.GreaterThan(before));
            Assert.That(token.LastUsedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        [Test]
        public void Revoke_ShouldSetIsRevokedAndRevokedAt()
        {
            RefreshToken token = new RefreshToken("token", 1, "session_id", "read write", 30);
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            token.Revoke();

            Assert.That(token.IsRevoked, Is.True);
            Assert.That(token.RevokedAt, Is.Not.Null);
            Assert.That(token.RevokedAt.Value, Is.GreaterThan(before));
            Assert.That(token.RevokedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }
    }
}
