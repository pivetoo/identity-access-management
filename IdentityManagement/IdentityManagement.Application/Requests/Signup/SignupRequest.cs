using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Signup
{
    /// <summary>
    /// Cadastro publico de agencia. Superficie deliberadamente minima: quem chama NAO escolhe
    /// plano por id nem quais sistemas contratar — os dois vem da configuracao do servidor.
    /// Aceitar isso do cliente deixaria qualquer um se atribuir o plano Interno (gratuito) ou
    /// contratar sistemas que nao fazem parte da oferta.
    /// </summary>
    public sealed class SignupRequest
    {
        [Required][StringLength(200, MinimumLength = 3)] public string LegalName { get; set; } = string.Empty;

        [Required][StringLength(200, MinimumLength = 2)] public string TradeName { get; set; } = string.Empty;

        /// <summary>CNPJ com ou sem mascara. Validado por digito verificador antes de qualquer escrita.</summary>
        [Required][StringLength(18, MinimumLength = 14)] public string Document { get; set; } = string.Empty;

        [Required][EmailAddress][StringLength(200)] public string Email { get; set; } = string.Empty;

        [StringLength(20)] public string? PhoneNumber { get; set; }

        /// <summary>Cobranca anual (dois meses gratis) em vez de mensal.</summary>
        public bool Annual { get; set; }

        /// <summary>Aceite dos termos de uso. Sem aceite explicito nao ha contratacao.</summary>
        public bool AcceptedTerms { get; set; }
    }
}
