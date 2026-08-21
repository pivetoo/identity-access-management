namespace IdentityManagement.Application.Responses.Signup
{
    /// <summary>
    /// Resposta da SEGUNDA etapa: o e-mail foi confirmado e o ambiente acabou de ser provisionado.
    /// </summary>
    public sealed class SignupConfirmResponse
    {
        /// <summary>
        /// Token do convite de administrador, para a tela emendar direto na definicao de senha sem
        /// obrigar uma segunda ida ao e-mail.
        ///
        /// Devolver isto para um chamador anonimo e seguro porque ele so chega aqui apresentando o
        /// token de confirmacao, que por sua vez so existe dentro da caixa de entrada — a capacidade
        /// e a mesma, nao uma escalada. O convite tambem vai por e-mail, que e o caminho de volta se
        /// a aba fechar no meio.
        /// </summary>
        public string SetupToken { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string PlanName { get; set; } = string.Empty;

        public DateTimeOffset? TrialEndsAt { get; set; }

        /// <summary>
        /// Falso quando a empresa foi criada mas a assinatura nao pode ser concluida (falha do
        /// provedor de cobranca). A conta existe e NAO entra ate a assinatura existir — a tela
        /// precisa dizer que o suporte vai finalizar, em vez de mandar tentar o login.
        /// </summary>
        public bool SubscriptionActive { get; set; }
    }
}
