using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Billing
{
    public class AssignSubscriptionRequest
    {
        [Required]
        public long CompanyId { get; set; }

        [Required]
        public long PlanId { get; set; }
    }
}
