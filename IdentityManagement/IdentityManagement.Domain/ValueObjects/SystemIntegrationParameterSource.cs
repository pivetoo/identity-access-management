namespace IdentityManagement.Domain.ValueObjects
{
    public enum SystemIntegrationParameterSource
    {
        Static = 0,
        TenantApiKey = 1,
        TenantId = 2,
        GeneratedSecret = 3
    }
}
