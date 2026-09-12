using Archon.Infrastructure.RestApi;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rest = Archon.Infrastructure.RestApi.RestApi;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AsaasBillingGateway : IBillingGateway
    {
        private readonly Rest restApi;
        private readonly DbContext dbContext;
        private readonly AsaasOptions options;

        public AsaasBillingGateway(Rest restApi, DbContext dbContext, IOptions<AsaasOptions> options)
        {
            this.restApi = restApi;
            this.dbContext = dbContext;
            this.options = options.Value;
        }

        public async Task<string?> CreateCustomerAsync(long companyId, CancellationToken ct = default)
        {
            Company? company = await dbContext.Set<Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId, ct);

            if (company is null)
            {
                throw new InvalidOperationException("company.notFound");
            }

            string name = string.IsNullOrWhiteSpace(company.TradeName) ? company.LegalName : company.TradeName;

            AsaasCustomerRequest body = new AsaasCustomerRequest
            {
                Name = name,
                CpfCnpj = GatewayDocument(company.Document),
                Email = company.Email,
                MobilePhone = DigitsOnly(company.PhoneNumber)
            };

            string url = $"{BaseUrl}/customers";
            RestResponse<AsaasCustomerResponse> resp = await restApi.Fetch<AsaasCustomerResponse>(
                RestRequest.Post(url, body).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok)
            {
                throw new InvalidOperationException(
                    $"Asaas CreateCustomer falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }

            return resp.Data?.Id;
        }

        public async Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, decimal priceAmount, string? externalCustomerId, CancellationToken ct = default)
        {
            Plan? plan = await dbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId, ct);

            if (plan is null)
            {
                throw new InvalidOperationException("plan.notFound");
            }

            string cycle = plan.BillingPeriod == BillingPeriod.Yearly ? "YEARLY" : "MONTHLY";
            string nextDueDate = DateTime.UtcNow.Date.AddDays(plan.TrialDays).ToString("yyyy-MM-dd");

            AsaasSubscriptionRequest body = new AsaasSubscriptionRequest
            {
                Customer = externalCustomerId,
                BillingType = options.BillingType,
                Value = priceAmount,
                Cycle = cycle,
                NextDueDate = nextDueDate,
                Description = plan.Name
            };

            string url = $"{BaseUrl}/subscriptions";
            RestResponse<AsaasSubscriptionResponse> resp = await restApi.Fetch<AsaasSubscriptionResponse>(
                RestRequest.Post(url, body).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok)
            {
                throw new InvalidOperationException(
                    $"Asaas CreateSubscription falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }

            if (resp.Data?.Id is null)
            {
                throw new InvalidOperationException("asaas.subscription.missingId");
            }

            return new GatewaySubscriptionResult(resp.Data.Id, externalCustomerId, options.BillingType);
        }

        public async Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            string url = $"{BaseUrl}/subscriptions/{externalSubscriptionId}";
            RestResponse<object> resp = await restApi.Fetch<object>(
                RestRequest.Delete(url).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            // 404 = assinatura ja removida no Asaas; tratamos como sucesso idempotente.
            if (resp.Status == 404)
            {
                return;
            }

            if (!resp.Ok)
            {
                throw new InvalidOperationException(
                    $"Asaas CancelSubscription falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }
        }

        public async Task<GatewayCheckoutResult> CreateRecurringCardCheckoutAsync(long companyId, long planId, decimal priceAmount, CancellationToken ct = default)
        {
            Company company = await dbContext.Set<Company>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct)
                ?? throw new InvalidOperationException("company.notFound");

            Plan plan = await dbContext.Set<Plan>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct)
                ?? throw new InvalidOperationException("plan.notFound");

            BillingAddress address = company.GetBillingAddress()
                ?? throw new InvalidOperationException("company.billingAddress.missing");

            if (string.IsNullOrWhiteSpace(options.CheckoutSuccessUrl))
            {
                throw new InvalidOperationException("asaas.checkout.callbackNotConfigured");
            }

            string cycle = plan.BillingPeriod == BillingPeriod.Yearly ? "YEARLY" : "MONTHLY";

            // Primeira cobranca do cartao no fim do periodo ja pago. Cobrar hoje seria cobrar duas
            // vezes o mesmo mes de quem esta migrando do PIX no meio do ciclo.
            Subscription? current = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CompanyId == companyId, ct);

            string customerId = current?.ExternalCustomerId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(customerId))
            {
                customerId = await CreateCustomerAsync(companyId, ct) ?? throw new InvalidOperationException("asaas.customer.missingId");
            }

            // O provedor so aceita vincular um cliente ao checkout se o cadastro dele estiver
            // completo (endereco inclusive). Como o endereco chega depois do cadastro, o cliente
            // precisa ser atualizado agora.
            await UpdateCustomerAddressAsync(customerId, company, address, ct);

            DateTime nextDue = (current?.CurrentPeriodEnd ?? DateTimeOffset.UtcNow).UtcDateTime.Date;
            if (nextDue <= DateTime.UtcNow.Date)
            {
                nextDue = DateTime.UtcNow.Date.AddDays(1);
            }

            AsaasCheckoutRequest body = new AsaasCheckoutRequest
            {
                BillingTypes = ["CREDIT_CARD"],
                ChargeTypes = ["RECURRENT"],
                MinutesToExpire = options.CheckoutMinutesToExpire,
                ExternalReference = $"company:{companyId}",
                Items =
                [
                    new AsaasCheckoutItem
                    {
                        Name = $"Mainstay {plan.Name}",
                        Quantity = 1,
                        Value = priceAmount
                    }
                ],
                Subscription = new AsaasCheckoutSubscription
                {
                    Cycle = cycle,
                    NextDueDate = nextDue.ToString("yyyy-MM-dd")
                },
                // Vincula o cliente que JA temos, em vez de mandar customerData. E o que torna a
                // correlacao possivel depois: a assinatura que o checkout criar nasce sob este
                // mesmo cliente, entao o webhook consegue encontra-la consultando por ele.
                // Mandar customerData criaria um cliente novo e a assinatura ficaria orfa.
                Customer = customerId,
                Callback = new AsaasCheckoutCallback
                {
                    SuccessUrl = options.CheckoutSuccessUrl,
                    CancelUrl = string.IsNullOrWhiteSpace(options.CheckoutCancelUrl) ? options.CheckoutSuccessUrl : options.CheckoutCancelUrl,
                    ExpiredUrl = string.IsNullOrWhiteSpace(options.CheckoutExpiredUrl) ? options.CheckoutSuccessUrl : options.CheckoutExpiredUrl
                }
            };

            RestResponse<AsaasCheckoutResponse> resp = await restApi.Fetch<AsaasCheckoutResponse>(
                RestRequest.Post($"{BaseUrl}/checkouts", body).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok || resp.Data?.Id is null || string.IsNullOrWhiteSpace(resp.Data.Link))
            {
                throw new InvalidOperationException(
                    $"Asaas CreateCheckout falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }

            return new GatewayCheckoutResult(
                resp.Data.Id,
                resp.Data.Link,
                DateTimeOffset.UtcNow.AddMinutes(options.CheckoutMinutesToExpire));
        }

        private async Task UpdateCustomerAddressAsync(string customerId, Company company, BillingAddress address, CancellationToken ct)
        {
            AsaasCustomerRequest body = new AsaasCustomerRequest
            {
                Name = string.IsNullOrWhiteSpace(company.TradeName) ? company.LegalName : company.TradeName,
                CpfCnpj = GatewayDocument(company.Document),
                Email = company.Email,
                // O provedor exige `phone` (nao so `mobilePhone`) para vincular o cliente ao
                // checkout. Mandar os dois evita depender de qual deles ele valida.
                Phone = DigitsOnly(company.PhoneNumber),
                MobilePhone = DigitsOnly(company.PhoneNumber),
                PostalCode = address.PostalCode,
                Address = address.Street,
                AddressNumber = address.Number,
                Complement = address.Complement,
                Province = address.District
            };

            RestResponse<AsaasCustomerResponse> resp = await restApi.Fetch<AsaasCustomerResponse>(
                RestRequest.Post($"{BaseUrl}/customers/{customerId}", body).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok)
            {
                throw new InvalidOperationException(
                    $"Asaas UpdateCustomer falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }
        }

        public async Task<IReadOnlyList<GatewaySubscriptionSummary>> ListActiveCardSubscriptionsAsync(string externalCustomerId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(externalCustomerId))
            {
                return [];
            }

            string url = $"{BaseUrl}/subscriptions?customer={Uri.EscapeDataString(externalCustomerId)}&status=ACTIVE&limit=100";
            RestResponse<AsaasSubscriptionListResponse> resp = await restApi.Fetch<AsaasSubscriptionListResponse>(
                RestRequest.Get(url).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok || resp.Data?.Data is null)
            {
                throw new InvalidOperationException(
                    $"Asaas ListSubscriptions falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }

            return resp.Data.Data
                .Where(item => !item.Deleted && item.Id is not null && string.Equals(item.BillingType, "CREDIT_CARD", StringComparison.OrdinalIgnoreCase))
                .Select(item => new GatewaySubscriptionSummary(item.Id!, item.BillingType ?? string.Empty, item.Status ?? string.Empty, item.DateCreated))
                .ToList();
        }

        public async Task<GatewayPendingCharge?> GetPendingChargeAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(externalSubscriptionId))
            {
                return null;
            }

            string url = $"{BaseUrl}/subscriptions/{externalSubscriptionId}/payments?limit=20";
            RestResponse<AsaasPaymentListResponse> resp = await restApi.Fetch<AsaasPaymentListResponse>(
                RestRequest.Get(url).WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            if (!resp.Ok || resp.Data?.Data is null)
            {
                throw new InvalidOperationException(
                    $"Asaas ListSubscriptionPayments falhou ({resp.Status}): {string.Join("; ", resp.Errors)}");
            }

            // A mais antiga em aberto primeiro: e a que vence antes e a que trava o acesso.
            AsaasPaymentListItem? pending = resp.Data.Data
                .Where(item => item.Id is not null && OpenStatuses.Contains(item.Status ?? string.Empty))
                .OrderBy(item => item.DueDate ?? DateTimeOffset.MaxValue)
                .FirstOrDefault();

            if (pending is null)
            {
                return null;
            }

            string? pixPayload = null;
            string? pixImage = null;

            if (string.Equals(pending.BillingType, "PIX", StringComparison.OrdinalIgnoreCase))
            {
                (pixPayload, pixImage) = await GetPixQrCodeAsync(pending.Id!, ct);
            }

            return new GatewayPendingCharge(
                pending.Id!,
                pending.Value,
                pending.DueDate,
                pending.BillingType ?? string.Empty,
                pending.Status ?? string.Empty,
                pending.InvoiceUrl,
                pixPayload,
                pixImage);
        }

        private async Task<(string? Payload, string? EncodedImage)> GetPixQrCodeAsync(string paymentId, CancellationToken ct)
        {
            RestResponse<AsaasPixQrCodeResponse> resp = await restApi.Fetch<AsaasPixQrCodeResponse>(
                RestRequest.Get($"{BaseUrl}/payments/{paymentId}/pixQrCode").WithHeader("access_token", options.ApiKey).WithHeader("User-Agent", "Mainstay-IdM"), ct);

            // QR indisponivel nao invalida a cobranca: o link da fatura continua servindo.
            if (!resp.Ok || resp.Data?.Success != true)
            {
                return (null, null);
            }

            return (resp.Data.Payload, resp.Data.EncodedImage);
        }

        private string BaseUrl => options.BaseUrl.TrimEnd('/');

        private static readonly string[] OpenStatuses = ["PENDING", "OVERDUE", "AWAITING_RISK_ANALYSIS"];

        /// <summary>
        /// Documento como o provedor deve receber: sem mascara e com as LETRAS preservadas.
        ///
        /// Delega para o <see cref="Cnpj"/> em vez de manter regra propria. Manter uma regra aqui foi
        /// o bug: <c>DigitsOnly</c> aplicado a um CNPJ alfanumerico devolvia so os digitos soltos, o
        /// provedor recusava o cliente com "CPF/CNPJ invalido" e o tenant nascia sem assinatura.
        ///
        /// Serve tambem a CPF, que nao tem letra: o efeito ali continua sendo tirar a mascara.
        /// </summary>
        public static string GatewayDocument(string? document)
        {
            return Cnpj.Normalize(document);
        }

        /// <summary>Telefone. Aqui descartar o que nao e digito E o certo — nao vale para documento.</summary>
        private static string DigitsOnly(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        private sealed class AsaasCustomerRequest
        {
            public string Name { get; set; } = string.Empty;

            public string CpfCnpj { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string? MobilePhone { get; set; }

            public string? Phone { get; set; }

            public string? PostalCode { get; set; }

            public string? Address { get; set; }

            public string? AddressNumber { get; set; }

            public string? Complement { get; set; }

            public string? Province { get; set; }
        }

        private sealed class AsaasCustomerResponse
        {
            public string? Id { get; set; }
        }

        private sealed class AsaasSubscriptionRequest
        {
            public string? Customer { get; set; }

            public string BillingType { get; set; } = string.Empty;

            public decimal Value { get; set; }

            public string Cycle { get; set; } = string.Empty;

            public string NextDueDate { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;
        }

        private sealed class AsaasSubscriptionResponse
        {
            public string? Id { get; set; }
        }

        private sealed class AsaasSubscriptionListResponse
        {
            public List<AsaasSubscriptionListItem>? Data { get; set; }
        }

        private sealed class AsaasSubscriptionListItem
        {
            public string? Id { get; set; }

            public string? BillingType { get; set; }

            public string? Status { get; set; }

            public bool Deleted { get; set; }

            public DateTimeOffset? DateCreated { get; set; }
        }

        private sealed class AsaasCheckoutRequest
        {
            public List<string> BillingTypes { get; set; } = [];

            public List<string> ChargeTypes { get; set; } = [];

            public int MinutesToExpire { get; set; }

            public string? ExternalReference { get; set; }

            public List<AsaasCheckoutItem> Items { get; set; } = [];

            public AsaasCheckoutSubscription? Subscription { get; set; }

            public string? Customer { get; set; }

            public AsaasCheckoutCallback? Callback { get; set; }
        }

        private sealed class AsaasCheckoutItem
        {
            public string Name { get; set; } = string.Empty;

            public int Quantity { get; set; }

            public decimal Value { get; set; }
        }

        private sealed class AsaasCheckoutSubscription
        {
            public string Cycle { get; set; } = string.Empty;

            public string NextDueDate { get; set; } = string.Empty;
        }


        private sealed class AsaasCheckoutCallback
        {
            public string SuccessUrl { get; set; } = string.Empty;

            public string CancelUrl { get; set; } = string.Empty;

            public string ExpiredUrl { get; set; } = string.Empty;
        }

        private sealed class AsaasPaymentListResponse
        {
            public List<AsaasPaymentListItem>? Data { get; set; }
        }

        private sealed class AsaasPaymentListItem
        {
            public string? Id { get; set; }

            public string? Status { get; set; }

            public decimal Value { get; set; }

            public DateTimeOffset? DueDate { get; set; }

            public string? BillingType { get; set; }

            public string? InvoiceUrl { get; set; }
        }

        private sealed class AsaasPixQrCodeResponse
        {
            public bool Success { get; set; }

            public string? Payload { get; set; }

            public string? EncodedImage { get; set; }
        }

        private sealed class AsaasCheckoutResponse
        {
            public string? Id { get; set; }

            public string? Link { get; set; }
        }
    }
}
