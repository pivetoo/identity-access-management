using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class RoleAccessResourceTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateRoleAccessResource()
        {
            RoleAccessResource resource = new RoleAccessResource(1, 2);

            Assert.That(resource.RoleId, Is.EqualTo(1));
            Assert.That(resource.AccessResourceId, Is.EqualTo(2));
            Assert.That(resource.IsActive, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidRoleId_ShouldThrowArgumentOutOfRangeException(long roleId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoleAccessResource(roleId, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidAccessResourceId_ShouldThrowArgumentOutOfRangeException(long accessResourceId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoleAccessResource(1, accessResourceId));
        }

        [Test]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            RoleAccessResource resource = new RoleAccessResource(1, 2);
            resource.Deactivate();

            resource.Activate();

            Assert.That(resource.IsActive, Is.True);
        }

        [Test]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            RoleAccessResource resource = new RoleAccessResource(1, 2);

            resource.Deactivate();

            Assert.That(resource.IsActive, Is.False);
        }

        [Test]
        public void Activate_WhenAlreadyActive_ShouldRemainActive()
        {
            RoleAccessResource resource = new RoleAccessResource(1, 2);

            resource.Activate();

            Assert.That(resource.IsActive, Is.True);
        }

        [Test]
        public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
        {
            RoleAccessResource resource = new RoleAccessResource(1, 2);
            resource.Deactivate();

            resource.Deactivate();

            Assert.That(resource.IsActive, Is.False);
        }
    }
}
