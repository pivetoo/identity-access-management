namespace IdentityManagement.Application.Responses.Contracts
{
    public class ContractSummaryResponse
    {
        public long Id { get; set; }

        public long SystemApplicationId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string SystemApplicationName { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsValid { get; set; }

        public int AccessTokenLifetime { get; set; }

        public int RefreshTokenLifetime { get; set; }
    }
}
