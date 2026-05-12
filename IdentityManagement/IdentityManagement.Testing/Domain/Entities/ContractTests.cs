using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class ContractTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateContract()
        {
            Contract contract = new(1, 1, Guid.NewGuid());

            Assert.That(contract.CompanyId, Is.EqualTo(1));
            Assert.That(contract.SystemApplicationId, Is.EqualTo(1));
            Assert.That(contract.IsActive, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidCompanyId_ShouldThrowArgumentOutOfRangeException(long companyId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Contract(companyId, 1, Guid.NewGuid()));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Contract(1, systemApplicationId, Guid.NewGuid()));
        }

        [Test]
        public void IsValid_WhenActiveAndNoEndDate_ShouldReturnTrue()
        {
            Contract contract = new(1, 1, Guid.NewGuid());

            Assert.That(contract.IsValid(), Is.True);
        }

        [Test]
        public void IsValid_WhenInactive_ShouldReturnFalse()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-1), null, false);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenStartDateInFuture_ShouldReturnFalse()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(1), null, true);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenEndDateInPast_ShouldReturnFalse()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1), true);

            Assert.That(contract.IsValid(), Is.False);
        }

        [Test]
        public void IsValid_WhenEndDateInFuture_ShouldReturnTrue()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            contract.Update(1, 1, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), true);

            Assert.That(contract.IsValid(), Is.True);
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            DateTimeOffset startDate = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset endDate = DateTimeOffset.UtcNow.AddDays(30);

            contract.Update(2, 3, startDate, endDate, false);

            Assert.That(contract.CompanyId, Is.EqualTo(2));
            Assert.That(contract.SystemApplicationId, Is.EqualTo(3));
            Assert.That(contract.StartDate, Is.EqualTo(startDate.ToUniversalTime()));
            Assert.That(contract.EndDate, Is.EqualTo(endDate.ToUniversalTime()));
            Assert.That(contract.IsActive, Is.False);
        }

        [Test]
        public void Update_WithNullEndDate_ShouldSetEndDateToNull()
        {
            Contract contract = new(1, 1, Guid.NewGuid());
            contract.Update(1, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), true);

            contract.Update(1, 1, DateTimeOffset.UtcNow, null, true);

            Assert.That(contract.EndDate, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Update_WithInvalidCompanyId_ShouldThrowArgumentOutOfRangeException(long companyId)
        {
            Contract contract = new(1, 1, Guid.NewGuid());

            Assert.Throws<ArgumentOutOfRangeException>(() => contract.Update(companyId, 1, DateTimeOffset.UtcNow, null, true));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Update_WithInvalidSystemApplicationId_ShouldThrowArgumentOutOfRangeException(long systemApplicationId)
        {
            Contract contract = new(1, 1, Guid.NewGuid());

            Assert.Throws<ArgumentOutOfRangeException>(() => contract.Update(1, systemApplicationId, DateTimeOffset.UtcNow, null, true));
        }

        [Test]
        public void Collections_ShouldBeEmptyOnCreation()
        {
            Contract contract = new(1, 1, Guid.NewGuid());

            Assert.That(contract.Roles, Is.Empty);
            Assert.That(contract.AuthorizationCodes, Is.Empty);
            Assert.That(contract.RefreshTokens, Is.Empty);
        }
    }
}
