using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Auth
{
    public class ChangePasswordRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
