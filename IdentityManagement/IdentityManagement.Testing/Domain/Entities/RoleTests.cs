using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class RoleTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateRole()
        {
            Role role = new Role("Admin", "Administrator role", 1);

            Assert.That(role.Name, Is.EqualTo("Admin"));
            Assert.That(role.Description, Is.EqualTo("Administrator role"));
            Assert.That(role.ContractId, Is.EqualTo(1));
            Assert.That(role.IsRoot, Is.False);
            Assert.That(role.IsDefault, Is.False);
        }

        [Test]
        public void Constructor_WithOptionalParameters_ShouldCreateRole()
        {
            Role role = new Role("Admin", "Administrator role", 1, isRoot: true, isDefault: true);

            Assert.That(role.IsRoot, Is.True);
            Assert.That(role.IsDefault, Is.True);
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Role(null!, "Description", 1));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new Role(name, "Description", 1));
        }

        [Test]
        public void Constructor_WithNullDescription_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Role("Admin", null!, 1));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidDescription_ShouldThrowArgumentException(string description)
        {
            Assert.Throws<ArgumentException>(() => new Role("Admin", description, 1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidContractId_ShouldThrowArgumentOutOfRangeException(long contractId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Role("Admin", "Description", contractId));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            Role role = new Role("  Admin  ", "  Administrator role  ", 1);

            Assert.That(role.Name, Is.EqualTo("Admin"));
            Assert.That(role.Description, Is.EqualTo("Administrator role"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            Role role = new Role("Admin", "Administrator role", 1);

            role.Update("SuperAdmin", "Super administrator", true, true);

            Assert.That(role.Name, Is.EqualTo("SuperAdmin"));
            Assert.That(role.Description, Is.EqualTo("Super administrator"));
            Assert.That(role.IsRoot, Is.True);
            Assert.That(role.IsDefault, Is.True);
        }

        [Test]
        public void Update_WithNullName_ShouldThrowArgumentNullException()
        {
            Role role = new Role("Admin", "Description", 1);

            Assert.Throws<ArgumentNullException>(() => role.Update(null!, "Description", false, false));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Role role = new Role("Admin", "Description", 1);

            Assert.Throws<ArgumentException>(() => role.Update(name, "Description", false, false));
        }

        [Test]
        public void Update_WithNullDescription_ShouldThrowArgumentNullException()
        {
            Role role = new Role("Admin", "Description", 1);

            Assert.Throws<ArgumentNullException>(() => role.Update("Admin", null!, false, false));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidDescription_ShouldThrowArgumentException(string description)
        {
            Role role = new Role("Admin", "Description", 1);

            Assert.Throws<ArgumentException>(() => role.Update("Admin", description, false, false));
        }

        [Test]
        public void SetDefault_ShouldUpdateIsDefault()
        {
            Role role = new Role("Admin", "Description", 1);

            role.SetDefault(true);

            Assert.That(role.IsDefault, Is.True);
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            Role role = new Role("Admin", "Description", 1);

            Assert.That(role.UserRoles, Is.Empty);
            Assert.That(role.RoleAccessResources, Is.Empty);
        }

        [Test]
        public void Constructor_ShouldStartActive()
        {
            Role role = new Role("Gerente", "Perfil de gerente", 1);

            Assert.That(role.IsActive, Is.True);
        }

        [Test]
        public void SetActive_False_ShouldDeactivateRole()
        {
            Role role = new Role("Gerente", "Perfil de gerente", 1);

            role.SetActive(false);

            Assert.That(role.IsActive, Is.False);
        }
    }
}
