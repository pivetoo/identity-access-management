using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class UserTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateUser()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.That(user.Username, Is.EqualTo("john_doe"));
            Assert.That(user.Email, Is.EqualTo("john@example.com"));
            Assert.That(user.PasswordHash, Is.EqualTo("hashed_password"));
            Assert.That(user.Name, Is.EqualTo("John Doe"));
            Assert.That(user.IsActive, Is.True);
            Assert.That(user.PreferredLanguage, Is.EqualTo(Language.PtBr));
            Assert.That(user.AvatarUrl, Is.Null);
            Assert.That(user.LastLoginAt, Is.Null);
        }

        [Test]
        public void Constructor_WithAvatarUrl_ShouldSetAvatarUrl()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe", "https://avatar.com/john.png");

            Assert.That(user.AvatarUrl, Is.EqualTo("https://avatar.com/john.png"));
        }

        [Test]
        public void Constructor_WithPreferredLanguage_ShouldSetLanguage()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe", preferredLanguage: Language.EnUs);

            Assert.That(user.PreferredLanguage, Is.EqualTo(Language.EnUs));
        }

        [Test]
        public void Constructor_WithWhitespaceAvatarUrl_ShouldSetAvatarUrlToNull()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe", "   ");

            Assert.That(user.AvatarUrl, Is.Null);
        }

        [Test]
        public void Constructor_WithNullUsername_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new User(null!, "john@example.com", "hashed_password", "John Doe"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidUsername_ShouldThrowArgumentException(string username)
        {
            Assert.Throws<ArgumentException>(() => new User(username, "john@example.com", "hashed_password", "John Doe"));
        }

        [Test]
        public void Constructor_WithNullEmail_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new User("john_doe", null!, "hashed_password", "John Doe"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string email)
        {
            Assert.Throws<ArgumentException>(() => new User("john_doe", email, "hashed_password", "John Doe"));
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new User("john_doe", "john@example.com", "hashed_password", null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new User("john_doe", "john@example.com", "hashed_password", name));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            User user = new User("  john_doe  ", "  john@example.com  ", "  hashed_password  ", "  John Doe  ");

            Assert.That(user.Username, Is.EqualTo("john_doe"));
            Assert.That(user.Email, Is.EqualTo("john@example.com"));
            Assert.That(user.PasswordHash, Is.EqualTo("hashed_password"));
            Assert.That(user.Name, Is.EqualTo("John Doe"));
        }

        [Test]
        public void RegisterLogin_ShouldSetLastLoginAt()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");
            DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

            user.RegisterLogin();

            Assert.That(user.LastLoginAt, Is.Not.Null);
            Assert.That(user.LastLoginAt.Value, Is.GreaterThan(before));
            Assert.That(user.LastLoginAt.Value, Is.LessThan(DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            user.Update("jane_doe", "jane@example.com", "Jane Doe", false, "https://avatar.com/jane.png");

            Assert.That(user.Username, Is.EqualTo("jane_doe"));
            Assert.That(user.Email, Is.EqualTo("jane@example.com"));
            Assert.That(user.Name, Is.EqualTo("Jane Doe"));
            Assert.That(user.IsActive, Is.False);
            Assert.That(user.AvatarUrl, Is.EqualTo("https://avatar.com/jane.png"));
        }

        [Test]
        public void Update_WithNullAvatarUrl_ShouldSetAvatarUrlToNull()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe", "https://avatar.com/john.png");

            user.Update("john_doe", "john@example.com", "John Doe", true, null);

            Assert.That(user.AvatarUrl, Is.Null);
        }

        [Test]
        public void Update_WithNullUsername_ShouldThrowArgumentNullException()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.Throws<ArgumentNullException>(() => user.Update(null!, "john@example.com", "John Doe", true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidUsername_ShouldThrowArgumentException(string username)
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.Throws<ArgumentException>(() => user.Update(username, "john@example.com", "John Doe", true));
        }

        [Test]
        public void ChangePassword_WithValidPassword_ShouldUpdatePasswordHash()
        {
            User user = new User("john_doe", "john@example.com", "old_hash", "John Doe");

            user.ChangePassword("new_hash");

            Assert.That(user.PasswordHash, Is.EqualTo("new_hash"));
        }

        [Test]
        public void ChangePassword_WithNullPassword_ShouldThrowArgumentNullException()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.Throws<ArgumentNullException>(() => user.ChangePassword(null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void ChangePassword_WithInvalidPassword_ShouldThrowArgumentException(string password)
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.Throws<ArgumentException>(() => user.ChangePassword(password));
        }

        [Test]
        public void ChangePassword_ShouldTrimPassword()
        {
            User user = new User("john_doe", "john@example.com", "old_hash", "John Doe");

            user.ChangePassword("  new_hash  ");

            Assert.That(user.PasswordHash, Is.EqualTo("new_hash"));
        }

        [Test]
        public void UpdateAvatar_ShouldSetAvatarUrl()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            user.UpdateAvatar("https://avatar.com/new.png");

            Assert.That(user.AvatarUrl, Is.EqualTo("https://avatar.com/new.png"));
        }

        [Test]
        public void UpdateAvatar_WithWhitespace_ShouldSetAvatarUrlToNull()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe", "https://avatar.com/old.png");

            user.UpdateAvatar("   ");

            Assert.That(user.AvatarUrl, Is.Null);
        }

        [Test]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            user.Deactivate();

            Assert.That(user.IsActive, Is.False);
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            User user = new User("john_doe", "john@example.com", "hashed_password", "John Doe");

            Assert.That(user.AuthorizationCodes, Is.Empty);
            Assert.That(user.RefreshTokens, Is.Empty);
            Assert.That(user.UserRoles, Is.Empty);
        }
    }
}
