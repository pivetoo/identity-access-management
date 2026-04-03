using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Contracts
{
    public class CreateContractRequest
    {
        [Required]
        public long CompanyId { get; set; }

        [Required]
        public long ApplicationId { get; set; }

        [Required]
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public int AccessTokenLifetime { get; set; }

        public int RefreshTokenLifetime { get; set; }
    }
}
