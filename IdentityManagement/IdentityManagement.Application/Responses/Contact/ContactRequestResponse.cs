namespace IdentityManagement.Application.Responses.Contact
{
    public sealed class ContactRequestResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? CompanyName { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? SourceIp { get; set; }

        public bool NotificationSent { get; set; }

        public DateTimeOffset? HandledAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
