namespace IdentityManagement.Application.Responses.AccessResources
{
    public class AccessResourceResponse
    {
        public long Id { get; set; }

        public long SystemApplicationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public string Controller { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = string.Empty;

        public string Route { get; set; } = string.Empty;

        public IReadOnlyCollection<string> Capabilities { get; set; } = [];
    }
}
