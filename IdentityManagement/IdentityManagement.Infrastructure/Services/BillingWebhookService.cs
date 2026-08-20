using System.Globalization;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityManagement.Infrastructure.Services
{
    // Usa DbContext direto (multi-passo: dedupe + upsert do pagamento + transicao + persistencia em sequencia).
    public sealed class BillingWebhookService : IBillingWebhookService
    {
        private readonly DbContext dbContext;
        private readonly IBillingGateway billingGateway;
        private readonly ILogger<BillingWebhookService> logger;

        public BillingWebhookService(DbContext dbContext, IBillingGateway billingGateway, ILogger<BillingWebhookService> logger)
        {
            this.dbContext = dbContext;
            this.billingGateway = billingGateway;
            this.logger = logger;
        }

        public async Task<bool> ProcessAsaasEventAsync(AsaasWebhookPayload payload, string rawPayload = "", CancellationToken ct = default)
        {
            // Idempotencia: se ja processamos este evento, ignora (o Asaas reenvia ate receber 200).
            bool alreadyProcessed = await dbContext.Set<BillingWebhookEvent>()
                .AsNoTracking()
                .AnyAsync(e => e.ExternalEventId == payload.Id, ct);

            if (alreadyProcessed)
            {
                return false;
            }

            // Evento de checkout nao carrega pagamento: trata em caminho proprio, antes do upsert.
            if (payload.Event.StartsWith("CHECKOUT_", StringComparison.Ordinal))
            {
                string checkoutOutcome = await ProcessCheckoutEventAsync(payload, ct);
                RecordEvent(payload, checkoutOutcome, rawPayload);
                await dbContext.SaveChangesAsync(ct);
                return checkoutOutcome.EndsWith(".migrated_to_card", StringComparison.Ordinal);
            }

            PaymentStatus paymentStatus = MapPaymentStatus(payload.Event);
            DateTimeOffset? paidDate = ResolvePaidDate(payload);

            // SEMPRE persiste o pagamento (mesmo sem assinatura conhecida).
            Payment payment = await UpsertPaymentAsync(payload, paymentStatus, paidDate, ct);

            Subscription? subscription = await FindSubscriptionAsync(payload.Payment.Subscription, ct);

            bool applied = false;
            string outcome;

            if (subscription is not null)
            {
                payment.SetSubscription(subscription.Id, subscription.CompanyId, subscription.ExternalSubscriptionId);

                Plan? plan = await dbContext.Set<Plan>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == subscription.PlanId, ct);

                if (plan is null)
                {
                    outcome = BuildOutcome(payload.Event, "ignored.plan_not_found");
                }
                else
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    DateTimeOffset periodEnd = ComputePeriodEnd(now, plan.BillingPeriod);

                    TransitionResult transition = TryApplyTransition(payload.Event, subscription, now, periodEnd);
                    applied = transition.Applied;
                    outcome = BuildOutcome(payload.Event, transition.Outcome);
                }
            }
            else
            {
                outcome = BuildOutcome(payload.Event, "ignored.unknown_subscription");
            }

            RecordEvent(payload, outcome, rawPayload);

            await dbContext.SaveChangesAsync(ct);

            return applied;
        }

        /// <summary>
        /// Checkout pago = o cliente cadastrou cartao. A assinatura recorrente que o provedor acabou
        /// de criar precisa substituir a de PIX, e a antiga precisa ser cancelada — senao a empresa
        /// passa a ser cobrada DUAS vezes por ciclo, uma em cada assinatura.
        ///
        /// O evento nao traz o id da assinatura criada, entao ela e localizada pelo cliente: o
        /// checkout foi aberto vinculado ao mesmo cliente do provedor que ja guardamos.
        /// </summary>
        private async Task<string> ProcessCheckoutEventAsync(AsaasWebhookPayload payload, CancellationToken ct)
        {
            if (!string.Equals(payload.Event, "CHECKOUT_PAID", StringComparison.Ordinal))
            {
                return BuildOutcome(payload.Event, "ignored.not_paid");
            }

            Subscription? subscription = await FindSubscriptionForCheckoutAsync(payload.Checkout, ct);
            if (subscription is null)
            {
                logger.LogWarning(
                    "Checkout pago {CheckoutId} sem assinatura correspondente (customer {Customer}, ref {Reference}).",
                    payload.Checkout?.Id,
                    payload.Checkout?.Customer,
                    payload.Checkout?.ExternalReference);

                return BuildOutcome(payload.Event, "ignored.unknown_subscription");
            }

            string customerId = subscription.ExternalCustomerId ?? string.Empty;
            IReadOnlyList<GatewaySubscriptionSummary> cardSubscriptions = await billingGateway.ListActiveCardSubscriptionsAsync(customerId, ct);

            GatewaySubscriptionSummary? card = cardSubscriptions
                .Where(item => !string.Equals(item.ExternalSubscriptionId, subscription.ExternalSubscriptionId, StringComparison.Ordinal))
                .OrderByDescending(item => item.CreatedAt ?? DateTimeOffset.MinValue)
                .FirstOrDefault();

            if (card is null)
            {
                // Propositalmente NAO registra o evento: sem registro o provedor reenvia, e a
                // proxima tentativa encontra a assinatura. Engolir aqui deixaria a empresa sendo
                // cobrada no cartao com a cobranca de PIX ainda ativa.
                throw new InvalidOperationException(
                    $"Checkout {payload.Checkout?.Id} pago, mas nenhuma assinatura de cartao encontrada para o cliente {customerId}.");
            }

            string? previousSubscriptionId = subscription.ExternalSubscriptionId;

            subscription.LinkGateway("asaas", customerId, card.ExternalSubscriptionId);
            subscription.SetPaymentMethod("CREDIT_CARD");

            if (!string.IsNullOrWhiteSpace(previousSubscriptionId) &&
                !string.Equals(previousSubscriptionId, card.ExternalSubscriptionId, StringComparison.Ordinal))
            {
                try
                {
                    await billingGateway.CancelSubscriptionAsync(previousSubscriptionId, ct);
                }
                catch (Exception exception)
                {
                    // A troca ja aconteceu do nosso lado; falhar aqui faria o provedor reenviar e
                    // reprocessar tudo. Registrar alto e suficiente: sobra uma assinatura de PIX
                    // ativa la, que o suporte cancela na mao.
                    logger.LogError(
                        exception,
                        "Assinatura de cartao {NewSubscription} vinculada, mas o cancelamento da anterior {OldSubscription} falhou. CANCELAR NO PROVEDOR para evitar cobranca dupla.",
                        card.ExternalSubscriptionId,
                        previousSubscriptionId);
                }
            }

            logger.LogInformation(
                "Empresa {CompanyId} migrou para cartao recorrente (assinatura {SubscriptionId}).",
                subscription.CompanyId,
                card.ExternalSubscriptionId);

            return BuildOutcome(payload.Event, "applied.migrated_to_card");
        }

        private async Task<Subscription?> FindSubscriptionForCheckoutAsync(AsaasCheckoutInfo? checkout, CancellationToken ct)
        {
            if (checkout is null)
            {
                return null;
            }

            // externalReference e nosso: "company:{id}". Quando presente, e o caminho mais direto.
            if (!string.IsNullOrWhiteSpace(checkout.ExternalReference) &&
                checkout.ExternalReference.StartsWith("company:", StringComparison.Ordinal) &&
                long.TryParse(checkout.ExternalReference["company:".Length..], out long companyId))
            {
                // AsTracking obrigatorio: o DbContext do Archon e NoTracking por padrao, e sem isso
                // a entidade volta solta — a troca de assinatura seria perdida no SaveChanges.
                Subscription? byCompany = await dbContext.Set<Subscription>()
                    .AsTracking()
                    .FirstOrDefaultAsync(item => item.CompanyId == companyId, ct);

                if (byCompany is not null)
                {
                    return byCompany;
                }
            }

            if (string.IsNullOrWhiteSpace(checkout.Customer))
            {
                return null;
            }

            return await dbContext.Set<Subscription>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.ExternalCustomerId == checkout.Customer, ct);
        }

        private async Task<Payment> UpsertPaymentAsync(
            AsaasWebhookPayload payload,
            PaymentStatus status,
            DateTimeOffset? paidDate,
            CancellationToken ct)
        {
            Payment? payment = await dbContext.Set<Payment>()
                .AsTracking()
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == payload.Payment.Id, ct);

            if (payment is null)
            {
                payment = new Payment(payload.Payment.Id, payload.Payment.Value ?? 0m, status);
                await dbContext.Set<Payment>().AddAsync(payment, ct);
            }

            payment.SetDetails(payload.Payment.BillingType, ParseDate(payload.Payment.DueDate));
            payment.UpdateStatus(status, paidDate);

            return payment;
        }

        private async Task<Subscription?> FindSubscriptionAsync(string? externalSubscriptionId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(externalSubscriptionId))
            {
                return null;
            }

            return await dbContext.Set<Subscription>()
                .AsTracking()
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == externalSubscriptionId, ct);
        }

        private TransitionResult TryApplyTransition(string eventType, Subscription subscription, DateTimeOffset now, DateTimeOffset periodEnd)
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

                        subscription.Unblock();

                        return new TransitionResult(true, "subscription.activated");

                    case "PAYMENT_OVERDUE":
                        subscription.MarkPastDue();
                        return new TransitionResult(true, "subscription.pastdue");

                    case "PAYMENT_REFUNDED":
                    case "PAYMENT_CHARGEBACK_REQUESTED":
                    case "PAYMENT_DELETED":
                        subscription.Suspend();
                        return new TransitionResult(true, "subscription.suspended");

                    default:
                        return new TransitionResult(false, "subscription.unmapped");
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

                return new TransitionResult(false, "transition.invalid");
            }
        }

        private void RecordEvent(AsaasWebhookPayload payload, string outcome, string rawPayload)
        {
            BillingWebhookEvent webhookEvent = new BillingWebhookEvent(
                payload.Id,
                payload.Event,
                DateTimeOffset.UtcNow,
                payload.Payment.Id,
                outcome,
                string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload);

            dbContext.Set<BillingWebhookEvent>().Add(webhookEvent);
        }

        private static PaymentStatus MapPaymentStatus(string eventType)
        {
            switch (eventType)
            {
                case "PAYMENT_CREATED":
                    return PaymentStatus.Pending;
                case "PAYMENT_CONFIRMED":
                    return PaymentStatus.Confirmed;
                case "PAYMENT_RECEIVED":
                    return PaymentStatus.Received;
                case "PAYMENT_OVERDUE":
                    return PaymentStatus.Overdue;
                case "PAYMENT_REFUNDED":
                    return PaymentStatus.Refunded;
                case "PAYMENT_CHARGEBACK_REQUESTED":
                    return PaymentStatus.ChargebackRequested;
                case "PAYMENT_DELETED":
                    return PaymentStatus.Deleted;
                default:
                    return PaymentStatus.Pending;
            }
        }

        private static DateTimeOffset? ResolvePaidDate(AsaasWebhookPayload payload)
        {
            if (payload.Event != "PAYMENT_RECEIVED" && payload.Event != "PAYMENT_CONFIRMED")
            {
                return null;
            }

            return ParseDate(payload.Payment.PaymentDate) ?? ParseDate(payload.Payment.ConfirmedDate);
        }

        private static DateTimeOffset? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string BuildOutcome(string eventType, string detail)
        {
            string paymentPart = eventType switch
            {
                "PAYMENT_CREATED" => "payment.created",
                "PAYMENT_CONFIRMED" => "payment.confirmed",
                "PAYMENT_RECEIVED" => "payment.received",
                "PAYMENT_OVERDUE" => "payment.overdue",
                "PAYMENT_REFUNDED" => "payment.refunded",
                "PAYMENT_CHARGEBACK_REQUESTED" => "payment.chargeback",
                "PAYMENT_DELETED" => "payment.deleted",
                _ => "payment.unmapped"
            };

            return $"{paymentPart}; {detail}";
        }

        private static DateTimeOffset ComputePeriodEnd(DateTimeOffset start, BillingPeriod period)
        {
            if (period == BillingPeriod.Yearly)
            {
                return start.AddYears(1);
            }

            return start.AddMonths(1);
        }

        private readonly record struct TransitionResult(bool Applied, string Outcome);
    }
}
