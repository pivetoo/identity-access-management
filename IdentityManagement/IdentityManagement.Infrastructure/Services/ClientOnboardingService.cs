using Archon.Core.ValueObjects;
using Archon.Infrastructure.RestApi;
using IdentityManagement.Application.Requests.Billing;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Requests.Tenants;
using IdentityManagement.Application.Responses.Billing;
using IdentityManagement.Application.Responses.Clients;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Rest = Archon.Infrastructure.RestApi.RestApi;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ClientOnboardingService : IClientOnboardingService
    {
        private const string CentralSystemAudience = "identity-management";

        private readonly DbContext dbContext;
        private readonly IContractService contractService;
        private readonly ITenantProvisioner provisioner;
        private readonly IEmailSender emailSender;
        private readonly Rest restApi;
        private readonly ISubscriptionService subscriptionService;
        private readonly ILogger<ClientOnboardingService> logger;

        public ClientOnboardingService(
            DbContext dbContext,
            IContractService contractService,
            ITenantProvisioner provisioner,
            IEmailSender emailSender,
            Rest restApi,
            ISubscriptionService subscriptionService,
            ILogger<ClientOnboardingService> logger)
        {
            this.dbContext = dbContext;
            this.contractService = contractService;
            this.provisioner = provisioner;
            this.emailSender = emailSender;
            this.restApi = restApi;
            this.subscriptionService = subscriptionService;
            this.logger = logger;
        }

        public async Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, bool sendInvitationEmail = true, CancellationToken ct = default)
        {
            List<(string Db, string Audience)> plannedDatabases = new();
            List<long> contractIds = new();
            List<string> systemNames = new();
            List<string> createdDatabases = new();
            List<long> contractedSystemApplicationIds = new();
            Dictionary<string, string> apiKeyByAudience = new();

            Company company = new Company(request.LegalName, request.TradeName, request.Document, request.Email, request.PhoneNumber ?? string.Empty);

            if (request.Attribution is not null)
            {
                company.SetSignupAttribution(SignupAttribution.Normalize(
                    request.Attribution.Source, request.Attribution.Medium, request.Attribution.Campaign, request.Attribution.Content, request.Attribution.Term,
                    request.Attribution.Gclid, request.Attribution.Fbclid, request.Attribution.LandingPage, request.Attribution.Referrer));
            }
            string setupLink = string.Empty;
            string setupToken = string.Empty;

            // A Subscription so e criada DEPOIS que esta transacao confirma: AssignAsync (CrudService)
            // abre transacao propria, e o commit acima ja limpa a CurrentTransaction do EF.
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                dbContext.Set<Company>().Add(company);
                await dbContext.SaveChangesAsync(ct);

                string slug = TenantNaming.Slugify(!string.IsNullOrWhiteSpace(request.TradeName) ? request.TradeName : request.LegalName);

                foreach (OnboardClientSystemItem item in request.Systems)
                {
                    SystemApplication systemApp = await dbContext.Set<SystemApplication>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(application => application.Id == item.SystemApplicationId, ct)
                        ?? throw new InvalidOperationException("systemApplication.notFoundOrInactive");

                    if (systemApp.Audience == CentralSystemAudience)
                    {
                        throw new InvalidOperationException("onboarding.centralSystemNotAllowed");
                    }

                    if (!systemApp.IsActive)
                    {
                        throw new InvalidOperationException("systemApplication.notFoundOrInactive");
                    }

                    Contract contract = await contractService.CreateContractCore(new CreateContractRequest
                    {
                        CompanyId = company.Id,
                        SystemApplicationId = item.SystemApplicationId,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate
                    }, ct);

                    string dbName = TenantNaming.DatabaseName(systemApp.Audience, slug, company.Id);
                    string apiKey = provisioner.GenerateApiKey();
                    string conn = provisioner.BuildTenantConnectionString(dbName, systemApp.Audience);

                    TenantDatabase tenantDatabase = new TenantDatabase(contract.Id, conn, DatabaseProvider.PostgreSql, apiKey, "public");
                    dbContext.Set<TenantDatabase>().Add(tenantDatabase);

                    plannedDatabases.Add((dbName, systemApp.Audience));
                    contractIds.Add(contract.Id);
                    systemNames.Add(systemApp.Name);
                    contractedSystemApplicationIds.Add(systemApp.Id);
                    apiKeyByAudience[systemApp.Audience] = apiKey;
                }

                string token = GenerateOpaqueToken();
                ContractAdminInvitation invitation = new ContractAdminInvitation(company.Id, token, DateTimeOffset.UtcNow.AddDays(7), true);
                dbContext.Set<ContractAdminInvitation>().Add(invitation);
                setupToken = token;
                setupLink = $"{setupBaseUrl.TrimEnd('/')}/setup-admin?token={token}";

                await dbContext.SaveChangesAsync(ct);

                foreach ((string db, string audience) in plannedDatabases)
                {
                    await provisioner.CreateDatabaseAsync(db, audience, ct);
                    createdDatabases.Add(db);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                foreach (string db in createdDatabases)
                {
                    try
                    {
                        await provisioner.DropDatabaseAsync(db, ct);
                    }
                    catch (Exception dropEx)
                    {
                        logger.LogError(dropEx, "Failed to drop tenant database '{Database}' during onboarding compensation.", db);
                    }
                }

                await transaction.RollbackAsync(ct);
                throw;
            }

            List<SystemBootstrapResult> bootstrapResults = await BootstrapContractedSystemsAsync(contractedSystemApplicationIds, apiKeyByAudience, company, ct);

            SubscriptionProvisionResult subscription = await ProvisionSubscriptionAsync(request, company.Id, ct);

            if (sendInvitationEmail)
            {
                await emailSender.SendClientAdminInvitationEmailAsync(company.Email, company.LegalName, systemNames, setupLink, ct);
            }

            return new OnboardClientResponse
            {
                CompanyId = company.Id,
                ContractIds = contractIds.ToArray(),
                DatabaseNames = plannedDatabases.Select(pair => pair.Db).ToArray(),
                BootstrapResults = bootstrapResults,
                Subscription = subscription,
                SetupToken = setupToken
            };
        }

        // Cria a assinatura do tenant (Trialing/Active conforme o plano) reusando o AssignAsync ja testado.
        // Best-effort: o tenant ja foi provisionado; falha aqui e logada e retornavel pelo endpoint de billing.
        private async Task<SubscriptionProvisionResult> ProvisionSubscriptionAsync(OnboardClientRequest request, long companyId, CancellationToken ct)
        {
            if (!request.PlanId.HasValue)
            {
                return new SubscriptionProvisionResult { Skipped = true, Detail = "plan.notProvided" };
            }

            try
            {
                SubscriptionResponse subscription = await subscriptionService.AssignAsync(
                    new AssignSubscriptionRequest { CompanyId = companyId, PlanId = request.PlanId.Value },
                    ct);

                return new SubscriptionProvisionResult
                {
                    Success = true,
                    SubscriptionId = subscription.Id,
                    PlanId = subscription.PlanId,
                    Status = subscription.Status.ToString(),
                    TrialEndsAt = subscription.TrialEndsAt
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to provision subscription (plan {PlanId}) for company {CompanyId} during onboarding.", request.PlanId, companyId);
                return new SubscriptionProvisionResult { Success = false, Detail = ex.Message };
            }
        }

        private async Task<List<SystemBootstrapResult>> BootstrapContractedSystemsAsync(
            IReadOnlyCollection<long> systemApplicationIds,
            IReadOnlyDictionary<string, string> apiKeyByAudience,
            Company company,
            CancellationToken ct)
        {
            List<SystemBootstrapResult> results = new();

            // Segredos GeneratedSecret sao compartilhados por chave dentro de um mesmo onboarding: o mesmo
            // Key (ex.: "CallbackSecret") em blueprints de apps diferentes resolve para o MESMO valor, de modo
            // que o segredo case nos dois lados (ex.: AgencyCampaign valida o callback; IntegrationPlatform o envia).
            Dictionary<string, string> generatedSecretByKey = new();

            List<SystemApplication> systemApplications = await dbContext.Set<SystemApplication>()
                .AsNoTracking()
                .Include(application => application.Integrations)
                    .ThenInclude(integration => integration.Parameters)
                .Where(application => systemApplicationIds.Contains(application.Id))
                .ToListAsync(ct);

            foreach (SystemApplication systemApp in systemApplications)
            {
                SystemBootstrapResult result = new SystemBootstrapResult
                {
                    Audience = systemApp.Audience,
                    SystemName = systemApp.Name
                };

                List<SystemIntegration> activeIntegrations = systemApp.Integrations
                    .Where(integration => integration.IsActive)
                    .ToList();

                apiKeyByAudience.TryGetValue(systemApp.Audience, out string? systemApiKey);

                string? skipReason = ResolveBootstrapSkipReason(systemApp.BaseUrl, !string.IsNullOrWhiteSpace(systemApiKey));
                if (skipReason is not null)
                {
                    logger.LogWarning(
                        "Skipping tenant bootstrap for system '{System}' (audience '{Audience}'): {Reason}. The tenant database will have NO schema.",
                        systemApp.Name,
                        systemApp.Audience,
                        skipReason);

                    result.Skipped = true;
                    result.Detail = skipReason;
                    results.Add(result);
                    continue;
                }

                // Blueprint vazio nao impede o bootstrap: e ele que roda a migration do banco do tenant.
                // Sem integracao pra semear, o bootstrap so migra (seed 0) e o tenant nasce utilizavel.
                if (activeIntegrations.Count == 0)
                {
                    logger.LogWarning(
                        "No blueprint integrations to seed for system '{System}' (audience '{Audience}'): running tenant migration only.",
                        systemApp.Name,
                        systemApp.Audience);
                }

                TenantBootstrapRequest body = BuildBootstrapRequest(activeIntegrations, apiKeyByAudience, company, generatedSecretByKey);
                string url = $"{systemApp.BaseUrl!.TrimEnd('/')}/api/tenants/bootstrap";

                try
                {
                    RestResponse<object> resp = await restApi.Fetch<object>(
                        RestRequest.Post(url, body).WithHeader("X-Api-Key", systemApiKey),
                        ct);

                    if (resp.Ok)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Success = false;
                        result.Detail = $"status {resp.Status}: {string.Join("; ", resp.Errors)}";
                        logger.LogError(
                            "Tenant bootstrap failed for system '{System}' (audience '{Audience}') at {Url}: status {Status} {Errors}.",
                            systemApp.Name,
                            systemApp.Audience,
                            url,
                            resp.Status,
                            string.Join("; ", resp.Errors));
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Detail = ex.Message;
                    logger.LogError(
                        ex,
                        "Tenant bootstrap threw for system '{System}' (audience '{Audience}') at {Url}.",
                        systemApp.Name,
                        systemApp.Audience,
                        url);
                }

                results.Add(result);
            }

            return results;
        }

        private TenantBootstrapRequest BuildBootstrapRequest(
            IEnumerable<SystemIntegration> activeIntegrations,
            IReadOnlyDictionary<string, string> apiKeyByAudience,
            Company company,
            Dictionary<string, string> generatedSecretByKey)
        {
            TenantBootstrapRequest request = new TenantBootstrapRequest();

            foreach (SystemIntegration integration in activeIntegrations)
            {
                TenantBootstrapIntegration target = new TenantBootstrapIntegration
                {
                    Name = integration.Name,
                    BaseUrl = integration.BaseUrl
                };

                foreach (SystemIntegrationParameter parameter in integration.Parameters)
                {
                    string? resolvedValue;

                    if (parameter.ValueSource == SystemIntegrationParameterSource.TenantApiKey)
                    {
                        if (string.IsNullOrWhiteSpace(parameter.SourceAudience)
                            || !apiKeyByAudience.TryGetValue(parameter.SourceAudience, out string? sourceApiKey))
                        {
                            logger.LogWarning(
                                "Skipping bootstrap parameter '{Key}' of integration '{Integration}': source audience '{SourceAudience}' was not provisioned for this tenant.",
                                parameter.Key,
                                integration.Name,
                                parameter.SourceAudience);
                            continue;
                        }

                        resolvedValue = sourceApiKey;
                    }
                    else if (parameter.ValueSource == SystemIntegrationParameterSource.TenantId)
                    {
                        resolvedValue = company.TenantId.ToString();
                    }
                    else if (parameter.ValueSource == SystemIntegrationParameterSource.GeneratedSecret)
                    {
                        // Segredo aleatorio por tenant (ex.: CallbackSecret), COMPARTILHADO por Key entre os apps
                        // deste onboarding: a 1a ocorrencia gera, as demais reusam o mesmo valor. Assim o segredo
                        // casa nos dois lados (AgencyCampaign valida o callback; IntegrationPlatform o envia).
                        resolvedValue = ResolveGeneratedSecret(generatedSecretByKey, parameter.Key, provisioner.GenerateApiKey);
                    }
                    else
                    {
                        resolvedValue = parameter.Value;
                    }

                    target.Parameters.Add(new TenantBootstrapParameter
                    {
                        Key = parameter.Key,
                        Value = resolvedValue,
                        IsSecret = parameter.IsSecret
                    });
                }

                request.Integrations.Add(target);
            }

            return request;
        }

        // Decide se o bootstrap do tenant pode ser pulado. Retorna null quando deve rodar.
        // Nao considera a quantidade de integracoes do blueprint de proposito: o bootstrap tambem roda
        // a migration do banco do tenant, entao pular por blueprint vazio deixava o tenant sem schema.
        internal static string? ResolveBootstrapSkipReason(string? baseUrl, bool apiKeyProvisioned)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return "baseUrl.notConfigured";
            }

            if (!apiKeyProvisioned)
            {
                return "apiKey.notProvisioned";
            }

            return null;
        }

        // Resolve um GeneratedSecret compartilhado por chave: gera uma vez por Key e reusa no mesmo onboarding,
        // de modo que o mesmo segredo (ex.: CallbackSecret) seja propagado identico para os dois lados que precisam casar.
        internal static string ResolveGeneratedSecret(Dictionary<string, string> generatedSecretByKey, string key, Func<string> generator)
        {
            if (!generatedSecretByKey.TryGetValue(key, out string? secret))
            {
                secret = generator();
                generatedSecretByKey[key] = secret;
            }

            return secret;
        }

        private static string GenerateOpaqueToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}
