using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class Company : Entity
    {
        private readonly List<Contract> contracts = [];

        public string LegalName { get; private set; } = string.Empty;

        public string TradeName { get; private set; } = string.Empty;

        public string Document { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string PhoneNumber { get; private set; } = string.Empty;

        public Guid TenantId { get; private set; }

        public bool IsActive { get; private set; } = true;

        // Endereco de cobranca. Fica nulo ate a empresa ir pagar: o cadastro publico nao pede
        // endereco de proposito (atrito na porta de entrada), e o PIX por cobranca nao precisa.
        // Quem exige e o checkout recorrente de cartao do Asaas.
        public string? BillingPostalCode { get; private set; }

        public string? BillingStreet { get; private set; }

        public string? BillingNumber { get; private set; }

        public string? BillingComplement { get; private set; }

        public string? BillingDistrict { get; private set; }

        public string? BillingCity { get; private set; }

        public string? BillingState { get; private set; }

        // Origem do cadastro publico que criou a empresa. Nulo para empresas cadastradas pelo admin.
        // Ver SignupAttribution.

        public string? UtmSource { get; private set; }

        public string? UtmMedium { get; private set; }

        public string? UtmCampaign { get; private set; }

        public string? UtmContent { get; private set; }

        public string? UtmTerm { get; private set; }

        public string? Gclid { get; private set; }

        public string? Fbclid { get; private set; }

        public string? LandingPage { get; private set; }

        public string? Referrer { get; private set; }

        public IReadOnlyCollection<Contract> Contracts => contracts.AsReadOnly();

        private Company()
        {
        }

        public Company(string legalName, string tradeName, string document, string email, string phoneNumber)
        {
            SetNames(legalName, tradeName);
            Document = NormalizeDocument(document);
            Email = email.Trim();
            PhoneNumber = phoneNumber.Trim();
            TenantId = Guid.NewGuid();
        }

        public void Update(string legalName, string tradeName, string document, string email, string phoneNumber, bool isActive)
        {
            SetNames(legalName, tradeName);
            Document = NormalizeDocument(document);
            Email = email.Trim();
            PhoneNumber = phoneNumber.Trim();
            IsActive = isActive;
        }

        // O telefone e opcional no cadastro publico, mas o provedor de pagamento exige para vincular
        // o cliente ao checkout de cartao. Por isso ele pode ser completado depois, sozinho.
        public void SetContactPhone(string phoneNumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
            PhoneNumber = phoneNumber.Trim();
        }

        public void SetSignupAttribution(SignupAttribution attribution)
        {
            ArgumentNullException.ThrowIfNull(attribution);

            if (attribution.IsEmpty)
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

        public SignupAttribution? GetSignupAttribution()
        {
            SignupAttribution attribution = new SignupAttribution(UtmSource, UtmMedium, UtmCampaign, UtmContent, UtmTerm, Gclid, Fbclid, LandingPage, Referrer);
            return attribution.IsEmpty ? null : attribution;
        }

        public void SetBillingAddress(BillingAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            BillingPostalCode = BillingAddress.NormalizePostalCode(address.PostalCode);
            BillingStreet = address.Street.Trim();
            BillingNumber = address.Number.Trim();
            BillingComplement = string.IsNullOrWhiteSpace(address.Complement) ? null : address.Complement.Trim();
            BillingDistrict = address.District.Trim();
            BillingCity = address.City.Trim();
            BillingState = address.State.Trim().ToUpperInvariant();
        }

        public BillingAddress? GetBillingAddress()
        {
            if (string.IsNullOrWhiteSpace(BillingPostalCode))
            {
                return null;
            }

            return new BillingAddress(
                BillingPostalCode,
                BillingStreet ?? string.Empty,
                BillingNumber ?? string.Empty,
                BillingComplement,
                BillingDistrict ?? string.Empty,
                BillingCity ?? string.Empty,
                BillingState ?? string.Empty);
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Documento sempre so com digitos.
        ///
        /// O cadastro publico normaliza antes de comparar, mas o console de administracao gravava o
        /// que o usuario digitasse — inclusive com mascara. Com formatos diferentes na mesma coluna,
        /// a checagem de CNPJ duplicado do signup nao enxerga a empresa existente, e o indice unico
        /// tambem nao: "64.224.591/0001-63" e "64224591000163" sao valores distintos para o banco.
        /// </summary>
        private static string NormalizeDocument(string document)
        {
            // Delega para o Cnpj: manter duas normalizacoes era como o CNPJ alfanumerico entrava
            // aqui e perdia as letras em silencio, gravando um documento diferente do informado.
            return Cnpj.Normalize(document);
        }

        private void SetNames(string legalName, string tradeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
            ArgumentException.ThrowIfNullOrWhiteSpace(tradeName);

            LegalName = legalName.Trim();
            TradeName = tradeName.Trim();
        }
    }
}
