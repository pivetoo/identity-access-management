namespace IdentityManagement.Application.Responses.Tenants
{
    public sealed class TenantResolutionResponse
    {
        public string TenantId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ApplicationId { get; set; } = string.Empty;

        public string ConnectionString { get; set; } = string.Empty;

        public int DatabaseProvider { get; set; }

        public string Schema { get; set; } = "public";

        public string IntegrationSecret { get; set; } = string.Empty;
    }
}
