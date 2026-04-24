using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class SystemRoleTemplateAccessResourceTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateSystemRoleTemplateAccessResource()
        {
            SystemRoleTemplateAccessResource resource = new SystemRoleTemplateAccessResource(1, 2);

            Assert.That(resource.SystemRoleTemplateId, Is.EqualTo(1));
            Assert.That(resource.AccessResourceId, Is.EqualTo(2));
            Assert.That(resource.IsActive, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidSystemRoleTemplateId_ShouldThrowArgumentOutOfRangeException(long systemRoleTemplateId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SystemRoleTemplateAccessResource(systemRoleTemplateId, 2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidAccessResourceId_ShouldThrowArgumentOutOfRangeException(long accessResourceId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SystemRoleTemplateAccessResource(1, accessResourceId));
        }

        [Test]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            SystemRoleTemplateAccessResource resource = new SystemRoleTemplateAccessResource(1, 2);
            resource.Deactivate();

            resource.Activate();

            Assert.That(resource.IsActive, Is.True);
        }

        [Test]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            SystemRoleTemplateAccessResource resource = new SystemRoleTemplateAccessResource(1, 2);

            resource.Deactivate();

            Assert.That(resource.IsActive, Is.False);
        }

        [Test]
        public void Activate_WhenAlreadyActive_ShouldRemainActive()
        {
            SystemRoleTemplateAccessResource resource = new SystemRoleTemplateAccessResource(1, 2);

            resource.Activate();

            Assert.That(resource.IsActive, Is.True);
        }

        [Test]
        public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
        {
            SystemRoleTemplateAccessResource resource = new SystemRoleTemplateAccessResource(1, 2);
            resource.Deactivate();

            resource.Deactivate();

            Assert.That(resource.IsActive, Is.False);
        }
    }
}
