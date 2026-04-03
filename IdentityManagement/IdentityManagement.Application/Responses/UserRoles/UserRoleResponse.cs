namespace IdentityManagement.Application.Responses.UserRoles
{
    public class UserRoleResponse
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public long RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public long ContractId { get; set; }

        public bool IsRoot { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public DateTimeOffset AssignedAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
