using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class PendingAuthorizationSessionTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreatePendingAuthorizationSession()
        {
            PendingAuthorizationSession session = new(1, "token-value");

            Assert.That(session.UserId, Is.EqualTo(1));
            Assert.That(session.Token, Is.EqualTo("token-value"));
            Assert.That(session.IsUsed, Is.False);
            Assert.That(session.IsRevoked, Is.False);
            Assert.That(session.UsedAt, Is.Null);
            Assert.That(session.RevokedAt, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PendingAuthorizationSession(userId, "token-value"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidToken_ShouldThrowArgumentException(string token)
        {
            Assert.Throws<ArgumentException>(() => new PendingAuthorizationSession(1, token));
        }

        [Test]
        public void Constructor_ShouldTrimToken()
        {
            PendingAuthorizationSession session = new(1, "  token-value  ");

            Assert.That(session.Token, Is.EqualTo("token-value"));
        }

        [Test]
        public void IsValid_WhenNotUsedOrRevoked_ShouldReturnTrue()
        {
            PendingAuthorizationSession session = new(1, "token-value");

            Assert.That(session.IsValid(), Is.True);
        }

        [Test]
        public void MarkAsUsed_ShouldInvalidateSession()
        {
            PendingAuthorizationSession session = new(1, "token-value");
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            session.MarkAsUsed();

            Assert.That(session.IsUsed, Is.True);
            Assert.That(session.IsValid(), Is.False);
            Assert.That(session.UsedAt, Is.Not.Null);
            Assert.That(session.UsedAt.Value, Is.GreaterThan(before));
        }

        [Test]
        public void Revoke_ShouldInvalidateSession()
        {
            PendingAuthorizationSession session = new(1, "token-value");
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            session.Revoke();

            Assert.That(session.IsRevoked, Is.True);
            Assert.That(session.IsValid(), Is.False);
            Assert.That(session.RevokedAt, Is.Not.Null);
            Assert.That(session.RevokedAt.Value, Is.GreaterThan(before));
        }
    }
}
