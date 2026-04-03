using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.UserRoles
{
    public class RevokeUserRoleRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public long RoleId { get; set; }
    }
}
