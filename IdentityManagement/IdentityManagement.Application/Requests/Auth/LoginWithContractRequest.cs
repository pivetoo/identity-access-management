using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Auth
{
    public class LoginWithContractRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public long ContractId { get; set; }

        [Required]
        public string TemporaryToken { get; set; } = string.Empty;
    }
}
