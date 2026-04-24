using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class ContractTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateContract()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");

            Assert.That(contract.CompanyId, Is.EqualTo(1));
            Assert.That(contract.SystemApplicationId, Is.EqualTo(1));
            Assert.That(contract.ClientId, Is.EqualTo("client_id"));
            Assert.That(contract.ClientSecret, Is.EqualTo("client_secret"));
            Assert.That(contract.JwtSecretKey, Is.EqualTo("jwt_secret_key"));
            Assert.That(contract.IsActive, Is.True);
            Assert.That(contract.AccessTokenLifetime, Is.EqualTo(3600));
            Assert.That(contract.RefreshTokenLifetime, Is.EqualTo(2592000));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidCompanyId_ShouldThrowArgumentOutOfRangeException(long companyId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Contract(companyId, 1, "client_id", "client_secret", "jwt_secret_key"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Contract(1, systemApplicationId, "client_id", "client_secret", "jwt_secret_key"));
        }

        [Test]
        public void Constructor_WithNullClientId_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Contract(1, 1, null!, "client_secret", "jwt_secret_key"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidClientId_ShouldThrowArgumentException(string clientId)
        {
            Assert.Throws<ArgumentException>(() => new Contract(1, 1, clientId, "client_secret", "jwt_secret_key"));
        }

        [Test]
        public void Constructor_WithNullClientSecret_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Contract(1, 1, "client_id", null!, "jwt_secret_key"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidClientSecret_ShouldThrowArgumentException(string clientSecret)
        {
            Assert.Throws<ArgumentException>(() => new Contract(1, 1, "client_id", clientSecret, "jwt_secret_key"));
        }

        [Test]
        public void Constructor_WithNullJwtSecretKey_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Contract(1, 1, "client_id", "client_secret", null!));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidJwtSecretKey_ShouldThrowArgumentException(string jwtSecretKey)
        {
            Assert.Throws<ArgumentException>(() => new Contract(1, 1, "client_id", "client_secret", jwtSecretKey));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            Contract contract = new Contract(1, 1, "  client_id  ", "  client_secret  ", "  jwt_secret_key  ");

            Assert.That(contract.ClientId, Is.EqualTo("client_id"));
            Assert.That(contract.ClientSecret, Is.EqualTo("client_secret"));
            Assert.That(contract.JwtSecretKey, Is.EqualTo("jwt_secret_key"));
        }

        [Test]
        public void IsValid_WhenActiveAndNoEndDate_ShouldReturnTrue()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");

            Assert.That(contract.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenInactive_ShouldReturnFalse()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-1), null, false, 3600, 2592000);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenStartDateInFuture_ShouldReturnFalse()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(1), null, true, 3600, 2592000);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenEndDateInPast_ShouldReturnFalse()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1), true, 3600, 2592000);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenEndDateInFuture_ShouldReturnTrue()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), true, 3600, 2592000);

            Assert.That(contract.IsValid(), Is.True);
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            DateTimeOffset startDate = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endDate = DateTimeOffset.UtcNow.AddDays(30);

            contract.Update(2, 3, startDate, endDate, false, 7200, 5184000);

            Assert.That(contract.CompanyId, Is.EqualTo(2));
            Assert.That(contract.SystemApplicationId, Is.EqualTo(3));
            Assert.That(contract.StartDate, Is.EqualTo(startDate.ToUniversalTime()));
            Assert.That(contract.EndDate, Is.EqualTo(endDate.ToUniversalTime()));
            Assert.That(contract.IsActive, Is.False);
            Assert.That(contract.AccessTokenLifetime, Is.EqualTo(7200));
            Assert.That(contract.RefreshTokenLifetime, Is.EqualTo(5184000));
        }

        [Test]
        public void Update_WithNullEndDate_ShouldSetEndDateToNull()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");
            contract.Update(1, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), true, 3600, 2592000);

            contract.Update(1, 1, DateTimeOffset.UtcNow, null, true, 3600, 2592000);

            Assert.That(contract.EndDate, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Update_WithInvalidCompanyId_ShouldThrowArgumentOutOfRangeException(long companyId)
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");

            Assert.Throws<ArgumentOutOfRangeException>(() => contract.Update(companyId, 1, DateTimeOffset.UtcNow, null, true, 3600, 2592000));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Update_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");

            Assert.Throws<ArgumentOutOfRangeException>(() => contract.Update(1, systemApplicationId, DateTimeOffset.UtcNow, null, true, 3600, 2592000));
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            Contract contract = new Contract(1, 1, "client_id", "client_secret", "jwt_secret_key");

            Assert.That(contract.Roles, Is.Empty);
            Assert.That(contract.AuthorizationCodes, Is.Empty);
            Assert.That(contract.RefreshTokens, Is.Empty);
        }
    }
}
