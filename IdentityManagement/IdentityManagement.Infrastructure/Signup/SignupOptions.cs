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

        public string MonthlyPlanName { get; set; } = "Completo Mensal";

        public string AnnualPlanName { get; set; } = "Completo Anual";

        /// <summary>
        /// Validade do link de confirmacao de e-mail. Curto o bastante para a tabela de pendentes
        /// nao acumular, longo o bastante para quem so olha o e-mail no dia seguinte.
        /// </summary>
        public int VerificationLinkHours { get; set; } = 24;

        /// <summary>
        /// Teto GLOBAL de provisionamentos por hora pelo cadastro publico — nao por IP.
        ///
        /// Rate limit e captcha encarecem o ataque, mas o estrago continua proporcional ao bolso de
        /// quem ataca: com IPs e caixas de entrada suficientes, o volume passa. Este teto limita o
        /// ESTRAGO, independente do tamanho do atacante.
        ///
        /// Importa porque o Postgres roda no host compartilhado por todos os sistemas: encher o
        /// disco por aqui derruba IdM, IntegrationPlatform e help-desk junto.
        ///
        /// Subir e so trocar no arquivo montado e reiniciar a API (IOptions le no startup).
        /// </summary>
        public int GlobalHourlyProvisioningLimit { get; set; } = 20;

        /// <summary>
        /// Quantos e-mails de confirmacao um MESMO endereco pode receber em 24h.
        ///
        /// Existe contra bombardeio de inscricao: o ataque mira um endereco a partir de muitos IPs,
        /// entao limite por IP nao enxerga o padrao — so um limite chaveado no destino enxerga.
        /// Retentativa honesta e uma ou duas; bombardeio e centena.
        /// </summary>
        public int MaxVerificationEmailsPerAddressPerDay { get; set; } = 5;

        /// <summary>
        /// Teto GLOBAL de e-mails de confirmacao por hora, independente de IP e de destinatario.
        ///
        /// Ultima linha: se as camadas de cima forem furadas, isto limita o estrago na reputacao do
        /// dominio. Vale porque o dano aqui e assimetrico — disco cheio se resolve comprando espaco,
        /// mas dominio em blocklist derruba redefinicao de senha, convite e regua de cobranca, e
        /// leva semanas para recuperar.
        /// </summary>
        public int GlobalHourlyVerificationEmailLimit { get; set; } = 60;

        /// <summary>
        /// Audiences provisionadas no cadastro. O Mainstay precisa de agency-campaign E
        /// integration-platform: o blueprint do AgencyCampaign injeta a chave de API do
        /// IntegrationPlatform (ValueSource TenantApiKey, SourceAudience integration-platform),
        /// e sem o contrato do IP essa chave nao existe — as integracoes nascem mortas.
        /// </summary>
        public List<string> SystemAudiences { get; set; } = ["agency-campaign", "integration-platform"];
    }
}
