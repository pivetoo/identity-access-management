namespace IdentityManagement.Infrastructure.Signup
{
    /// <summary>
    /// Configuracao do cadastro publico. Fica no servidor de proposito: plano e sistemas
    /// contratados NAO podem vir da requisicao.
    /// </summary>
    public sealed class SignupOptions
    {
        public const string SectionName = "Signup";

        /// <summary>Chave geral. Desligado, o endpoint responde 404 — sem pista de que existe.</summary>
        public bool Enabled { get; set; }

        public string MonthlyPlanName { get; set; } = "Essencial Mensal";

        public string AnnualPlanName { get; set; } = "Essencial Anual";

        /// <summary>
        /// Audiences provisionadas no cadastro. O Mainstay precisa de agency-campaign E
        /// integration-platform: o blueprint do AgencyCampaign injeta a chave de API do
        /// IntegrationPlatform (ValueSource TenantApiKey, SourceAudience integration-platform),
        /// e sem o contrato do IP essa chave nao existe — as integracoes nascem mortas.
        /// </summary>
        public List<string> SystemAudiences { get; set; } = ["agency-campaign", "integration-platform"];
    }
}
