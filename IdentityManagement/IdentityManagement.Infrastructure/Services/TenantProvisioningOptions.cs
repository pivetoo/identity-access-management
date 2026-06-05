namespace IdentityManagement.Infrastructure.Services
{
    public sealed class TenantProvisioningOptions
    {
        public const string SectionName = "TenantProvisioning";

        public DatabaseCredential? Admin { get; set; }

        public Dictionary<string, DatabaseCredential> SystemCredentials { get; set; } = new();
    }

    public sealed class DatabaseCredential
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
