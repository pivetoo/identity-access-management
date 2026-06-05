using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityManagement.Infrastructure.Services
{
    // Implementacao real do gateway de cobranca usando a API do Asaas.
    // O HttpClient e configurado no DI com BaseAddress e header access_token.
    public sealed class AsaasBillingGateway : IBillingGateway
    {
        private readonly HttpClient httpClient;
        private readonly DbContext dbContext;
        private readonly AsaasOptions options;

        public AsaasBillingGateway(HttpClient httpClient, DbContext dbContext, IOptions<AsaasOptions> options)
        {
            this.httpClient = httpClient;
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

            AsaasCustomerRequest request = new AsaasCustomerRequest
            {
                Name = name,
                CpfCnpj = DigitsOnly(company.Document),
                Email = company.Email,
                MobilePhone = DigitsOnly(company.PhoneNumber)
            };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("customers", request, ct);
            response.EnsureSuccessStatusCode();

            AsaasCustomerResponse? created = await response.Content.ReadFromJsonAsync<AsaasCustomerResponse>(ct);
            return created?.Id;
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

            AsaasSubscriptionRequest request = new AsaasSubscriptionRequest
            {
                Customer = externalCustomerId,
                BillingType = options.BillingType,
                Value = plan.PriceAmount,
                Cycle = cycle,
                NextDueDate = nextDueDate,
                Description = plan.Name
            };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("subscriptions", request, ct);
            response.EnsureSuccessStatusCode();

            AsaasSubscriptionResponse? created = await response.Content.ReadFromJsonAsync<AsaasSubscriptionResponse>(ct);

            if (created?.Id is null)
            {
                throw new InvalidOperationException("asaas.subscription.missingId");
            }

            return new GatewaySubscriptionResult(created.Id, externalCustomerId);
        }

        public async Task CancelSubscriptionAsync(string externalSubscriptionId, CancellationToken ct = default)
        {
            HttpResponseMessage response = await httpClient.DeleteAsync($"subscriptions/{externalSubscriptionId}", ct);

            // 404 = assinatura ja removida no Asaas; tratamos como sucesso idempotente.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            response.EnsureSuccessStatusCode();
        }

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
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("cpfCnpj")]
            public string CpfCnpj { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("mobilePhone")]
            public string? MobilePhone { get; set; }
        }

        private sealed class AsaasCustomerResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class AsaasSubscriptionRequest
        {
            [JsonPropertyName("customer")]
            public string? Customer { get; set; }

            [JsonPropertyName("billingType")]
            public string BillingType { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public decimal Value { get; set; }

            [JsonPropertyName("cycle")]
            public string Cycle { get; set; } = string.Empty;

            [JsonPropertyName("nextDueDate")]
            public string NextDueDate { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;
        }

        private sealed class AsaasSubscriptionResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }
    }
}
