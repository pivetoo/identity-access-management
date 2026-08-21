using Archon.Core.Exceptions;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
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
    /// Travas do cadastro publico. Todos os casos aqui recusam ANTES do onboarding — nenhum chega a
    /// provisionar empresa ou banco de tenant, que e exatamente a propriedade que se quer garantir:
    /// o unico endpoint anonimo que cria infraestrutura so cria depois de passar por todas.
    /// </summary>
    [TestFixture]
    public sealed class SelfServiceSignupIntegrationTests : IntegrationTestBase
    {
        private const string ValidDocument = "11444777000161";

        private static SelfServiceSignupService CreateSubject(IServiceProvider sp, SignupOptions? options = null)
        {
            // O onboarding real nao e exercitado nestes casos: todos falham antes de chama-lo.
            IClientOnboardingService onboarding = sp.GetRequiredService<IClientOnboardingService>();

            return new SelfServiceSignupService(
                sp.GetRequiredService<DbContext>(),
                onboarding,
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

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(ValidRequest(), "https://auth.example");

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

                Func<Task> act = () => subject.SignupAsync(request, "https://auth.example");
                await act.Should().ThrowAsync<BusinessRuleException>();

                (await dbContext.Set<Company>().CountAsync()).Should().Be(0);
                (await dbContext.Set<Contract>().CountAsync()).Should().Be(0);
            });
        }
    }
}
