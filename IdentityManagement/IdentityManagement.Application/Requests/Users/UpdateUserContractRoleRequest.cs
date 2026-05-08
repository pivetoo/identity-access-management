using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Users
{
    public class UpdateUserContractRoleRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long RoleId { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long ContractId { get; set; }
    }
}
