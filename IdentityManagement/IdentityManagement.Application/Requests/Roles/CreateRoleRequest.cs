using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Roles
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public long ContractId { get; set; }

        public bool IsRoot { get; set; }

        public bool IsDefault { get; set; }

        public List<long> AccessResourceIds { get; set; } = [];

        // Nulo = nao mexer (clientes antigos que so mandam recursos); lista vazia = limpar.
        public List<string>? CapabilityKeys { get; set; }
    }
}
