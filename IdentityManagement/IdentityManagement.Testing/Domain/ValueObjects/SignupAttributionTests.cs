using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.ValueObjects
{
    /// <summary>
    /// A origem do cadastro vem da URL montada por quem anuncia. O que importa e caber na coluna e
    /// nao gravar lixo: vazio vira nulo, espaco some, excesso e cortado sem erro.
    /// </summary>
    [TestFixture]
    public class SignupAttributionTests
    {
        [Test]
        public void Blank_values_become_null_and_the_result_is_empty()
        {
            SignupAttribution subject = SignupAttribution.Normalize("", "   ", null, "", null, " ", null, "", null);

            Assert.That(subject.IsEmpty, Is.True);
            Assert.That(subject.Source, Is.Null);
            Assert.That(subject.Medium, Is.Null);
        }

        [Test]
        public void Values_are_trimmed_and_kept()
        {
            SignupAttribution subject = SignupAttribution.Normalize(
                " meta ", "cpc", " lancamento-fundador ", "video-hero", null,
                null, " IwAR0abc ", "https://mainstay.com.br/?utm_source=meta", "https://l.facebook.com/");

            Assert.That(subject.IsEmpty, Is.False);
            Assert.That(subject.Source, Is.EqualTo("meta"));
            Assert.That(subject.Campaign, Is.EqualTo("lancamento-fundador"));
            Assert.That(subject.Fbclid, Is.EqualTo("IwAR0abc"));
            Assert.That(subject.Term, Is.Null);
            Assert.That(subject.Referrer, Is.EqualTo("https://l.facebook.com/"));
        }

        [Test]
        public void Values_longer_than_the_column_are_truncated_instead_of_rejected()
        {
            string longValue = new string('x', 700);

            SignupAttribution subject = SignupAttribution.Normalize(longValue, null, null, null, null, null, null, longValue, null);

            Assert.That(subject.Source, Has.Length.EqualTo(SignupAttribution.ShortMaxLength));
            Assert.That(subject.LandingPage, Has.Length.EqualTo(SignupAttribution.LongMaxLength));
        }
    }
}
