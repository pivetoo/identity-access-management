using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Contracts
{
    public class CreateContractRequest
    {
        [Required]
        public long CompanyId { get; set; }

        [Required]
        public long SystemApplicationId { get; set; }

        [Required]
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }
    }
}
