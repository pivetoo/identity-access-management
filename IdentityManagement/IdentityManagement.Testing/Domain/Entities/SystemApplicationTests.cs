using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class SystemApplicationTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateSystemApplication()
        {
            SystemApplication app = new("My App", "Application description", "my-audience");

            Assert.That(app.Name, Is.EqualTo("My App"));
            Assert.That(app.Description, Is.EqualTo("Application description"));
            Assert.That(app.Audience, Is.EqualTo("my-audience"));
            Assert.That(app.IsActive, Is.True);
            Assert.That(app.Type, Is.EqualTo(ApplicationType.External));
            Assert.That(app.GrantsAdminOnSetup, Is.True, "por padrao a aplicacao entra no convite de administrador");
        }

        [Test]
        public void Constructor_WithInternalType_ShouldCreateSystemApplication()
        {
            SystemApplication app = new("My App", "Description", "my-audience", ApplicationType.Internal);

            Assert.That(app.Type, Is.EqualTo(ApplicationType.Internal));
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SystemApplication(null!, "Description", "my-audience"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new SystemApplication(name, "Description", "my-audience"));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            SystemApplication app = new("  My App  ", "  Description  ", "  my-audience  ");

            Assert.That(app.Name, Is.EqualTo("My App"));
            Assert.That(app.Description, Is.EqualTo("Description"));
            Assert.That(app.Audience, Is.EqualTo("my-audience"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            SystemApplication app = new("My App", "Description", "my-audience");

            app.Update("New App", "New Description", "new-audience", ApplicationType.Internal, false, false);

            Assert.That(app.Name, Is.EqualTo("New App"));
            Assert.That(app.Description, Is.EqualTo("New Description"));
            Assert.That(app.Audience, Is.EqualTo("new-audience"));
            Assert.That(app.Type, Is.EqualTo(ApplicationType.Internal));
            Assert.That(app.IsActive, Is.False);
            Assert.That(app.GrantsAdminOnSetup, Is.False);
        }

        [Test]
        public void Update_ShouldKeepGrantsAdminOnSetupWhenLeftEnabled()
        {
            SystemApplication app = new("My App", "Description", "my-audience");

            app.Update("My App", "Description", "my-audience", ApplicationType.External, true, true);

            Assert.That(app.GrantsAdminOnSetup, Is.True);
        }

        [Test]
        public void Update_WithNullName_ShouldThrowArgumentNullException()
        {
            SystemApplication app = new("My App", "Description", "my-audience");

            Assert.Throws<ArgumentNullException>(() => app.Update(null!, "Description", "my-audience", ApplicationType.External, true, true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            SystemApplication app = new("My App", "Description", "my-audience");

            Assert.Throws<ArgumentException>(() => app.Update(name, "Description", "my-audience", ApplicationType.External, true, true));
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            SystemApplication app = new("My App", "Description", "my-audience");

            Assert.That(app.Contracts, Is.Empty);
        }
    }
}
