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

        private BillingWebhookEvent()
        {
        }

        public BillingWebhookEvent(string externalEventId, string eventType, DateTimeOffset processedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);

            ExternalEventId = externalEventId.Trim();
            EventType = eventType?.Trim() ?? string.Empty;
            ProcessedAt = processedAt;
        }
    }
}
