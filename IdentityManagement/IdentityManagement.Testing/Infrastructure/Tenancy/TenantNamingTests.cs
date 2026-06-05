using IdentityManagement.Infrastructure.Tenancy;

namespace IdentityManagement.Testing.Infrastructure.Tenancy
{
    [TestFixture]
    public sealed class TenantNamingTests
    {
        [TestCase("Mainstay ME", "mainstay")]
        [TestCase("  Construtora Silva ", "construtora")]
        [TestCase("José & Cia", "jose")]
        [TestCase("3M do Brasil", "3m")]
        [TestCase("açaí top", "acai")]
        public void Slugify_first_word_lowercase_no_accents(string input, string expected)
        {
            Assert.That(TenantNaming.Slugify(input), Is.EqualTo(expected));
        }

        [Test]
        public void Slugify_empty_or_non_latin_falls_back_to_tenant()
        {
            Assert.That(TenantNaming.Slugify("   "), Is.EqualTo("tenant"));
            Assert.That(TenantNaming.Slugify("名前"), Is.EqualTo("tenant"));
        }

        [Test]
        public void DatabaseName_builds_prefix_slug_id()
        {
            Assert.That(TenantNaming.DatabaseName("agency-campaign", "mainstay", 6), Is.EqualTo("agencycampaign_mainstay_6"));
            Assert.That(TenantNaming.DatabaseName("integration-platform", "mainstay", 6), Is.EqualTo("integrationplatform_mainstay_6"));
        }

        [Test]
        public void DatabaseName_truncates_slug_to_fit_63_bytes()
        {
            string longSlug = new string('a', 80);
            string name = TenantNaming.DatabaseName("agency-campaign", longSlug, 6);
            Assert.That(name.Length, Is.LessThanOrEqualTo(63));
            Assert.That(name, Does.StartWith("agencycampaign_"));
            Assert.That(name, Does.EndWith("_6"));
        }

        [TestCase("agencycampaign_mainstay_6", true)]
        [TestCase("Robert'); DROP TABLE", false)]
        [TestCase("1bad", false)]
        [TestCase("with-hyphen", false)]
        public void IsValidIdentifier(string name, bool valid)
        {
            Assert.That(TenantNaming.IsValidIdentifier(name), Is.EqualTo(valid));
        }
    }
}
