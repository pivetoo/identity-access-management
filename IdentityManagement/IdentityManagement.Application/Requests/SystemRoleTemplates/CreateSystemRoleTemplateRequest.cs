using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.SystemRoleTemplates
{
    public class CreateSystemRoleTemplateRequest
    {
        [Required]
        public long SystemApplicationId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }

        public List<long> AccessResourceIds { get; set; } = [];

        // Nulo = nao mexer (clientes antigos que so mandam recursos); lista vazia = limpar.
        public List<string>? CapabilityKeys { get; set; }
    }
}
