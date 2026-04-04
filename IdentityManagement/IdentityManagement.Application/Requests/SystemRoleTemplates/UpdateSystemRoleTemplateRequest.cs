using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.SystemRoleTemplates
{
    public class UpdateSystemRoleTemplateRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        public List<long> AccessResourceIds { get; set; } = [];
    }
}
