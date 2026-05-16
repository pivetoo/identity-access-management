using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Users
{
    public class UpdateUserRequest
    {
        [Required]
        public long Id { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
