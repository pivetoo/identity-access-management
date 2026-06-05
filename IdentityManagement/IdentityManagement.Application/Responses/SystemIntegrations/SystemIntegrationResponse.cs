namespace IdentityManagement.Application.Responses.SystemIntegrations
{
    public class SystemIntegrationResponse
    {
        public long Id { get; set; }

        public long SystemApplicationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public List<SystemIntegrationParameterResponse> Parameters { get; set; } = new();
    }
}
