namespace IdentityManagement.Application.Responses.Auth
{
    public sealed class AdminInvitationInfoResponse
    {
        public string CompanyName { get; set; } = string.Empty;
        public string SystemApplicationName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string[] SystemApplicationNames { get; set; } = Array.Empty<string>();
    }
}
