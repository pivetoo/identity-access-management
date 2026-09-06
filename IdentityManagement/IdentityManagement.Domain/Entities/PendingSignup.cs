using Archon.Core.Entities;
using IdentityManagement.Domain.Security;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    /// <summary>
    /// Cadastro publico aguardando confirmacao do e-mail.
    ///
    /// Existe para que o provisionamento — empresa, contratos e DOIS bancos no Postgres — so rode
    /// depois que alguem provou controlar a caixa de entrada. Antes disso o custo era pago na
    /// primeira chamada anonima, com duas consequencias:
    ///
    /// 1. e-mail digitado errado deixava a agencia trancada do lado de fora, com o CNPJ ja ocupado
    ///    por uma conta fantasma — a saida era apagar os bancos na mao;
    /// 2. o rate limit por IP nao segura ataque distribuido, e o Postgres roda no HOST compartilhado
    ///    por todos os sistemas: encher o disco por aqui derrubava tudo, nao so o cadastro.
    ///
    /// Uma linha aqui e barata e podavel. Um banco de tenant nao e.
    /// </summary>
    public class PendingSignup : Entity
    {
        public string LegalName { get; private set; } = string.Empty;

        public string TradeName { get; private set; } = string.Empty;

        /// <summary>CNPJ ja normalizado (so digitos), igual ao que vai para <c>companies.document</c>.</summary>
        public string Document { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string? PhoneNumber { get; private set; }

        public bool Annual { get; private set; }

        /// <summary>Hash do token de confirmacao. O valor em claro so existe no link do e-mail.</summary>
        public string Token { get; private set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; private set; }

        /// <summary>
        /// Marcado ANTES de provisionar, nao depois. E a trava contra clique duplo: quem conseguir
        /// gravar isto primeiro e o unico que provisiona. Liberado de volta se o provisionamento
        /// falhar, para a pessoa poder tentar de novo pelo mesmo link.
        /// </summary>
        public DateTimeOffset? ConsumedAt { get; private set; }

        /// <summary>Preenchido so quando o provisionamento concluiu. Serve de trilha, nao de trava.</summary>
        public long? CompanyId { get; private set; }

        public string? SourceIp { get; private set; }

        // Origem do cadastro (utm_*, gclid, fbclid, pagina de entrada, referrer). Copiada para a
        // empresa na confirmacao. Ver SignupAttribution.

        public string? UtmSource { get; private set; }

        public string? UtmMedium { get; private set; }

        public string? UtmCampaign { get; private set; }

        public string? UtmContent { get; private set; }

        public string? UtmTerm { get; private set; }

        public string? Gclid { get; private set; }

        public string? Fbclid { get; private set; }

        public string? LandingPage { get; private set; }

        public string? Referrer { get; private set; }

        private PendingSignup() { }

        public PendingSignup(
            string legalName,
            string tradeName,
            string document,
            string email,
            string? phoneNumber,
            bool annual,
            string token,
            DateTimeOffset expiresAt,
            string? sourceIp,
            SignupAttribution? attribution = null)
        {
            LegalName = legalName;
            TradeName = tradeName;
            Document = document;
            Email = email;
            PhoneNumber = phoneNumber;
            Annual = annual;
            Token = TokenHasher.Hash(token);
            ExpiresAt = expiresAt;
            SourceIp = sourceIp;
            ApplyAttribution(attribution);
        }

        public SignupAttribution? GetAttribution()
        {
            SignupAttribution attribution = new SignupAttribution(UtmSource, UtmMedium, UtmCampaign, UtmContent, UtmTerm, Gclid, Fbclid, LandingPage, Referrer);
            return attribution.IsEmpty ? null : attribution;
        }

        private void ApplyAttribution(SignupAttribution? attribution)
        {
            if (attribution is null || attribution.IsEmpty)
            {
                return;
            }

            UtmSource = attribution.Source;
            UtmMedium = attribution.Medium;
            UtmCampaign = attribution.Campaign;
            UtmContent = attribution.Content;
            UtmTerm = attribution.Term;
            Gclid = attribution.Gclid;
            Fbclid = attribution.Fbclid;
            LandingPage = attribution.LandingPage;
            Referrer = attribution.Referrer;
        }

        public bool IsValid() => ConsumedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
    }
}
