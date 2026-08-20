namespace IdentityManagement.Application.Responses.Signup
{
    /// <summary>
    /// Resposta do cadastro publico. NAO devolve id de empresa, nome de banco de tenant, chave de
    /// API nem resultado de bootstrap: o onboarding administrativo devolve tudo isso, e nada disso
    /// pode vazar para um endpoint anonimo. O que a tela precisa e so para onde o convite foi e
    /// ate quando vai o teste.
    /// </summary>
    public sealed class SignupResponse
    {
        /// <summary>E-mail que recebeu o convite de administrador (o link de acesso vai por ele).</summary>
        public string Email { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string PlanName { get; set; } = string.Empty;

        public DateTimeOffset? TrialEndsAt { get; set; }

        /// <summary>
        /// Falso quando a empresa foi criada mas a assinatura nao pode ser concluida (falha do
        /// provedor de cobranca). A conta existe e NAO consegue entrar ate a assinatura existir —
        /// a tela precisa dizer que o suporte vai finalizar, em vez de mandar o usuario tentar logar.
        /// </summary>
        public bool SubscriptionActive { get; set; }
    }
}
