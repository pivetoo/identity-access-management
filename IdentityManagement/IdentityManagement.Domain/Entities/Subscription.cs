using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class Subscription : Entity
    {
        public long CompanyId { get; private set; }

        public long PlanId { get; private set; }

        public SubscriptionStatus Status { get; private set; }

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset? TrialEndsAt { get; private set; }

        public DateTimeOffset CurrentPeriodStart { get; private set; }

        public DateTimeOffset CurrentPeriodEnd { get; private set; }

        public DateTimeOffset? CanceledAt { get; private set; }

        public string? ExternalCustomerId { get; private set; }

        public string? ExternalSubscriptionId { get; private set; }

        public string? ProviderName { get; private set; }

        // Forma de pagamento vigente no provedor: PIX (cobranca por ciclo, paga na mao) ou
        // CREDIT_CARD (debito automatico). Guardado aqui para a tela nao precisar perguntar ao
        // Asaas a cada carregamento.
        public string? PaymentMethod { get; private set; }

        public bool IsBlocked { get; private set; }

        public SubscriptionBlockReason BlockReason { get; private set; } = SubscriptionBlockReason.None;

        public DateTimeOffset? BlockedAt { get; private set; }

        public Company Company { get; private set; } = null!;

        public Plan Plan { get; private set; } = null!;

        private Subscription()
        {
        }

        private Subscription(long companyId, long planId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(companyId));
            }

            if (planId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(planId));
            }

            CompanyId = companyId;
            PlanId = planId;
        }

        public static Subscription StartTrialing(long companyId, long planId, DateTimeOffset now, int trialDays)
        {
            Subscription subscription = new Subscription(companyId, planId)
            {
                Status = SubscriptionStatus.Trialing,
                StartedAt = now,
                TrialEndsAt = now.AddDays(trialDays),
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddDays(trialDays)
            };

            return subscription;
        }

        public static Subscription StartActive(long companyId, long planId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
        {
            Subscription subscription = new Subscription(companyId, planId)
            {
                Status = SubscriptionStatus.Active,
                StartedAt = periodStart,
                TrialEndsAt = null,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd
            };

            return subscription;
        }

        public void Activate(DateTimeOffset periodStart, DateTimeOffset periodEnd)
        {
            if (Status != SubscriptionStatus.Trialing &&
                Status != SubscriptionStatus.PastDue &&
                Status != SubscriptionStatus.Suspended)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            Status = SubscriptionStatus.Active;
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            CanceledAt = null;
        }

        public void MarkPastDue()
        {
            if (Status != SubscriptionStatus.Active &&
                Status != SubscriptionStatus.Trialing)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            Status = SubscriptionStatus.PastDue;
        }

        public void Suspend()
        {
            if (Status != SubscriptionStatus.Active &&
                Status != SubscriptionStatus.PastDue &&
                Status != SubscriptionStatus.Trialing)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            Status = SubscriptionStatus.Suspended;
        }

        public void Cancel(DateTimeOffset now)
        {
            if (Status == SubscriptionStatus.Canceled)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            Status = SubscriptionStatus.Canceled;
            CanceledAt = now;
        }

        public void Renew(DateTimeOffset periodStart, DateTimeOffset periodEnd)
        {
            if (Status != SubscriptionStatus.Active)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
        }

        public void ChangePlan(long planId)
        {
            if (Status == SubscriptionStatus.Canceled)
            {
                throw new InvalidOperationException("subscription.transition.invalid");
            }

            if (planId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(planId));
            }

            PlanId = planId;
        }

        public void SetPaymentMethod(string? paymentMethod)
        {
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? null : paymentMethod.Trim().ToUpperInvariant();
        }

        public void LinkGateway(string providerName, string externalCustomerId, string externalSubscriptionId)
        {
            ProviderName = providerName;
            ExternalCustomerId = externalCustomerId;
            ExternalSubscriptionId = externalSubscriptionId;
        }

        // Bloqueio recuperavel, ortogonal ao Status (nao inativa a assinatura).
        public void Block(SubscriptionBlockReason reason, DateTimeOffset now)
        {
            if (IsBlocked && BlockReason == reason)
            {
                return;
            }

            IsBlocked = true;
            BlockReason = reason;
            BlockedAt = now;
        }

        public void Unblock()
        {
            IsBlocked = false;
            BlockReason = SubscriptionBlockReason.None;
            BlockedAt = null;
        }

        public bool GrantsAccess(DateTimeOffset now)
        {
            if (Status == SubscriptionStatus.Active)
            {
                return true;
            }

            if (Status == SubscriptionStatus.PastDue)
            {
                return true;
            }

            if (Status == SubscriptionStatus.Trialing)
            {
                return TrialEndsAt.HasValue && now <= TrialEndsAt.Value;
            }

            return false;
        }
    }
}
