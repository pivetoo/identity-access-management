using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class SystemRoleTemplateTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateSystemRoleTemplate()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Administrator template description");

            Assert.That(template.SystemApplicationId, Is.EqualTo(1));
            Assert.That(template.Name, Is.EqualTo("Admin Template"));
            Assert.That(template.Description, Is.EqualTo("Administrator template description"));
            Assert.That(template.IsRoot, Is.False);
            Assert.That(template.IsDefault, Is.False);
            Assert.That(template.IsActive, Is.True);
        }

        [Test]
        public void Constructor_WithOptionalParameters_ShouldCreateSystemRoleTemplate()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description", isRoot: true, isDefault: true);

            Assert.That(template.IsRoot, Is.True);
            Assert.That(template.IsDefault, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SystemRoleTemplate(systemApplicationId, "Admin Template", "Description"));
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SystemRoleTemplate(1, null!, "Description"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new SystemRoleTemplate(1, name, "Description"));
        }

        [Test]
        public void Constructor_WithNullDescription_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SystemRoleTemplate(1, "Admin Template", null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidDescription_ShouldThrowArgumentException(string description)
        {
            Assert.Throws<ArgumentException>(() => new SystemRoleTemplate(1, "Admin Template", description));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "  Admin Template  ", "  Description  ");

            Assert.That(template.Name, Is.EqualTo("Admin Template"));
            Assert.That(template.Description, Is.EqualTo("Description"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            template.Update("Super Admin", "Super admin description", true, true, false);

            Assert.That(template.Name, Is.EqualTo("Super Admin"));
            Assert.That(template.Description, Is.EqualTo("Super admin description"));
            Assert.That(template.IsRoot, Is.True);
            Assert.That(template.IsDefault, Is.True);
            Assert.That(template.IsActive, Is.False);
        }

        [Test]
        public void Update_WithNullName_ShouldThrowArgumentNullException()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            Assert.Throws<ArgumentNullException>(() => template.Update(null!, "Description", false, false, true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            Assert.Throws<ArgumentException>(() => template.Update(name, "Description", false, false, true));
        }

        [Test]
        public void Update_WithNullDescription_ShouldThrowArgumentNullException()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            Assert.Throws<ArgumentNullException>(() => template.Update("Admin Template", null!, false, false, true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidDescription_ShouldThrowArgumentException(string description)
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            Assert.Throws<ArgumentException>(() => template.Update("Admin Template", description, false, false, true));
        }

        [Test]
        public void SetDefault_ShouldUpdateIsDefault()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            template.SetDefault(true);

            Assert.That(template.IsDefault, Is.True);
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            SystemRoleTemplate template = new SystemRoleTemplate(1, "Admin Template", "Description");

            Assert.That(template.SystemRoleTemplateAccessResources, Is.Empty);
        }
    }
}
