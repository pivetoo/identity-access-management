namespace IdentityManagement.Application.Responses.Companies
{
    public class CompanyResponse
    {
        public long Id { get; set; }

        public string LegalName { get; set; } = string.Empty;

        public string TradeName { get; set; } = string.Empty;

        public string Document { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public Guid TenantId { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public SignupAttributionResponse? Attribution { get; set; }
    }
}
