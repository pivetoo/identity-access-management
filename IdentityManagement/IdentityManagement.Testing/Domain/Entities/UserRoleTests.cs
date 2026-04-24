using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class UserRoleTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateUserRole()
        {
            UserRole userRole = new UserRole(1, 2);

            Assert.That(userRole.UserId, Is.EqualTo(1));
            Assert.That(userRole.RoleId, Is.EqualTo(2));
            Assert.That(userRole.IsActive, Is.True);
            Assert.That(userRole.RevokedAt, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidUserId_ShouldThrowArgumentOutOfRangeException(long userId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UserRole(userId, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidRoleId_ShouldThrowArgumentOutOfRangeException(long roleId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UserRole(1, roleId));
        }

        [Test]
        public void IsValid_WhenActiveAndNotRevoked_ShouldReturnTrue()
        {
            UserRole userRole = new UserRole(1, 2);

            Assert.That(userRole.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenInactive_ShouldReturnFalse()
        {
            UserRole userRole = new UserRole(1, 2);
            userRole.Revoke();

            Assert.That(userRole.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            UserRole userRole = new UserRole(1, 2);
            userRole.Revoke();

            Assert.That(userRole.IsValid(), Is.False);
        }

        [Test]
        public void Revoke_ShouldSetIsActiveToFalseAndRevokedAt()
        {
            UserRole userRole = new UserRole(1, 2);
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            userRole.Revoke();

            Assert.That(userRole.IsActive, Is.False);
            Assert.That(userRole.RevokedAt, Is.Not.Null);
            Assert.That(userRole.RevokedAt.Value, Is.GreaterThan(before));
            Assert.That(userRole.RevokedAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        [Test]
        public void Reactivate_ShouldSetIsActiveToTrueAndRevokedAtToNull()
        {
            UserRole userRole = new UserRole(1, 2);
            userRole.Revoke();

            userRole.Reactivate();

            Assert.That(userRole.IsActive, Is.True);
            Assert.That(userRole.RevokedAt, Is.Null);
        }

        [Test]
        public void Reactivate_WhenAlreadyActive_ShouldRemainActive()
        {
            UserRole userRole = new UserRole(1, 2);

            userRole.Reactivate();

            Assert.That(userRole.IsActive, Is.True);
            Assert.That(userRole.RevokedAt, Is.Null);
        }
    }
}
