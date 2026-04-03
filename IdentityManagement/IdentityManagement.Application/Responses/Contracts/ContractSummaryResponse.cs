namespace IdentityManagement.Application.Responses.Contracts
{
    public class ContractSummaryResponse
    {
        public long Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string SystemApplicationName { get; set; } = string.Empty;

        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsValid { get; set; }
    }
}
