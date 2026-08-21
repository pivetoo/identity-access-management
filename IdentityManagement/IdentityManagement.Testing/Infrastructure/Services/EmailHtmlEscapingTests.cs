using System.Net;
using IdentityManagement.Infrastructure.Services;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    /// <summary>
    /// O nome da agencia chega por um endpoint ANONIMO e cai dentro do corpo do e-mail. Sem escapar,
    /// qualquer um manda HTML — inclusive um link — num e-mail assinado por mainstay.com.br, com SPF
    /// e DKIM validos, para o endereco que escolher: relay de phishing usando a reputacao do dominio.
    /// </summary>
    [TestFixture]
    public class EmailHtmlEscapingTests
    {
        [Test]
        public void SignupVerification_escapes_markup_in_the_company_name()
        {
            const string malicioso = "</strong><a href=\"https://phishing.example\">Valide sua conta</a><strong>";

            string html = ResendEmailSender.BuildSignupVerificationHtml(malicioso, "https://auth.example/signup/confirmar?token=abc", 24);

            Assert.That(html, Does.Not.Contain("<a href=\"https://phishing.example\""));
            Assert.That(html, Does.Contain("&lt;a href="));
        }

        [Test]
        public void SignupVerification_keeps_accented_names_readable()
        {
            // Escapar nao pode estragar nome legitimo: acento e & sao comuns em razao social.
            // O WebUtility escreve acento como entidade numerica (ê vira &#234;), que renderiza
            // igual no cliente de e-mail — por isso a verificacao e por ida e volta, nao literal.
            const string nome = "Agência Ação & Cia";

            string html = ResendEmailSender.BuildSignupVerificationHtml(nome, "https://auth.example/x", 24);

            Assert.That(WebUtility.HtmlDecode(html), Does.Contain(nome));
        }
    }
}
