using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    [TestFixture]
    public class CompanyTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateCompany()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.That(company.LegalName, Is.EqualTo("Legal Name Inc"));
            Assert.That(company.TradeName, Is.EqualTo("Trade Name"));
            Assert.That(company.Document, Is.EqualTo("12345678000195"));
            Assert.That(company.Email, Is.EqualTo("company@example.com"));
            Assert.That(company.PhoneNumber, Is.EqualTo("+5511999999999"));
            Assert.That(company.IsActive, Is.True);
        }

        [Test]
        public void Constructor_WithNullLegalName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Company(null!, "Trade Name", "12345678000195", "company@example.com", "+5511999999999"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidLegalName_ShouldThrowArgumentException(string legalName)
        {
            Assert.Throws<ArgumentException>(() => new Company(legalName, "Trade Name", "12345678000195", "company@example.com", "+5511999999999"));
        }

        [Test]
        public void Constructor_WithNullTradeName_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Company("Legal Name", null!, "12345678000195", "company@example.com", "+5511999999999"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_WithInvalidTradeName_ShouldThrowArgumentException(string tradeName)
        {
            Assert.Throws<ArgumentException>(() => new Company("Legal Name", tradeName, "12345678000195", "company@example.com", "+5511999999999"));
        }

        [Test]
        public void Constructor_ShouldTrimInputValues()
        {
            Company company = new Company("  Legal Name Inc  ", "  Trade Name  ", "  12345678000195  ", "  company@example.com  ", "  +5511999999999  ");

            Assert.That(company.LegalName, Is.EqualTo("Legal Name Inc"));
            Assert.That(company.TradeName, Is.EqualTo("Trade Name"));
            Assert.That(company.Document, Is.EqualTo("12345678000195"));
            Assert.That(company.Email, Is.EqualTo("company@example.com"));
            Assert.That(company.PhoneNumber, Is.EqualTo("+5511999999999"));
        }

        [Test]
        public void Update_WithValidParameters_ShouldUpdateProperties()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            company.Update("New Legal Name", "New Trade Name", "98765432000196", "new@example.com", "+5511888888888", false);

            Assert.That(company.LegalName, Is.EqualTo("New Legal Name"));
            Assert.That(company.TradeName, Is.EqualTo("New Trade Name"));
            Assert.That(company.Document, Is.EqualTo("98765432000196"));
            Assert.That(company.Email, Is.EqualTo("new@example.com"));
            Assert.That(company.PhoneNumber, Is.EqualTo("+5511888888888"));
            Assert.That(company.IsActive, Is.False);
        }

        [Test]
        public void Update_WithNullLegalName_ShouldThrowArgumentNullException()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.Throws<ArgumentNullException>(() => company.Update(null!, "Trade Name", "12345678000195", "company@example.com", "+5511999999999", true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidLegalName_ShouldThrowArgumentException(string legalName)
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.Throws<ArgumentException>(() => company.Update(legalName, "Trade Name", "12345678000195", "company@example.com", "+5511999999999", true));
        }

        [Test]
        public void Update_WithNullTradeName_ShouldThrowArgumentNullException()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.Throws<ArgumentNullException>(() => company.Update("Legal Name Inc", null!, "12345678000195", "company@example.com", "+5511999999999", true));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Update_WithInvalidTradeName_ShouldThrowArgumentException(string tradeName)
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.Throws<ArgumentException>(() => company.Update("Legal Name Inc", tradeName, "12345678000195", "company@example.com", "+5511999999999", true));
        }

        [Test]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            company.Deactivate();

            Assert.That(company.IsActive, Is.False);
        }

        [Test]
        public void Contracts_ShouldBeEmptyOnCreation()
        {
            Company company = new Company("Legal Name Inc", "Trade Name", "12345678000195", "company@example.com", "+5511999999999");

            Assert.That(company.Contracts, Is.Empty);
        }
    }
}
