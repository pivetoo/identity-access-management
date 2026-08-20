using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityManagement.Infrastructure.Services
{
    /// <summary>
    /// Cobranca na visao do tenant. Ver <see cref="ITenantBillingService"/> para o porque de tudo
    /// ser enderecado por tenantId.
    /// </summary>
    public sealed class TenantBillingService : ITenantBillingService
    {
        private readonly DbContext dbContext;
        private readonly IBillingGateway billingGateway;
        private readonly ILogger<TenantBillingService> logger;

        public TenantBillingService(DbContext dbContext, IBillingGateway billingGateway, ILogger<TenantBillingService> logger)
        {
            this.dbContext = dbContext;
            this.billingGateway = billingGateway;
            this.logger = logger;
        }

        public async Task<TenantSubscriptionResponse> GetSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            (Company company, Subscription subscription, Plan plan) = await LoadAsync(tenantId, cancellationToken);

            TenantSubscriptionResponse response = Build(company, subscription, plan);
            response.PendingCharge = await LoadPendingChargeAsync(subscription, cancellationToken);

            return response;
        }

        /// <summary>
        /// A cobranca em aberto vem do provedor a cada carregamento (link e QR expiram, e o status
        /// muda fora do nosso banco). Falha aqui NAO derruba a tela: sem a cobranca o cliente ainda
        /// ve plano, status e a opcao de cartao — derrubar tudo por causa do provedor seria pior.
        /// </summary>
        private async Task<TenantPendingChargeResponse?> LoadPendingChargeAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(subscription.ExternalSubscriptionId))
            {
                return null;
            }

            try
            {
                GatewayPendingCharge? charge = await billingGateway.GetPendingChargeAsync(subscription.ExternalSubscriptionId, cancellationToken);
                if (charge is null)
                {
                    return null;
                }

                return new TenantPendingChargeResponse
                {
                    Value = charge.Value,
                    DueDate = charge.DueDate,
                    BillingType = charge.BillingType,
                    Status = charge.Status,
                    InvoiceUrl = charge.InvoiceUrl,
                    PixPayload = charge.PixPayload,
                    PixQrCodeBase64 = charge.PixQrCodeBase64
                };
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Falha ao consultar a cobranca em aberto da assinatura {SubscriptionId} no provedor.",
                    subscription.ExternalSubscriptionId);

                return null;
            }
        }

        public async Task<TenantSubscriptionResponse> UpdateBillingAddressAsync(Guid tenantId, UpdateBillingAddressRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            BillingAddress address = new BillingAddress(
                request.PostalCode,
                request.Street,
                request.Number,
                request.Complement,
                request.District,
                request.City,
                request.State);

            if (!address.IsComplete())
            {
                throw new BusinessRuleException("billing.address.incomplete");
            }

            // AsTracking obrigatorio: o DbContext do Archon e NoTracking por padrao, entao sem isso
            // a entidade volta solta e o endereco nao chega ao banco.
            Company? company = await dbContext.Set<Company>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
            if (company is null)
            {
                throw new NotFoundException("company.notFound");
            }

            company.SetBillingAddress(address);
            await dbContext.SaveChangesAsync(cancellationToken);

            (Company reloaded, Subscription subscription, Plan plan) = await LoadAsync(tenantId, cancellationToken);
            return Build(reloaded, subscription, plan);
        }

        public async Task<TenantCheckoutResponse> StartCardCheckoutAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            (Company company, Subscription subscription, Plan plan) = await LoadAsync(tenantId, cancellationToken);

            if (company.GetBillingAddress() is null)
            {
                // O provedor recusa o checkout sem endereco do pagador. Barrar aqui devolve uma
                // mensagem que a tela sabe tratar, em vez de propagar erro cru do provedor.
                throw new BusinessRuleException("billing.address.required");
            }

            if (subscription.Status == SubscriptionStatus.Canceled)
            {
                throw new BusinessRuleException("billing.subscription.canceled");
            }

            GatewayCheckoutResult checkout = await billingGateway.CreateRecurringCardCheckoutAsync(company.Id, plan.Id, cancellationToken);

            logger.LogInformation(
                "Checkout de cartao aberto para a empresa {CompanyId} no plano {Plan} (checkout {CheckoutId}).",
                company.Id,
                plan.Name,
                checkout.CheckoutId);

            return new TenantCheckoutResponse
            {
                CheckoutUrl = checkout.CheckoutUrl,
                ExpiresAt = checkout.ExpiresAt
            };
        }

        private async Task<(Company Company, Subscription Subscription, Plan Plan)> LoadAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            Company? company = await dbContext.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);

            if (company is null)
            {
                throw new NotFoundException("company.notFound");
            }

            Subscription? subscription = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CompanyId == company.Id, cancellationToken);

            if (subscription is null)
            {
                throw new NotFoundException("subscription.notFound");
            }

            Plan? plan = await dbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == subscription.PlanId, cancellationToken);

            if (plan is null)
            {
                throw new NotFoundException("plan.notFound");
            }

            return (company, subscription, plan);
        }

        private static TenantSubscriptionResponse Build(Company company, Subscription subscription, Plan plan)
        {
            BillingAddress? address = company.GetBillingAddress();

            return new TenantSubscriptionResponse
            {
                PlanName = plan.Name,
                PriceAmount = plan.PriceAmount,
                Currency = plan.Currency,
                BillingPeriod = plan.BillingPeriod.ToString(),
                Status = subscription.Status.ToString(),
                PaymentMethod = subscription.PaymentMethod,
                TrialEndsAt = subscription.TrialEndsAt,
                CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                IsBlocked = subscription.IsBlocked,
                HasBillingAddress = address is not null,
                BillingAddress = address is null ? null : new TenantBillingAddressResponse
                {
                    PostalCode = address.PostalCode,
                    Street = address.Street,
                    Number = address.Number,
                    Complement = address.Complement,
                    District = address.District,
                    City = address.City,
                    State = address.State
                }
            };
        }
    }
}
