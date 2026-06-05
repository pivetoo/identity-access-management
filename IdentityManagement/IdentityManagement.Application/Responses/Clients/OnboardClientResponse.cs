namespace IdentityManagement.Application.Responses.Clients
{
    public sealed class OnboardClientResponse
    {
        public long CompanyId { get; set; }
        public long[] ContractIds { get; set; } = Array.Empty<long>();
        public string[] DatabaseNames { get; set; } = Array.Empty<string>();
    }
}
