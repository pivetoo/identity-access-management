namespace IdentityManagement.Application.Responses.Billing
{
    // Perfil cadastral da empresa na visao do proprio tenant: usado pelos sistemas consumidores para
    // pre-popular a configuracao da agencia no provisionamento (nome, documento, contato).
    public sealed class TenantCompanyProfileResponse
    {
        public string LegalName { get; init; } = string.Empty;

        public string TradeName { get; init; } = string.Empty;

        public string Document { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public string? BillingPostalCode { get; init; }

        public string? BillingStreet { get; init; }

        public string? BillingNumber { get; init; }

        public string? BillingComplement { get; init; }

        public string? BillingDistrict { get; init; }

        public string? BillingCity { get; init; }

        public string? BillingState { get; init; }
    }
}
