namespace IdentityManagement.Application.Responses.Contracts
{
    public class ContractSelectionResponseItem
    {
        public long ContractId { get; set; }

        public string ApplicationName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string RedirectUris { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;
    }
}
