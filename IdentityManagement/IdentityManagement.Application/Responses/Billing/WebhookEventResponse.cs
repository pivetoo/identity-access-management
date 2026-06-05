namespace IdentityManagement.Application.Responses.Billing
{
    public class WebhookEventResponse
    {
        public long Id { get; set; }

        public string ExternalEventId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string? ExternalPaymentId { get; set; }

        public string? Outcome { get; set; }

        public DateTimeOffset ProcessedAt { get; set; }

        public string? RawPayload { get; set; }
    }
}
