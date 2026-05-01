using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Auth
{
    public class IdentifyUserRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? AuthorizeUrl { get; set; }
    }
}
