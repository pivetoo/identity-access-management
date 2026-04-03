using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
