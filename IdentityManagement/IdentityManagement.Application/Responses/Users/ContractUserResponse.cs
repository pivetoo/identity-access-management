namespace IdentityManagement.Application.Responses.Users
{
    public class ContractUserResponse
    {
        public long UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }

        public long RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public bool IsRoot { get; set; }

        public DateTimeOffset AssignedAt { get; set; }
    }
}
