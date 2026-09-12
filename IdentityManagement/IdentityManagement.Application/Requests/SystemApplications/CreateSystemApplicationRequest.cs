using System.ComponentModel.DataAnnotations;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.SystemApplications
{
    public class CreateSystemApplicationRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Audience { get; set; } = string.Empty;

        public ApplicationType Type { get; set; } = ApplicationType.External;

        /// <summary>A aplicacao entra no convite de administrador do tenant. Desligar para sistemas que sao motor, nao produto.</summary>
        public bool GrantsAdminOnSetup { get; set; } = true;
    }
}
