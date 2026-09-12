namespace IdentityManagement.Application.Responses.Signup
{
    /// <summary>
    /// O que o cadastro publico vai contratar, lido dos MESMOS planos que o Confirm usa.
    ///
    /// Existe para a tela de cadastro parar de anunciar preco em texto fixo. Enquanto o preco morava
    /// no HTML, a tela dizia um valor e o cadastro contratava outro — foi exatamente o que aconteceu
    /// com o plano de lancamento.
    /// </summary>
    public sealed class SignupOfferResponse
    {
        public string PlanName { get; set; } = string.Empty;

        public decimal MonthlyAmount { get; set; }

        public decimal AnnualAmount { get; set; }

        public string Currency { get; set; } = "BRL";

        public int TrialDays { get; set; }
    }
}
