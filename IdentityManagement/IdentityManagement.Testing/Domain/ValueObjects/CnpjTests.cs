using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.ValueObjects
{
    /// <summary>
    /// CNPJ do cadastro publico. E a primeira barreira do unico endpoint anonimo que cria
    /// infraestrutura: documento invalido tem que morrer antes de virar empresa e banco de tenant.
    /// </summary>
    [TestFixture]
    public class CnpjTests
    {
        [TestCase("11444777000161")]
        [TestCase("11.444.777/0001-61")]
        [TestCase(" 11444777000161 ")]
        public void Valid_documents_are_accepted_with_or_without_mask(string document)
        {
            Assert.That(Cnpj.IsValid(document), Is.True);
        }

        [TestCase("11444777000160", "digito verificador errado")]
        [TestCase("1144477700016", "curto demais")]
        [TestCase("114447770001611", "longo demais")]
        [TestCase("11111111111111", "todos os digitos iguais")]
        [TestCase("00000000000000", "zeros")]
        [TestCase("", "vazio")]
        [TestCase(null, "nulo")]
        [TestCase("abcdefghijklmn", "sem digito")]
        public void Invalid_documents_are_rejected(string? document, string motivo)
        {
            Assert.That(Cnpj.IsValid(document), Is.False, motivo);
        }

        [Test]
        public void Normalize_keeps_only_digits()
        {
            Assert.That(Cnpj.Normalize("11.444.777/0001-61"), Is.EqualTo("11444777000161"));
            Assert.That(Cnpj.Normalize(null), Is.Empty);
        }
    }
}
