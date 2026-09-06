using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Signup
{
    /// <summary>
    /// Origem do cadastro, lida pela tela a partir da URL (utm_*, gclid, fbclid) e do navegador
    /// (pagina de entrada, referrer). Nada aqui e obrigatorio nem confiavel: serve para atribuicao
    /// de marketing, nunca para decisao de negocio.
    /// </summary>
    public sealed class SignupAttributionRequest
    {
        [StringLength(200)] public string? Source { get; set; }

        [StringLength(200)] public string? Medium { get; set; }

        [StringLength(200)] public string? Campaign { get; set; }

        [StringLength(200)] public string? Content { get; set; }

        [StringLength(200)] public string? Term { get; set; }

        [StringLength(200)] public string? Gclid { get; set; }

        [StringLength(200)] public string? Fbclid { get; set; }

        [StringLength(500)] public string? LandingPage { get; set; }

        [StringLength(500)] public string? Referrer { get; set; }
    }
}
