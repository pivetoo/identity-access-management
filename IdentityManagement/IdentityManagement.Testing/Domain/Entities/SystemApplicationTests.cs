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
            SystemApplication app = new SystemApplication("My App", "Application description", "https://app.com/callback", "my-audience");

            Assert.That(app.Name, Is.EqualTo("My App"));
            Assert.That(app.Description, Is.EqualTo("Application description"));
            Assert.That(app.RedirectUris, Is.EqualTo("https://app.com/callback"));
            Assert.That(app.Audience, Is.EqualTo("my-audience"));
            Assert.That(app.IsActive, Is.True);
            Assert.That(app.Type, Is.EqualTo(ApplicationType.External));
        }

        [Test]
        public void Constructor_WithInternalType_ShouldCreateSystemApplication()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience", ApplicationType.Internal);

            Assert.That(app.Type, Is.EqualTo(ApplicationType.Internal));
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SystemApplication(null!, "Description", "https://app.com/callback", "my-audience"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new SystemApplication(name, "Description", "https://app.com/callback", "my-audience"));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            SystemApplication app = new SystemApplication("  My App  ", "  Description  ", "  https://app.com/callback  ", "  my-audience  ");

            Assert.That(app.Name, Is.EqualTo("My App"));
            Assert.That(app.Description, Is.EqualTo("Description"));
            Assert.That(app.RedirectUris, Is.EqualTo("https://app.com/callback"));
            Assert.That(app.Audience, Is.EqualTo("my-audience"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            app.Update("New App", "New Description", "https://new.com/callback", "new-audience", ApplicationType.Internal, false);

            Assert.That(app.Name, Is.EqualTo("New App"));
            Assert.That(app.Description, Is.EqualTo("New Description"));
            Assert.That(app.RedirectUris, Is.EqualTo("https://new.com/callback"));
            Assert.That(app.Audience, Is.EqualTo("new-audience"));
            Assert.That(app.Type, Is.EqualTo(ApplicationType.Internal));
            Assert.That(app.IsActive, Is.False);
        }

        [Test]
        public void Update_WithNullName_ShouldThrowArgumentNullException()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.Throws<ArgumentNullException>(() => app.Update(null!, "Description", "https://app.com/callback", "my-audience", ApplicationType.External, true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.Throws<ArgumentException>(() => app.Update(name, "Description", "https://app.com/callback", "my-audience", ApplicationType.External, true));
        }

        [Test]
        public void IsRedirectUriValid_WithMatchingUri_ShouldReturnTrue()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback,https://app.com/redirect", "my-audience");

            Assert.That(app.IsRedirectUriValid("https://app.com/callback"), Is.True);
            Assert.That(app.IsRedirectUriValid("https://app.com/redirect"), Is.True);
        }

        [Test]
        public void IsRedirectUriValid_WithCaseInsensitiveMatch_ShouldReturnTrue()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.That(app.IsRedirectUriValid("HTTPS://APP.COM/CALLBACK"), Is.True);
        }

        [Test]
        public void IsRedirectUriValid_WithNonMatchingUri_ShouldReturnFalse()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.That(app.IsRedirectUriValid("https://other.com/callback"), Is.False);
        }

        [Test]
        public void IsRedirectUriValid_WithEmptyRedirectUris_ShouldReturnFalse()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "", "my-audience");

            Assert.That(app.IsRedirectUriValid("https://app.com/callback"), Is.False);
        }

        [Test]
        public void IsRedirectUriValid_WithWhitespaceRedirectUri_ShouldReturnFalse()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.That(app.IsRedirectUriValid("   "), Is.False);
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            SystemApplication app = new SystemApplication("My App", "Description", "https://app.com/callback", "my-audience");

            Assert.That(app.Contracts, Is.Empty);
        }
    }
}
