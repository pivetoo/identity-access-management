using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Billing
{
    public class ChangePlanRequest
    {
        [Required]
        public long PlanId { get; set; }
    }
}
