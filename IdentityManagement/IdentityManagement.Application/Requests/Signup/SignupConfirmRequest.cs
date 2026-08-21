using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Signup
{
    public sealed class SignupConfirmRequest
    {
        /// <summary>Token opaco do link enviado por e-mail. Comparado por hash, nunca em claro.</summary>
        [Required][StringLength(200, MinimumLength = 20)] public string Token { get; set; } = string.Empty;
    }
}
