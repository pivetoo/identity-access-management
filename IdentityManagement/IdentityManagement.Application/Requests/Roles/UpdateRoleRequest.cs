using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Roles
{
    public class UpdateRoleRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }
    }
}
