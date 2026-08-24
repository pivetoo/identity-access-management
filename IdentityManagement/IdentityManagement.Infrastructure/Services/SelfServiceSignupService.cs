using System.Security.Cryptography;
using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Clients;
using IdentityManagement.Application.Responses.Signup;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.Security;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Signup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityManagement.Infrastructure.Services
{
    /// <summary>
    /// Cadastro publico de agencia, em DUAS etapas.
    ///
    /// Etapa 1 (<see cref="SignupAsync"/>) valida e grava uma linha em <c>pendingsignups</c>, e manda
    /// o link de confirmacao. Etapa 2 (<see cref="ConfirmAsync"/>) e a unica que provisiona.
    ///
    /// A ordem e deliberada. Provisionar na etapa 1 significava que cada chamada anonima criava
    /// empresa, contratos e DOIS bancos no Postgres antes de qualquer prova de que o e-mail era
    /// real — o que quebrava de dois jeitos: e-mail digitado errado trancava a agencia do lado de
    /// fora com o CNPJ ocupado (so saia apagando banco na mao), e o rate limit por IP nao segura
    /// ataque distribuido contra um Postgres que e compartilhado por todos os sistemas.
    ///
    /// O usuario legitimo nao paga nada por isso: o numero de passos e o mesmo de antes
    /// (formulario, e-mail, clique, senha) — so mudou o instante em que os bancos nascem.
    /// </summary>
    public sealed class SelfServiceSignupService : ISelfServiceSignupService
    {
        private const string CentralSystemAudience = "identity-management";

        /// <summary>Validade do convite de administrador, igual a usada no ClientOnboardingService.</summary>
        private const int InvitationDays = 7;

        private readonly DbContext dbContext;
        private readonly IClientOnboardingService onboarding;
        private readonly IEmailSender emailSender;
        private readonly SignupOptions options;
        private readonly ILogger<SelfServiceSignupService> logger;

        public SelfServiceSignupService(
            DbContext dbContext,
            IClientOnboardingService onboarding,
            IEmailSender emailSender,
            IOptions<SignupOptions> options,
            ILogger<SelfServiceSignupService> logger)
        {
            this.dbContext = dbContext;
            this.onboarding = onboarding;
            this.emailSender = emailSender;
            this.options = options.Value;
            this.logger = logger;
        }

        public async Task<SignupResponse> SignupAsync(SignupRequest request, string confirmBaseUrl, string? sourceIp, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            EnsureEnabled();

            if (!string.IsNullOrWhiteSpace(request.Website))
            {
                // Descarte SILENCIOSO, com resposta de sucesso: dizer "recusado" ensina o robo a
                // contornar na proxima tentativa. O nome do plano sai da configuracao, sem ida ao
                // banco — robo nao merece consulta.
                logger.LogInformation("Cadastro publico descartado pelo honeypot (origem {SourceIp}).", sourceIp);

                return new SignupResponse
                {
                    Email = request.Email.Trim(),
                    CompanyName = request.TradeName.Trim(),
                    PlanName = request.Annual ? options.AnnualPlanName : options.MonthlyPlanName,
                    VerificationExpiresAt = DateTimeOffset.UtcNow.AddHours(options.VerificationLinkHours)
                };
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

            if (!Phone.IsValid(request.PhoneNumber))
            {
                throw new BusinessRuleException("signup.phone.invalid");
            }

            string email = request.Email.Trim();

            await EnsureNotTakenAsync(document, email, cancellationToken);

            // Resolvido JA na etapa 1 mesmo sem provisionar: plano mal configurado e falha de
            // operacao, e descobrir isso so depois que a pessoa confirmou o e-mail seria pior —
            // ela ja teria gasto o clique e nao teria como tentar de novo.
            Plan plan = await ResolvePlanAsync(request.Annual, cancellationToken);
            await ResolveSystemApplicationIdsAsync(cancellationToken);

            string tradeName = request.TradeName.Trim();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(options.VerificationLinkHours);

            SignupResponse resposta = new SignupResponse
            {
                Email = email,
                CompanyName = tradeName,
                PlanName = plan.Name,
                VerificationExpiresAt = expiresAt
            };

            if (!await CanSendVerificationEmailAsync(email, cancellationToken))
            {
                // Resposta IDENTICA a do envio bem-sucedido, e de proposito: uma resposta diferente
                // viraria oraculo para o atacante descobrir quais enderecos ja estao em jogo. Nao
                // grava a linha pendente tampouco — sem e-mail entregue, o token nasceria inutil.
                return resposta;
            }

            string token = GenerateOpaqueToken();

            PendingSignup pending = new PendingSignup(
                request.LegalName.Trim(),
                tradeName,
                document,
                email,
                Phone.Normalize(request.PhoneNumber),
                request.Annual,
                token,
                expiresAt,
                sourceIp);

            await dbContext.Set<PendingSignup>().AddAsync(pending, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            string confirmLink = $"{confirmBaseUrl.TrimEnd('/')}/signup/confirmar?token={token}";

            try
            {
                await emailSender.SendSignupVerificationEmailAsync(
                    email,
                    pending.TradeName,
                    confirmLink,
                    options.VerificationLinkHours,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                // O e-mail E o fluxo aqui: sem ele o cadastro nao tem como continuar. Diferente do
                // contato do site, nao da para engolir a falha e seguir dizendo que deu certo.
                logger.LogError(exception, "Signup: falha ao enviar o e-mail de confirmacao para {Email}.", email);
                throw new BusinessRuleException("signup.verification.emailFailed");
            }

            logger.LogInformation(
                "Signup publico: cadastro pendente {PendingId} registrado para {Document}; confirmacao valida ate {ExpiresAt}.",
                pending.Id,
                document,
                expiresAt);

            return resposta;
        }

        /// <summary>
        /// Duas travas de ENVIO, conferidas juntas porque a resposta e a mesma nos dois casos.
        ///
        /// Elas nao protegem o disco (disso cuida o teto de provisionamento) e sim a reputacao do
        /// dominio: o cadastro dispara e-mail para o endereco que o visitante digitar, e o limite
        /// por IP nao segura bombardeio vindo de muitos IPs contra um unico destinatario.
        /// </summary>
        private async Task<bool> CanSendVerificationEmailAsync(string email, CancellationToken cancellationToken)
        {
            DateTimeOffset agora = DateTimeOffset.UtcNow;

            int limitePorEndereco = options.MaxVerificationEmailsPerAddressPerDay;

            if (limitePorEndereco > 0)
            {
                DateTimeOffset umDiaAtras = agora.AddDays(-1);

                int noEndereco = await dbContext.Set<PendingSignup>()
                    .AsNoTracking()
                    .CountAsync(item => item.Email == email && item.CreatedAt >= umDiaAtras, cancellationToken);

                if (noEndereco >= limitePorEndereco)
                {
                    logger.LogWarning(
                        "Signup: {Endereco} ja recebeu {Limite} confirmacoes em 24h; envio suprimido.",
                        email,
                        limitePorEndereco);

                    return false;
                }
            }

            int limiteGlobal = options.GlobalHourlyVerificationEmailLimit;

            if (limiteGlobal > 0)
            {
                DateTimeOffset umaHoraAtras = agora.AddHours(-1);

                int naUltimaHora = await dbContext.Set<PendingSignup>()
                    .AsNoTracking()
                    .CountAsync(item => item.CreatedAt >= umaHoraAtras, cancellationToken);

                if (naUltimaHora >= limiteGlobal)
                {
                    // Nivel de erro: ou viralizou, ou tem ataque em curso. Os dois querem olho agora.
                    logger.LogError(
                        "Signup: teto global de e-mails de confirmacao atingido ({Atual} na ultima hora, limite {Limite}). Envios suprimidos ate a janela abrir.",
                        naUltimaHora,
                        limiteGlobal);

                    return false;
                }
            }

            return true;
        }

        public async Task<SignupConfirmResponse> ConfirmAsync(SignupConfirmRequest request, string setupBaseUrl, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            EnsureEnabled();

            string tokenHash = TokenHasher.Hash(request.Token);

            PendingSignup? pending = await dbContext.Set<PendingSignup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Token == tokenHash, cancellationToken);

            if (pending is null)
            {
                throw new NotFoundException("signup.confirm.invalidToken");
            }

            // Ja provisionado. O link do e-mail passa a ser o caminho de VOLTA ate a senha existir —
            // e o que permite mandar um unico e-mail em vez de dois. Sem isto, fechar a aba durante
            // o provisionamento deixaria a pessoa trancada do lado de fora com o CNPJ ja ocupado,
            // que e exatamente a armadilha que este desenho veio eliminar.
            if (pending.CompanyId is not null)
            {
                return await ReissueSetupAsync(pending, cancellationToken);
            }

            if (pending.ConsumedAt is not null)
            {
                // Reivindicado e ainda sem empresa: outra aba esta provisionando agora, ou o processo
                // caiu no meio (o PendingSignupCleanupJob devolve a reserva em ate 30 min).
                throw new ConflictException("signup.confirm.inProgress");
            }

            if (DateTimeOffset.UtcNow >= pending.ExpiresAt)
            {
                throw new BusinessRuleException("signup.confirm.expired");
            }

            // Trava contra clique duplo. A checagem acima e so para produzir mensagem boa; a decisao
            // de quem provisiona e ESTE update condicional, resolvido pelo banco. Sem ele, dois
            // cliques simultaneos disparam dois provisionamentos do mesmo CNPJ.
            int claimed = await dbContext.Set<PendingSignup>()
                .Where(item => item.Id == pending.Id && item.ConsumedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.ConsumedAt, DateTimeOffset.UtcNow),
                    cancellationToken);

            if (claimed == 0)
            {
                throw new ConflictException("signup.confirm.inProgress");
            }

            // Teto global, conferido DEPOIS da reserva de proposito: a reserva e atomica, entao
            // contar reservas da ultima hora (incluindo a que acabou de ser feita) da um numero
            // muito mais proximo do real do que conferir antes. Nao e exato sob concorrencia alta —
            // duas confirmacoes simultaneas podem passar juntas — mas o excedente e da ordem de
            // requisicoes em voo, nao do tamanho do pool de IPs do atacante, que e o ponto.
            await EnsureGlobalCapacityAsync(pending.Id, cancellationToken);

            try
            {
                // De novo, e nao so na etapa 1: entre o cadastro e o clique alguem pode ter tomado o
                // CNPJ ou o e-mail — inclusive outro cadastro pendente que confirmou primeiro.
                await EnsureNotTakenAsync(pending.Document, pending.Email, cancellationToken);

                Plan plan = await ResolvePlanAsync(pending.Annual, cancellationToken);
                List<long> systemApplicationIds = await ResolveSystemApplicationIdsAsync(cancellationToken);

                OnboardClientRequest onboardRequest = new OnboardClientRequest
                {
                    LegalName = pending.LegalName,
                    TradeName = pending.TradeName,
                    Document = pending.Document,
                    Email = pending.Email,
                    PhoneNumber = pending.PhoneNumber,
                    PlanId = plan.Id,
                    Systems = systemApplicationIds
                        .Select(id => new OnboardClientSystemItem { SystemApplicationId = id, StartDate = DateTimeOffset.UtcNow })
                        .ToList()
                };

                // sendInvitationEmail: false — a pessoa acabou de clicar no link do e-mail de
                // confirmacao e cai direto na tela de senha. Um segundo e-mail chegaria junto com o
                // primeiro dizendo quase a mesma coisa.
                OnboardClientResponse result = await onboarding.OnboardClient(onboardRequest, setupBaseUrl, sendInvitationEmail: false, ct: cancellationToken);

                await dbContext.Set<PendingSignup>()
                    .Where(item => item.Id == pending.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(item => item.CompanyId, result.CompanyId),
                        cancellationToken);

                bool subscriptionActive = result.Subscription?.Success == true;

                if (!subscriptionActive)
                {
                    // A empresa JA existe neste ponto (o onboarding commitou). Sem assinatura ela nao
                    // passa no gate de acesso, entao isso e incidente, nao detalhe.
                    logger.LogError(
                        "Signup concluiu o provisionamento da empresa {CompanyId} ({Document}) mas NAO criou a assinatura: {Detail}. A conta nao consegue entrar ate a assinatura existir.",
                        result.CompanyId,
                        pending.Document,
                        result.Subscription?.Detail ?? "sem detalhe");
                }

                string[] brokenBootstraps = result.BootstrapResults
                    .Where(bootstrap => !bootstrap.Success)
                    .Select(bootstrap => $"{bootstrap.Audience}: {(bootstrap.Skipped ? "pulado" : "falhou")} ({bootstrap.Detail ?? "sem detalhe"})")
                    .ToArray();

                if (brokenBootstraps.Length > 0)
                {
                    logger.LogError(
                        "Signup provisionou a empresa {CompanyId} mas o bootstrap nao concluiu em {Count} sistema(s): {Detail}. O banco do tenant pode estar vazio.",
                        result.CompanyId,
                        brokenBootstraps.Length,
                        string.Join(" | ", brokenBootstraps));
                }

                logger.LogInformation(
                    "Signup publico confirmado: empresa {CompanyId} criada no plano {Plan} com {Contracts} contrato(s); assinatura ativa: {SubscriptionActive}.",
                    result.CompanyId,
                    plan.Name,
                    result.ContractIds.Length,
                    subscriptionActive);

                return new SignupConfirmResponse
                {
                    SetupToken = result.SetupToken ?? string.Empty,
                    CompanyName = pending.TradeName,
                    PlanName = plan.Name,
                    TrialEndsAt = result.Subscription?.TrialEndsAt,
                    SubscriptionActive = subscriptionActive
                };
            }
            catch
            {
                // O onboarding e transacional e compensa o que criou, entao devolver a linha ao
                // estado pendente e correto: o link volta a valer e a pessoa pode tentar de novo.
                // Sem isto, uma falha do provedor de cobranca queimaria o cadastro em definitivo.
                await ReleaseClaimAsync(pending.Id, cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Emite um convite novo para uma empresa que ja foi provisionada por este cadastro.
        ///
        /// Os tokens ficam com hash, entao nao ha como reapresentar o anterior: emite outro e revoga
        /// os que ainda valiam, para nao deixar convite solto.
        /// </summary>
        private async Task<SignupConfirmResponse> ReissueSetupAsync(PendingSignup pending, CancellationToken cancellationToken)
        {
            long companyId = pending.CompanyId!.Value;

            // Limite: o link nao vale para sempre. Espelha a validade do proprio convite (7 dias),
            // para um e-mail antigo nao virar acesso de administrador meses depois.
            if (pending.ConsumedAt is not null && DateTimeOffset.UtcNow > pending.ConsumedAt.Value.AddDays(InvitationDays))
            {
                throw new BusinessRuleException("signup.confirm.expired");
            }

            bool jaConfigurado = await dbContext.Set<ContractAdminInvitation>()
                .AsNoTracking()
                .AnyAsync(item => item.CompanyId == companyId && item.UsedAt != null, cancellationToken);

            if (jaConfigurado)
            {
                throw new ConflictException("signup.confirm.alreadySetUp");
            }

            await dbContext.Set<ContractAdminInvitation>()
                .Where(item => item.CompanyId == companyId && item.UsedAt == null && item.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.RevokedAt, DateTimeOffset.UtcNow),
                    cancellationToken);

            string token = GenerateOpaqueToken();
            ContractAdminInvitation invitation = new ContractAdminInvitation(companyId, token, DateTimeOffset.UtcNow.AddDays(InvitationDays), true);
            await dbContext.Set<ContractAdminInvitation>().AddAsync(invitation, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            Subscription? subscription = await dbContext.Set<Subscription>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CompanyId == companyId, cancellationToken);

            Plan? plan = subscription is null
                ? null
                : await dbContext.Set<Plan>().AsNoTracking().FirstOrDefaultAsync(item => item.Id == subscription.PlanId, cancellationToken);

            logger.LogInformation(
                "Signup: link de confirmacao reaproveitado pela empresa {CompanyId}; convite anterior revogado e novo emitido.",
                companyId);

            return new SignupConfirmResponse
            {
                SetupToken = token,
                CompanyName = pending.TradeName,
                PlanName = plan?.Name ?? string.Empty,
                TrialEndsAt = subscription?.TrialEndsAt,
                SubscriptionActive = subscription is not null
            };
        }

        private async Task EnsureGlobalCapacityAsync(long pendingSignupId, CancellationToken cancellationToken)
        {
            int limite = options.GlobalHourlyProvisioningLimit;

            if (limite <= 0)
            {
                return;
            }

            DateTimeOffset janela = DateTimeOffset.UtcNow.AddHours(-1);

            int naUltimaHora = await dbContext.Set<PendingSignup>()
                .AsNoTracking()
                .CountAsync(item => item.ConsumedAt != null && item.ConsumedAt >= janela, cancellationToken);

            if (naUltimaHora <= limite)
            {
                return;
            }

            // Devolve a reserva: o cadastro continua valido e a pessoa pode reabrir o link depois.
            await ReleaseClaimAsync(pendingSignupId, cancellationToken);

            // Nivel de erro de proposito: ou o produto viralizou, ou esta acontecendo um ataque.
            // Os dois casos precisam de olho humano agora, nao no relatorio de amanha.
            logger.LogError(
                "Signup: teto global de provisionamento atingido ({NaUltimaHora} na ultima hora, limite {Limite}). Confirmacoes recusadas ate a janela abrir.",
                naUltimaHora,
                limite);

            throw new BusinessRuleException("signup.confirm.capacityReached");
        }

        private void EnsureEnabled()
        {
            if (!options.Enabled)
            {
                throw new NotFoundException("signup.disabled");
            }
        }

        private async Task ReleaseClaimAsync(long pendingSignupId, CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.Set<PendingSignup>()
                    .Where(item => item.Id == pendingSignupId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(item => item.ConsumedAt, (DateTimeOffset?)null),
                        cancellationToken);
            }
            catch (Exception exception)
            {
                // Nao pode mascarar a excecao original que trouxe o fluxo ate aqui.
                logger.LogError(exception, "Signup: falha ao liberar o cadastro pendente {PendingId} apos erro no provisionamento.", pendingSignupId);
            }
        }

        private async Task EnsureNotTakenAsync(string document, string email, CancellationToken cancellationToken)
        {
            bool companyExists = await dbContext.Set<Company>()
                .AsNoTracking()
                .AnyAsync(company => company.Document == document, cancellationToken);

            if (companyExists)
            {
                // CNPJ e dado publico, entao dizer que ja existe nao entrega informacao nova —
                // e evita o suporte receber "cadastrei e nao chegou nada" de quem ja tem conta.
                throw new ConflictException("signup.company.alreadyExists");
            }

            // companies.email tem indice UNICO. Sem esta checagem a colisao so aparecia no
            // SaveChanges, como 500 cru.
            bool emailInUse = await dbContext.Set<Company>()
                .AsNoTracking()
                .AnyAsync(company => company.Email == email, cancellationToken);

            if (emailInUse)
            {
                throw new ConflictException("signup.email.alreadyExists");
            }
        }

        private async Task<Plan> ResolvePlanAsync(bool annual, CancellationToken cancellationToken)
        {
            // Dentro da janela de lancamento o cadastro contrata o plano Fundador; fora dela (ou se
            // o plano Fundador nao existir), o plano padrao da tabela.
            if (options.LaunchUntil.HasValue && DateTimeOffset.UtcNow <= options.LaunchUntil.Value)
            {
                string launchName = annual ? options.LaunchAnnualPlanName : options.LaunchMonthlyPlanName;
                Plan? launch = await dbContext.Set<Plan>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Name == launchName && item.IsActive, cancellationToken);

                if (launch is not null)
                {
                    return launch;
                }
            }

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

        private static string GenerateOpaqueToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}
