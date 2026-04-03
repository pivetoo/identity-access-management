namespace IdentityManagement.Application.Responses.Users
{
    public class UserResponse
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
