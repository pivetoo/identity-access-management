namespace IdentityManagement.Application.Responses.AccessCapabilities
{
    public class AccessCapabilityResponse
    {
        public string Key { get; set; } = string.Empty;

        public string Module { get; set; } = string.Empty;

        public string ModuleLabel { get; set; } = string.Empty;

        public int ModuleOrder { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Order { get; set; }

        public bool IsBaseline { get; set; }

        public int ResourceCount { get; set; }
    }
}
