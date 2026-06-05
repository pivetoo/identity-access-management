using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    // Registro de evento de webhook ja processado, para garantir idempotencia
    // (o Asaas reenvia eventos ate receber 200).
    public class BillingWebhookEvent : Entity
    {
        public string ExternalEventId { get; private set; } = string.Empty;

        public string EventType { get; private set; } = string.Empty;

        public DateTimeOffset ProcessedAt { get; private set; }

        public string? ExternalPaymentId { get; private set; }

        public string? Outcome { get; private set; }

        public string? RawPayload { get; private set; }

        private BillingWebhookEvent()
        {
        }

        public BillingWebhookEvent(
            string externalEventId,
            string eventType,
            DateTimeOffset processedAt,
            string? externalPaymentId = null,
            string? outcome = null,
            string? rawPayload = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);

            ExternalEventId = externalEventId.Trim();
            EventType = eventType?.Trim() ?? string.Empty;
            ProcessedAt = processedAt;
            ExternalPaymentId = externalPaymentId?.Trim();
            Outcome = outcome?.Trim();
            RawPayload = rawPayload;
        }
    }
}
