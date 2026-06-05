using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityManagement.Infrastructure.Services
{
    // Processa eventos de cobranca do Asaas e aplica as transicoes na Subscription.
    // Usa o DbContext cru (orquestracao multi-passo: dedupe + transicao + persistencia).
    public sealed class BillingWebhookService : IBillingWebhookService
    {
        private readonly DbContext dbContext;
        private readonly ILogger<BillingWebhookService> logger;

        public BillingWebhookService(DbContext dbContext, ILogger<BillingWebhookService> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public async Task<bool> ProcessAsaasEventAsync(AsaasWebhookPayload payload, CancellationToken ct = default)
        {
            // Idempotencia: se ja processamos este evento, ignora (o Asaas reenvia ate receber 200).
            bool alreadyProcessed = await dbContext.Set<BillingWebhookEvent>()
                .AsNoTracking()
                .AnyAsync(e => e.ExternalEventId == payload.Id, ct);

            if (alreadyProcessed)
            {
                return false;
            }

            string? subscriptionId = payload.Payment.Subscription;

            Subscription? subscription = null;
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                subscription = await dbContext.Set<Subscription>()
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId, ct);
            }

            // Assinatura desconhecida: registra o evento para nao reprocessar e ignora.
            if (subscription is null)
            {
                await RecordEventAsync(payload, ct);
                return false;
            }

            Plan? plan = await dbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == subscription.PlanId, ct);

            if (plan is null)
            {
                await RecordEventAsync(payload, ct);
                return false;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset periodEnd = ComputePeriodEnd(now, plan.BillingPeriod);

            bool applied = TryApplyTransition(payload.Event, subscription, now, periodEnd);

            // SEMPRE registra o evento (mesmo ignorado/invalido) para garantir idempotencia.
            await RecordEventAsync(payload, ct);

            return applied;
        }

        private bool TryApplyTransition(string eventType, Subscription subscription, DateTimeOffset now, DateTimeOffset periodEnd)
        {
            try
            {
                switch (eventType)
                {
                    case "PAYMENT_CONFIRMED":
                    case "PAYMENT_RECEIVED":
                        if (subscription.Status == SubscriptionStatus.Active)
                        {
                            subscription.Renew(now, periodEnd);
                        }
                        else
                        {
                            subscription.Activate(now, periodEnd);
                        }

                        return true;

                    case "PAYMENT_OVERDUE":
                        subscription.MarkPastDue();
                        return true;

                    case "PAYMENT_REFUNDED":
                    case "PAYMENT_CHARGEBACK_REQUESTED":
                    case "PAYMENT_DELETED":
                        subscription.Suspend();
                        return true;

                    default:
                        return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Transicao ilegal (ex: Activate vindo de Active, ou qualquer transicao de Canceled).
                // Nao propagar: o Asaas reenviaria o evento indefinidamente em caso de 500.
                logger.LogWarning(
                    ex,
                    "Transicao de assinatura invalida para evento {EventType} na subscription {SubscriptionId} (status {Status}).",
                    eventType,
                    subscription.ExternalSubscriptionId,
                    subscription.Status);

                return false;
            }
        }

        private async Task RecordEventAsync(AsaasWebhookPayload payload, CancellationToken ct)
        {
            BillingWebhookEvent webhookEvent = new BillingWebhookEvent(
                payload.Id,
                payload.Event,
                DateTimeOffset.UtcNow);

            await dbContext.Set<BillingWebhookEvent>().AddAsync(webhookEvent, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        private static DateTimeOffset ComputePeriodEnd(DateTimeOffset start, BillingPeriod period)
        {
            if (period == BillingPeriod.Yearly)
            {
                return start.AddYears(1);
            }

            return start.AddMonths(1);
        }
    }
}
