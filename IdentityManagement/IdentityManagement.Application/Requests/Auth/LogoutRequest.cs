using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Auth
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
