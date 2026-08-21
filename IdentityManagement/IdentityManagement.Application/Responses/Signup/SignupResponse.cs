namespace IdentityManagement.Application.Responses.Signup
{
    /// <summary>
    /// Resposta da PRIMEIRA etapa do cadastro publico. Neste ponto nada foi provisionado: existe
    /// apenas uma linha pendente e um e-mail de confirmacao a caminho.
    ///
    /// NAO devolve id, nome de banco de tenant nem chave de API — nada disso pode vazar por um
    /// endpoint anonimo. O que a tela precisa e para onde o link foi e ate quando ele vale.
    /// </summary>
    public sealed class SignupResponse
    {
        /// <summary>E-mail que recebeu o link de confirmacao.</summary>
        public string Email { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string PlanName { get; set; } = string.Empty;

        /// <summary>Quando o link de confirmacao deixa de valer.</summary>
        public DateTimeOffset VerificationExpiresAt { get; set; }
    }
}
