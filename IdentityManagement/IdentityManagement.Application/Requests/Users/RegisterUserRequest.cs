using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Users
{
    public class RegisterUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(150, MinimumLength = 5)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}
