using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Testing.Domain.ValueObjects
{
    /// <summary>
    /// Telefone do cadastro publico. Passou a ser obrigatorio porque o provedor de cobranca exige
    /// telefone no checkout de cartao: opcional aqui adiava a falha para a hora de assinar, longe
    /// da causa.
    /// </summary>
    [TestFixture]
    public class PhoneTests
    {
        [TestCase("11900000001", "celular sem mascara")]
        [TestCase("(11) 90000-0001", "celular com mascara")]
        [TestCase("1130000001", "fixo sem mascara")]
        [TestCase("(11) 3000-0001", "fixo com mascara")]
        [TestCase("(85) 98765-4321", "outro DDD")]
        public void Valid_phones_are_accepted(string phone, string caso)
        {
            Assert.That(Phone.IsValid(phone), Is.True, caso);
        }

        [TestCase("", "vazio")]
        [TestCase(null, "nulo")]
        [TestCase("119000000", "curto demais")]
        [TestCase("119000000012", "longo demais")]
        [TestCase("01900000001", "DDD comecando com zero")]
        [TestCase("10900000001", "DDD 10 nao existe")]
        [TestCase("11800000001", "celular de 11 digitos sem o 9")]
        [TestCase("1100000001", "fixo comecando com zero")]
        [TestCase("1199999999", "numero repetido")]
        [TestCase("abcdefghijk", "sem digito")]
        public void Invalid_phones_are_rejected(string? phone, string motivo)
        {
            Assert.That(Phone.IsValid(phone), Is.False, motivo);
        }

        [Test]
        public void Normalize_keeps_only_digits()
        {
            Assert.That(Phone.Normalize("(11) 90000-0001"), Is.EqualTo("11900000001"));
            Assert.That(Phone.Normalize(null), Is.Empty);
        }
    }
}
