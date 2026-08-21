using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Testing.Domain.Entities
{
    /// <summary>
    /// Documento normalizado na escrita. Sem isso, "64.224.591/0001-63" e "64224591000163" convivem
    /// na mesma coluna e o CNPJ duplicado passa direto pelo cadastro publico e pelo indice unico.
    /// </summary>
    [TestFixture]
    public class CompanyDocumentTests
    {
        [TestCase("64.224.591/0001-63")]
        [TestCase("64224591000163")]
        [TestCase(" 64.224.591/0001-63 ")]
        public void Document_is_stored_without_mask(string entrada)
        {
            Company company = new("Empresa LTDA", "Empresa", entrada, "a@b.com", "11999990000");

            Assert.That(company.Document, Is.EqualTo("64224591000163"));
        }

        /// <summary>
        /// Regressao: a normalizacao descartava tudo que nao fosse digito, entao um CNPJ
        /// alfanumerico era gravado MUTILADO — documento diferente do informado, em silencio.
        /// </summary>
        [TestCase("12.ABC.345/01DE-35")]
        [TestCase("12abc34501de35")]
        public void Alphanumeric_document_keeps_its_letters(string entrada)
        {
            Company company = new("Empresa LTDA", "Empresa", entrada, "a@b.com", "11999990000");

            Assert.That(company.Document, Is.EqualTo("12ABC34501DE35"));
        }

        [Test]
        public void Update_also_normalizes_the_document()
        {
            Company company = new("Empresa LTDA", "Empresa", "64224591000163", "a@b.com", "11999990000");

            company.Update("Empresa LTDA", "Empresa", "11.444.777/0001-61", "a@b.com", "11999990000", isActive: true);

            Assert.That(company.Document, Is.EqualTo("11444777000161"));
        }
    }
}
