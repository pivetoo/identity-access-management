using IdentityManagement.Infrastructure.Services;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    /// <summary>
    /// Regressao do incidente do tenant provisionado sem assinatura.
    ///
    /// O gateway mantinha regra propria de documento (so digitos). Aplicada a um CNPJ alfanumerico,
    /// ela devolvia oito digitos soltos, o provedor recusava o cliente com "CPF/CNPJ invalido", e o
    /// onboarding seguia sem assinatura — o que tranca o tenant em 402 com a tela de pagamento
    /// quebrada. O documento tem de chegar ao provedor como o dominio o guarda.
    /// </summary>
    [TestFixture]
    public class AsaasGatewayDocumentTests
    {
        [TestCase("CLKLZ92L000118")]
        [TestCase("CL.KLZ.92L/0001-18")]
        [TestCase(" clklz92l000118 ")]
        public void Alphanumeric_document_keeps_its_letters(string entrada)
        {
            Assert.That(AsaasBillingGateway.GatewayDocument(entrada), Is.EqualTo("CLKLZ92L000118"));
        }

        [TestCase("64.224.591/0001-63")]
        [TestCase("64224591000163")]
        public void Numeric_document_loses_only_the_mask(string entrada)
        {
            Assert.That(AsaasBillingGateway.GatewayDocument(entrada), Is.EqualTo("64224591000163"));
        }

        [Test]
        public void Cpf_is_left_untouched_apart_from_the_mask()
        {
            Assert.That(AsaasBillingGateway.GatewayDocument("123.456.789-09"), Is.EqualTo("12345678909"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Empty_document_stays_empty(string? entrada)
        {
            Assert.That(AsaasBillingGateway.GatewayDocument(entrada), Is.EqualTo(string.Empty));
        }
    }
}
