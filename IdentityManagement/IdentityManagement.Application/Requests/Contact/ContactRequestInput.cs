using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Contact
{
    public sealed class ContactRequestInput
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Campo isca, escondido no formulario. Pessoa nao preenche o que nao ve; robo ingenuo
        /// preenche tudo. Vindo com conteudo, a requisicao e descartada em silencio — responder
        /// "recusado" ensinaria o robo a contornar.
        /// </summary>
        [StringLength(200)]
        public string? Website { get; set; }
    }
}
