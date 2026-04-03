namespace IdentityManagement.Application.Responses.Dashboard
{
    public class ActiveSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;

        public long UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string EmpresaName { get; set; } = string.Empty;

        public string SistemaName { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}
