using System.ComponentModel.DataAnnotations;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.SystemApplications
{
    public class UpdateSystemApplicationRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Audience { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public ApplicationType Type { get; set; } = ApplicationType.External;

        [StringLength(2000)]
        public string? BaseUrl { get; set; }

        /// <summary>
        /// A aplicacao entra no convite de administrador do tenant. Desligar para sistemas que sao
        /// motor, nao produto.
        ///
        /// Nulo mantem o valor atual: ha chamadas que mandam um corpo parcial (a desativacao pelo
        /// backoffice, por exemplo), e um default fixo aqui religaria a flag em silencio.
        /// </summary>
        public bool? GrantsAdminOnSetup { get; set; }
    }
}
