using IdentityManagement.Application.Responses.Contracts;

namespace IdentityManagement.Application.Responses.Auth
{
    public class ContractSelectionResponse
    {
        public string AuthenticationStep { get; set; } = "contractSelection";

        public long UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string AuthorizationSessionToken { get; set; } = string.Empty;

        public List<ContractSelectionResponseItem> AvailableContracts { get; set; } = [];
    }
}
