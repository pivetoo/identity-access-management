using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Clients;
using IdentityManagement.Application.Responses.Signup;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Signup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityManagement.Infrastructure.Services
{
    /// <summary>
    /// Cadastro publico de agencia. Reusa o ClientOnboardingService (transacional, com compensacao)
    /// e adiciona por cima o que so o fluxo anonimo precisa: validacao de CNPJ, recusa de empresa
    /// duplicada, resolucao de plano e sistemas pela CONFIGURACAO e resposta sem dado interno.
    ///
    /// Ordem deliberada: tudo que da para recusar e recusado ANTES do onboarding, porque a partir
    /// dele ja existe empresa, contrato e banco de tenant provisionado.
    /// </summary>
    public sealed class SelfServiceSignupService : ISelfServiceSignupService
    {
        private const string CentralSystemAudience = "identity-management";

        private readonly DbContext dbContext;
        private readonly IClientOnboardingService onboarding;
        private readonly SignupOptions options;
        private readonly ILogger<SelfServiceSignupService> logger;

        public SelfServiceSignupService(
            DbContext dbContext,
            IClientOnboardingService onboarding,
            IOptions<SignupOptions> options,
            ILogger<SelfServiceSignupService> logger)
        {
            this.dbContext = dbContext;
            this.onboarding = onboarding;
            this.options = options.Value;
            this.logger = logger;
        }

        public async Task<SignupResponse> SignupAsync(SignupRequest request, string setupBaseUrl, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!options.Enabled)
            {
                throw new NotFoundException("signup.disabled");
            }

            if (!request.AcceptedTerms)
            {
                throw new BusinessRuleException("signup.termsNotAccepted");
            }

            string document = Cnpj.Normalize(request.Document);
            if (!Cnpj.IsValid(document))
            {
                throw new BusinessRuleException("signup.document.invalid");
            }

            string email = request.Email.Trim();

            bool companyExists = await dbContext.Set<Company>()
                .AsNoTracking()
                .AnyAsync(company => company.Document == document, cancellationToken);

            if (companyExists)
            {
                // CNPJ e dado publico, entao dizer que ja existe nao entrega informacao nova —
                // e evita o suporte receber "cadastrei e nao chegou nada" de quem ja tem conta.
                throw new ConflictException("signup.company.alreadyExists");
            }

            Plan plan = await ResolvePlanAsync(request.Annual, cancellationToken);
            List<long> systemApplicationIds = await ResolveSystemApplicationIdsAsync(cancellationToken);

            OnboardClientRequest onboardRequest = new OnboardClientRequest
            {
                LegalName = request.LegalName.Trim(),
                TradeName = request.TradeName.Trim(),
                Document = document,
                Email = email,
                PhoneNumber = request.PhoneNumber?.Trim(),
                PlanId = plan.Id,
                Systems = systemApplicationIds
                    .Select(id => new OnboardClientSystemItem { SystemApplicationId = id, StartDate = DateTimeOffset.UtcNow })
                    .ToList()
            };

            OnboardClientResponse result = await onboarding.OnboardClient(onboardRequest, setupBaseUrl, cancellationToken);

            bool subscriptionActive = result.Subscription?.Success == true;

            if (!subscriptionActive)
            {
                // A empresa JA existe neste ponto (o onboarding commitou). Sem assinatura ela nao
                // passa no gate de acesso, entao isso e incidente, nao detalhe: registra alto para
                // o suporte concluir a assinatura manualmente.
                logger.LogError(
                    "Signup concluiu o provisionamento da empresa {CompanyId} ({Document}) mas NAO criou a assinatura: {Detail}. A conta nao consegue entrar ate a assinatura existir.",
                    result.CompanyId,
                    document,
                    result.Subscription?.Detail ?? "sem detalhe");
            }

            logger.LogInformation(
                "Signup publico: empresa {CompanyId} criada no plano {Plan} com {Contracts} contrato(s); assinatura ativa: {SubscriptionActive}.",
                result.CompanyId,
                plan.Name,
                result.ContractIds.Length,
                subscriptionActive);

            return new SignupResponse
            {
                Email = email,
                CompanyName = onboardRequest.TradeName,
                PlanName = plan.Name,
                TrialEndsAt = result.Subscription?.TrialEndsAt,
                SubscriptionActive = subscriptionActive
            };
        }

        private async Task<Plan> ResolvePlanAsync(bool annual, CancellationToken cancellationToken)
        {
            string planName = annual ? options.AnnualPlanName : options.MonthlyPlanName;

            Plan? plan = await dbContext.Set<Plan>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Name == planName && item.IsActive, cancellationToken);

            if (plan is null)
            {
                // Config apontando para plano inexistente/inativo e erro de operacao, nao do usuario.
                logger.LogError("Signup: plano '{Plan}' nao encontrado ou inativo. Cadastro publico indisponivel.", planName);
                throw new BusinessRuleException("signup.plan.notConfigured");
            }

            return plan;
        }

        private async Task<List<long>> ResolveSystemApplicationIdsAsync(CancellationToken cancellationToken)
        {
            List<string> audiences = options.SystemAudiences
                .Select(audience => audience.Trim())
                .Where(audience => audience.Length > 0 && audience != CentralSystemAudience)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (audiences.Count == 0)
            {
                logger.LogError("Signup: nenhuma audience configurada em Signup:SystemAudiences.");
                throw new BusinessRuleException("signup.systems.notConfigured");
            }

            List<SystemApplication> applications = await dbContext.Set<SystemApplication>()
                .AsNoTracking()
                .Where(application => application.IsActive && audiences.Contains(application.Audience))
                .ToListAsync(cancellationToken);

            if (applications.Count != audiences.Count)
            {
                string missing = string.Join(", ", audiences.Except(applications.Select(item => item.Audience), StringComparer.OrdinalIgnoreCase));
                logger.LogError("Signup: audience(s) configurada(s) sem aplicacao ativa correspondente: {Missing}.", missing);
                throw new BusinessRuleException("signup.systems.notConfigured");
            }

            return applications.Select(application => application.Id).ToList();
        }
    }
}
