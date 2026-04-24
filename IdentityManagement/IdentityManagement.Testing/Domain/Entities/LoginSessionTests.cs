using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class LoginSessionTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateLoginSession()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");

            Assert.That(session.UserId, Is.EqualTo(1));
            Assert.That(session.ContractId, Is.EqualTo(1));
            Assert.That(session.IpAddress, Is.EqualTo("192.168.1.1"));
            Assert.That(session.UserAgent, Is.EqualTo("Mozilla/5.0"));
            Assert.That(session.IsActive, Is.True);
            Assert.That(session.RevokedAt, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LoginSession(userId, 1, "192.168.1.1", "Mozilla/5.0"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidContractId_ShouldThrowArgumentOutOfRangeException(long contractId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LoginSession(1, contractId, "192.168.1.1", "Mozilla/5.0"));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            LoginSession session = new LoginSession(1, 1, "  192.168.1.1  ", "  Mozilla/5.0  ");

            Assert.That(session.IpAddress, Is.EqualTo("192.168.1.1"));
            Assert.That(session.UserAgent, Is.EqualTo("Mozilla/5.0"));
        }

        [Test]
        public void Constructor_ShouldSetDefaultExpirationTo24Hours()
        {
            DateTimeOffset before = DateTimeOffset.UtcNow.AddHours(23);
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");
            DateTimeOffset after = DateTimeOffset.UtcNow.AddHours(25);

            Assert.That(session.ExpiresAt, Is.GreaterThan(before));
            Assert.That(session.ExpiresAt, Is.LessThan(after));
        }

        [Test]
        public void SetExpiration_ShouldUpdateExpiresAt()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");
            DateTimeOffset newExpiration = DateTimeOffset.UtcNow.AddHours(12);

            session.SetExpiration(newExpiration);

            Assert.That(session.ExpiresAt, Is.EqualTo(newExpiration));
        }

        [Test]
        public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");

            Assert.That(session.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenInactive_ShouldReturnFalse()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");
            session.Revoke();

            Assert.That(session.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenExpired_ShouldReturnFalse()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");
            session.SetExpiration(DateTimeOffset.UtcNow.AddHours(-1));

            Assert.That(session.IsValid(), Is.False);
        }

        [Test]
        public void Revoke_ShouldSetIsActiveToFalseAndRevokedAt()
        {
            LoginSession session = new LoginSession(1, 1, "192.168.1.1", "Mozilla/5.0");
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            session.Revoke();

            Assert.That(session.IsActive, Is.False);
            Assert.That(session.RevokedAt, Is.Not.Null);
            Assert.That(session.RevokedAt.Value, Is.GreaterThan(before));
            Assert.That(session.RevokedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }
    }
}
