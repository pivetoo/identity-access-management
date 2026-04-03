using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Contracts
{
    public class UpdateContractRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long CompanyId { get; set; }

        [Required]
        public long SystemApplicationId { get; set; }

        [Required]
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public bool IsActive { get; set; }

        public int AccessTokenLifetime { get; set; }

        public int RefreshTokenLifetime { get; set; }
    }
}
