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

        /// <summary>
        /// Formato alfanumerico da Receita: posicoes 1-12 com letras, verificadores numericos. O
        /// primeiro caso e o exemplo divulgado pela propria Receita, que serve de ancora — se a
        /// conta do modulo 11 for portada errada, e ele que denuncia.
        /// </summary>
        [TestCase("12ABC34501DE35")]
        [TestCase("12.ABC.345/01DE-35")]
        [TestCase("12abc34501de35")]
        [TestCase("ZZ999999ZZ0159")]
        [TestCase("A1B2C3D4E5F668")]
        public void Alphanumeric_documents_are_accepted(string document)
        {
            Assert.That(Cnpj.IsValid(document), Is.True);
        }

        [TestCase("12ABC34501DE34", "digito verificador errado")]
        [TestCase("12ABC34501DEA5", "verificador com letra")]
        [TestCase("12ABC34501D3E5", "verificador fora da posicao")]
        public void Alphanumeric_documents_with_broken_check_digits_are_rejected(string document, string motivo)
        {
            Assert.That(Cnpj.IsValid(document), Is.False, motivo);
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
        public void Normalize_keeps_letters_and_digits_in_upper_case()
        {
            Assert.That(Cnpj.Normalize("11.444.777/0001-61"), Is.EqualTo("11444777000161"));
            Assert.That(Cnpj.Normalize("12.abc.345/01de-35"), Is.EqualTo("12ABC34501DE35"));
            Assert.That(Cnpj.Normalize(null), Is.Empty);
        }
    }
}
