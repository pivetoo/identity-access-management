using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Signup;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.Security;
using IdentityManagement.Domain.ValueObjects;
using IdentityManagement.Infrastructure.Services;
using IdentityManagement.Infrastructure.Signup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityManagement.IntegrationTests.Services
{
    /// <summary>
    /// Travas do cadastro publico.
    ///
    /// A propriedade central: o endpoint anonimo NAO provisiona. Depois do desenho em duas etapas,
    /// nem o caminho feliz cria empresa ou banco — isso agora e exclusividade do Confirm, que exige
    /// um token que so existe dentro da caixa de entrada.
    /// </summary>
    [TestFixture]
    public sealed class SelfServiceSignupIntegrationTests : IntegrationTestBase
    {
        private const string ValidDocument = "11444777000161";

        private static SelfServiceSignupService CreateSubject(IServiceProvider sp, SignupOptions? options = null)
        {
            IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();

            return new SelfServiceSignupService(
                sp.GetRequiredService<DbContext>(),
                onboarding,
                new NoOpEmailSender(),
                Options.Create(options ?? new SignupOptions { Enabled = true }),
                NullLogger<SelfServiceSignupService>.Instance);
        }

        private static SignupRequest ValidRequest() => new()
        {
            LegalName = "Agencia Signup LTDA",
            TradeName = "Agencia Signup",
            Document = ValidDocument,
            Email = "contato@signup.example",
            AcceptedTerms = true
        };

        [Test]
        public async Task Signup_is_refused_when_disabled()
        {
            await InScopeAsync(async sp =>
            {
                SelfServiceSignupService subject = CreateSubject(sp, new SignupOptions { Enabled = false });

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                await act.Should().ThrowAsync<NotFoundException>();
            });
        }

        [Test]
        public async Task Signup_is_refused_without_accepting_terms()
        {
            await InScopeAsync(async sp =>
            {
                SelfServiceSignupService subject = CreateSubject(sp);
                SignupRequest request = ValidRequest();
                request.AcceptedTerms = false;

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example", null);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Signup_is_refused_when_document_is_invalid()
        {
            await InScopeAsync(async sp =>
            {
                SelfServiceSignupService subject = CreateSubject(sp);
                SignupRequest request = ValidRequest();
                request.Document = "11444777000160";

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example", null);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Signup_is_refused_when_document_already_has_a_company()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                dbContext.Set<Company>().Add(new Company("Ja Existe LTDA", "Ja Existe", ValidDocument, "ja@existe.example", "11999990000"));
                await dbContext.SaveChangesAsync();

                SelfServiceSignupService subject = CreateSubject(sp);

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                await act.Should().ThrowAsync<ConflictException>();
            });
        }

        [Test]
        public async Task Signup_is_refused_when_the_email_belongs_to_another_company()
        {
            // companies.email tem indice UNICO: sem esta trava a colisao so estourava no
            // SaveChanges, como 500 cru, depois de ja ter passado por todas as validacoes.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                dbContext.Set<Company>().Add(new Company("Outra Empresa LTDA", "Outra Empresa", "11222333000181", "contato@signup.example", "11999990000"));
                await dbContext.SaveChangesAsync();

                SelfServiceSignupService subject = CreateSubject(sp);

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                await act.Should().ThrowAsync<ConflictException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(1, "a empresa nova nao pode ser provisionada");
            });
        }

        [Test]
        public async Task Signup_is_refused_when_configured_plan_is_inactive()
        {
            // Plano inativo nao pode ser contratado pela porta publica — e assim que o plano
            // Interno (gratuito, isactive=false) fica fora do alcance de quem se cadastra sozinho.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                Plan plan = new("Plano Fechado", 0m, BillingPeriod.Monthly, "BRL", 0, null);
                plan.Deactivate();
                dbContext.Set<Plan>().Add(plan);
                await dbContext.SaveChangesAsync();

                SelfServiceSignupService subject = CreateSubject(sp, new SignupOptions
                {
                    Enabled = true,
                    MonthlyPlanName = "Plano Fechado"
                });

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Signup_is_refused_when_a_configured_audience_has_no_active_application()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                dbContext.Set<Plan>().Add(new Plan("Completo Mensal", 497m, BillingPeriod.Monthly, "BRL", 14, null));
                await dbContext.SaveChangesAsync();

                SelfServiceSignupService subject = CreateSubject(sp, new SignupOptions
                {
                    Enabled = true,
                    SystemAudiences = ["agency-campaign", "audience-que-nao-existe"]
                });

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                await act.Should().ThrowAsync<BusinessRuleException>();
            });
        }

        [Test]
        public async Task Signup_never_provisions_a_company_when_it_is_refused()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SelfServiceSignupService subject = CreateSubject(sp);
                SignupRequest request = ValidRequest();
                request.Document = "00000000000000";

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example", null);
                await act.Should().ThrowAsync<BusinessRuleException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0);
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(0);
            });
        }
        // ---------------------------------------------------------------------------------------
        // Desenho em duas etapas. O que estes casos protegem: nenhuma chamada anonima sem token
        // pode criar empresa, contrato ou banco de tenant.
        // ---------------------------------------------------------------------------------------

        [Test]
        public async Task Signup_registers_a_pending_row_and_provisions_nothing()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp);

                await subject.SignupAsync(ValidRequest(), "https://auth.example", "203.0.113.7");

                (await dbContext.Set<PendingSignup>().CountAsync()).Should().Be(1);

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0, "o caminho feliz tambem nao pode provisionar antes da confirmacao");
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(0);
                (await dbContext.Set<TenantDatabase>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task Signup_stores_only_the_token_hash()
        {
            // O link do e-mail e a unica copia em claro. Um SELECT na tabela nao pode entregar
            // tokens prontos para confirmar cadastros alheios.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());
                await subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                string token = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);
                token.Should().NotBeEmpty();

                PendingSignup pending = await dbContext.Set<PendingSignup>().AsNoTracking().SingleAsync();
                pending.Token.Should().NotBe(token);
                pending.Token.Should().Be(TokenHasher.Hash(token));
            });
        }

        [Test]
        public async Task Confirm_provisions_the_tenant_and_returns_the_setup_token()
        {
            List<string> createdDatabases = new();

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());
                await subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                string token = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);

                SignupConfirmResponse response = await subject.ConfirmAsync(
                    new SignupConfirmRequest { Token = token },
                    "https://auth.example");

                response.SetupToken.Should().NotBeEmpty("a tela emenda direto na definicao de senha");

                (await dbContext.Set<Company>().CountAsync()).Should().Be(1);
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(1);

                PendingSignup pending = await dbContext.Set<PendingSignup>().AsNoTracking().SingleAsync();
                pending.ConsumedAt.Should().NotBeNull();
                pending.CompanyId.Should().NotBeNull();

                createdDatabases.AddRange(await dbContext.Set<TenantDatabase>().AsNoTracking().Select(item => item.ConnectionString).ToListAsync());
            });

            await DropCreatedDatabasesAsync(createdDatabases);
        }

        [Test]
        public async Task Confirm_twice_provisions_once_and_reissues_the_setup_token()
        {
            // Abrir o link de novo (aba fechada no meio, e-mail reaberto) nao pode disparar um
            // segundo onboarding — mas TAMBEM nao pode dar erro: e o unico caminho de volta ate a
            // senha, e e o que permite mandar um e-mail so em vez de dois.
            List<string> createdDatabases = new();

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());
                await subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                string token = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);
                SignupConfirmRequest request = new() { Token = token };

                SignupConfirmResponse primeira = await subject.ConfirmAsync(request, "https://auth.example");
                SignupConfirmResponse segunda = await subject.ConfirmAsync(request, "https://auth.example");

                segunda.SetupToken.Should().NotBeEmpty();
                segunda.SetupToken.Should().NotBe(primeira.SetupToken, "o token fica com hash, entao o reenvio emite outro");

                (await dbContext.Set<Company>().CountAsync()).Should().Be(1, "o segundo clique nao pode provisionar de novo");
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(1);

                // O convite anterior tem de ser revogado, para nao sobrar dois validos na caixa.
                List<ContractAdminInvitation> convites = await dbContext.Set<ContractAdminInvitation>().AsNoTracking().ToListAsync();
                convites.Count(item => item.IsValid()).Should().Be(1);

                createdDatabases.AddRange(await dbContext.Set<TenantDatabase>().AsNoTracking().Select(item => item.ConnectionString).ToListAsync());
            });

            await DropCreatedDatabasesAsync(createdDatabases);
        }

        [Test]
        public async Task Confirm_does_not_send_the_invitation_email_on_the_public_path()
        {
            // A causa dos dois e-mails: o onboarding mandava o convite mesmo quando a pessoa ja
            // estava na tela de senha.
            List<string> createdDatabases = new();

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());
                await subject.SignupAsync(ValidRequest(), "https://auth.example", null);

                NoOpEmailSender.ResetLastSetupLink();

                string token = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);
                await subject.ConfirmAsync(new SignupConfirmRequest { Token = token }, "https://auth.example");

                NoOpEmailSender.LastSetupLink.Should().BeEmpty("o cadastro publico manda um e-mail so");

                createdDatabases.AddRange(await dbContext.Set<TenantDatabase>().AsNoTracking().Select(item => item.ConnectionString).ToListAsync());
            });

            await DropCreatedDatabasesAsync(createdDatabases);
        }

        [Test]
        public async Task Confirm_is_refused_with_an_unknown_token()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SelfServiceSignupService subject = CreateSubject(sp);

                Func<Task> act = () => subject.ConfirmAsync(
                    new SignupConfirmRequest { Token = "token-que-nunca-existiu-mas-tem-tamanho" },
                    "https://auth.example");

                await act.Should().ThrowAsync<NotFoundException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0);
            });
        }

        [Test]
        public async Task Confirm_is_refused_when_the_link_expired()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                string token = "token-expirado-de-teste-com-tamanho-suficiente";
                dbContext.Set<PendingSignup>().Add(new PendingSignup(
                    "Agencia Expirada LTDA", "Agencia Expirada", ValidDocument, "expirada@signup.example",
                    null, false, token, DateTimeOffset.UtcNow.AddHours(-1), null));
                await dbContext.SaveChangesAsync();

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());

                Func<Task> act = () => subject.ConfirmAsync(new SignupConfirmRequest { Token = token }, "https://auth.example");

                await act.Should().ThrowAsync<BusinessRuleException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0, "link vencido nao provisiona");
            });
        }

        [Test]
        public async Task Confirm_is_refused_when_the_document_was_taken_in_the_meantime()
        {
            // Duas pessoas cadastram o mesmo CNPJ antes de qualquer confirmacao: quem confirmar
            // primeiro leva. A segunda checagem existe por isso — a da etapa 1 ja esta velha aqui.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SelfServiceSignupService subject = CreateSubject(sp, SingleAudienceOptions());
                await subject.SignupAsync(ValidRequest(), "https://auth.example", null);
                string token = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);

                dbContext.Set<Company>().Add(new Company("Chegou Antes LTDA", "Chegou Antes", ValidDocument, "antes@signup.example", "11999990000"));
                await dbContext.SaveChangesAsync();

                Func<Task> act = () => subject.ConfirmAsync(new SignupConfirmRequest { Token = token }, "https://auth.example");
                await act.Should().ThrowAsync<ConflictException>();

                // Falhar nao pode queimar o cadastro: a linha volta a ficar pendente.
                PendingSignup pending = await dbContext.Set<PendingSignup>().AsNoTracking().SingleAsync();
                pending.ConsumedAt.Should().BeNull("provisionamento que falha devolve o link ao estado utilizavel");
            });
        }

        [Test]
        public async Task Confirm_is_refused_when_the_global_hourly_cap_is_reached()
        {
            // O teto existe porque rate limit e captcha so encarecem o ataque: com IPs e caixas de
            // entrada suficientes o volume passa. Isto limita o ESTRAGO, nao o custo do atacante.
            List<string> createdDatabases = new();

            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                await SeedForOnboardingAsync(dbContext);

                SignupOptions options = SingleAudienceOptions();
                options.GlobalHourlyProvisioningLimit = 1;

                SelfServiceSignupService subject = CreateSubject(sp, options);

                SignupRequest primeiro = ValidRequest();
                await subject.SignupAsync(primeiro, "https://auth.example", null);
                string tokenPrimeiro = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);

                SignupRequest segundo = ValidRequest();
                segundo.Document = "22333444000181";
                segundo.Email = "outra@signup.example";
                segundo.TradeName = "Outra Agencia";
                await subject.SignupAsync(segundo, "https://auth.example", null);
                string tokenSegundo = NoOpEmailSender.ExtractConfirmToken(NoOpEmailSender.LastConfirmLink);

                await subject.ConfirmAsync(new SignupConfirmRequest { Token = tokenPrimeiro }, "https://auth.example");

                Func<Task> acimaDoTeto = () => subject.ConfirmAsync(new SignupConfirmRequest { Token = tokenSegundo }, "https://auth.example");
                await acimaDoTeto.Should().ThrowAsync<BusinessRuleException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(1, "o teto tem de barrar o segundo provisionamento");

                // Recusar por capacidade nao pode queimar o cadastro de quem estava na fila.
                PendingSignup barrado = await dbContext.Set<PendingSignup>().AsNoTracking()
                    .SingleAsync(item => item.Document == "22333444000181");
                barrado.ConsumedAt.Should().BeNull("a reserva volta, para a pessoa reabrir o link depois");
                barrado.CompanyId.Should().BeNull();

                createdDatabases.AddRange(await dbContext.Set<TenantDatabase>().AsNoTracking().Select(item => item.ConnectionString).ToListAsync());
            });

            await DropCreatedDatabasesAsync(createdDatabases);
        }

        private static async Task SeedForOnboardingAsync(DbContext dbContext)
        {
            SystemApplication agencyApp = new("AgencyCampaign", "Mainstay", "agency-campaign", ApplicationType.External);
            dbContext.Set<SystemApplication>().Add(agencyApp);
            dbContext.Set<Plan>().Add(new Plan("Completo Mensal", 497m, BillingPeriod.Monthly, "BRL", 14, null));
            await dbContext.SaveChangesAsync();

            dbContext.Set<SystemRoleTemplate>().Add(new SystemRoleTemplate(agencyApp.Id, "Administrador", "Papel raiz", true, true));
            await dbContext.SaveChangesAsync();
        }

        private static SignupOptions SingleAudienceOptions() => new()
        {
            Enabled = true,
            SystemAudiences = ["agency-campaign"]
        };

        private async Task DropCreatedDatabasesAsync(List<string> connectionStrings)
        {
            await InScopeAsync(async sp =>
            {
                ITenantProvisioner provisioner = sp.GetRequiredService<ITenantProvisioner>();

                foreach (string connectionString in connectionStrings)
                {
                    string? nome = connectionString
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(parte => parte.TrimStart().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=', 2)[1];

                    if (!string.IsNullOrWhiteSpace(nome))
                    {
                        await provisioner.DropDatabaseAsync(nome);
                    }
                }
            });
        }
    }
}
