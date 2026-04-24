using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class AccessResourceTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateAccessResource()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            Assert.That(resource.SystemApplicationId, Is.EqualTo(1));
            Assert.That(resource.Name, Is.EqualTo("Users.Read"));
            Assert.That(resource.Description, Is.EqualTo("Read users"));
            Assert.That(resource.Controller, Is.EqualTo("UsersController"));
            Assert.That(resource.Action, Is.EqualTo("Get"));
            Assert.That(resource.HttpMethod, Is.EqualTo("GET"));
            Assert.That(resource.Route, Is.EqualTo("/api/users"));
            Assert.That(resource.IsActive, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AccessResource(systemApplicationId, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users"));
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, null!, "Read users", "UsersController", "Get", "GET", "/api/users"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => new AccessResource(1, name, "Read users", "UsersController", "Get", "GET", "/api/users"));
        }

        [Test]
        public void Constructor_WithNullDescription_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, "Users.Read", null!, "UsersController", "Get", "GET", "/api/users"));
        }

        [Test]
        public void Constructor_WithNullController_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, "Users.Read", "Read users", null!, "Get", "GET", "/api/users"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidController_ShouldThrowArgumentException(string controller)
        {
            Assert.Throws<ArgumentException>(() => new AccessResource(1, "Users.Read", "Read users", controller, "Get", "GET", "/api/users"));
        }

        [Test]
        public void Constructor_WithNullAction_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", null!, "GET", "/api/users"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidAction_ShouldThrowArgumentException(string action)
        {
            Assert.Throws<ArgumentException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", action, "GET", "/api/users"));
        }

        [Test]
        public void Constructor_WithNullHttpMethod_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", null!, "/api/users"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidHttpMethod_ShouldThrowArgumentException(string httpMethod)
        {
            Assert.Throws<ArgumentException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", httpMethod, "/api/users"));
        }

        [Test]
        public void Constructor_WithNullRoute_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidRoute_ShouldThrowArgumentException(string route)
        {
            Assert.Throws<ArgumentException>(() => new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", route));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            AccessResource resource = new AccessResource(1, "  Users.Read  ", "  Read users  ", "  UsersController  ", "  Get  ", "  get  ", "  /api/users  ");

            Assert.That(resource.Name, Is.EqualTo("Users.Read"));
            Assert.That(resource.Description, Is.EqualTo("Read users"));
            Assert.That(resource.Controller, Is.EqualTo("UsersController"));
            Assert.That(resource.Action, Is.EqualTo("Get"));
            Assert.That(resource.HttpMethod, Is.EqualTo("GET"));
            Assert.That(resource.Route, Is.EqualTo("/api/users"));
        }

        [Test]
        public void Constructor_ShouldConvertHttpMethodToUpperCase()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "get", "/api/users");

            Assert.That(resource.HttpMethod, Is.EqualTo("GET"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            resource.Update(2, "Read all users", "UsersController", "GetAll", "POST", "/api/users/all");

            Assert.That(resource.SystemApplicationId, Is.EqualTo(2));
            Assert.That(resource.Description, Is.EqualTo("Read all users"));
            Assert.That(resource.Controller, Is.EqualTo("UsersController"));
            Assert.That(resource.Action, Is.EqualTo("GetAll"));
            Assert.That(resource.HttpMethod, Is.EqualTo("POST"));
            Assert.That(resource.Route, Is.EqualTo("/api/users/all"));
        }

        [Test]
        public void Update_ShouldConvertHttpMethodToUpperCase()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            resource.Update(1, "Read users", "UsersController", "Get", "post", "/api/users");

            Assert.That(resource.HttpMethod, Is.EqualTo("POST"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Update_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            Assert.Throws<ArgumentOutOfRangeException>(() => resource.Update(systemApplicationId, "Read users", "UsersController", "Get", "GET", "/api/users"));
        }

        [Test]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");
            resource.Deactivate();

            resource.Activate();

            Assert.That(resource.IsActive, Is.True);
        }

        [Test]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            resource.Deactivate();

            Assert.That(resource.IsActive, Is.False);
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            AccessResource resource = new AccessResource(1, "Users.Read", "Read users", "UsersController", "Get", "GET", "/api/users");

            Assert.That(resource.RoleAccessResources, Is.Empty);
        }
    }
}
