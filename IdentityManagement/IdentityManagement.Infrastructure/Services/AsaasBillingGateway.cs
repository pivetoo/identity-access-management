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
                CpfCnpj = DigitsOnly(company.Document),
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

        public async Task<GatewaySubscriptionResult?> CreateSubscriptionAsync(long companyId, long planId, string? externalCustomerId, CancellationToken ct = default)
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
                Value = plan.PriceAmount,
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

            return new GatewaySubscriptionResult(resp.Data.Id, externalCustomerId);
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

        private string BaseUrl => options.BaseUrl.TrimEnd('/');

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
    }
}
