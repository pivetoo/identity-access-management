using IdentityManagement.Application.Requests.Contact;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Infrastructure.Contact;
using IdentityManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityManagement.IntegrationTests.Services
{
    /// <summary>
    /// Contato do site. O que estes casos protegem: o lead nao pode se perder por causa do e-mail,
    /// e o endpoint anonimo nao pode virar canal de spam.
    /// </summary>
    [TestFixture]
    public sealed class ContactRequestServiceIntegrationTests : IntegrationTestBase
    {
        private sealed class SpyEmailSender : IEmailSender
        {
            public List<string> Destinatarios { get; } = [];

            public bool Falhar { get; set; }

            public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default) => Task.CompletedTask;
            public Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
            public Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
            public Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
            public Task SendSignupVerificationEmailAsync(string toEmail, string companyName, string confirmLink, int expiresInHours, CancellationToken ct = default) => Task.CompletedTask;

            public Task SendAdminInvitationEmailAsync(string toEmail, string companyName, string systemApplicationName, string setupLink, CancellationToken ct = default) => Task.CompletedTask;
            public Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken ct = default) => Task.CompletedTask;

            public Task SendContactRequestEmailAsync(string toEmail, string contactName, string contactEmail, string? contactPhone, string? companyName, string message, CancellationToken ct = default)
            {
                if (Falhar)
                {
                    throw new InvalidOperationException("provedor fora do ar");
                }

                Destinatarios.Add(toEmail);
                return Task.CompletedTask;
            }
        }

        private static ContactRequestInput ValidInput() => new()
        {
            Name = "Rogerio",
            Email = "visitante@example.com",
            PhoneNumber = "11990000000",
            CompanyName = "Agencia Exemplo",
            Message = "Quero conhecer o Mainstay para a minha agencia."
        };

        private static ContactRequestService CreateSubject(DbContext dbContext, SpyEmailSender sender, string notifyEmail = "contato@mainstay.com.br")
            => new(dbContext, sender, Options.Create(new ContactOptions { NotifyEmail = notifyEmail }), NullLogger<ContactRequestService>.Instance);

        [Test]
        public async Task Contato_e_gravado_e_notificado()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SpyEmailSender sender = new();

                await CreateSubject(dbContext, sender).SubmitAsync(ValidInput(), "203.0.113.10");

                ContactRequest saved = await dbContext.Set<ContactRequest>().AsNoTracking().SingleAsync();
                saved.Email.Should().Be("visitante@example.com");
                saved.SourceIp.Should().Be("203.0.113.10");
                saved.NotificationSent.Should().BeTrue();

                sender.Destinatarios.Should().ContainSingle().Which.Should().Be("contato@mainstay.com.br");
            });
        }

        [Test]
        public async Task Notificacao_vai_para_a_caixa_da_configuracao_nunca_para_o_visitante()
        {
            // Enviar para o endereco digitado no formulario transformaria o endpoint anonimo em
            // relay de spam: qualquer um mandaria e-mail em nome do dominio para quem quisesse.
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SpyEmailSender sender = new();

                ContactRequestInput input = ValidInput();
                input.Email = "vitima@example.com";

                await CreateSubject(dbContext, sender, "time@mainstay.com.br").SubmitAsync(input, null);

                sender.Destinatarios.Should().ContainSingle().Which.Should().Be("time@mainstay.com.br");
                sender.Destinatarios.Should().NotContain("vitima@example.com");
            });
        }

        [Test]
        public async Task Falha_no_email_nao_perde_o_lead()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SpyEmailSender sender = new() { Falhar = true };

                Func<Task> act = () => CreateSubject(dbContext, sender).SubmitAsync(ValidInput(), null);

                await act.Should().NotThrowAsync("o visitante nao pode ver erro de um contato que foi salvo");

                ContactRequest saved = await dbContext.Set<ContactRequest>().AsNoTracking().SingleAsync();
                saved.NotificationSent.Should().BeFalse("e assim que o time acha o que precisa recuperar");
            });
        }

        [Test]
        public async Task Honeypot_preenchido_descarta_sem_gravar_nem_enviar()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SpyEmailSender sender = new();

                ContactRequestInput input = ValidInput();
                input.Website = "http://spam.example";

                await CreateSubject(dbContext, sender).SubmitAsync(input, null);

                (await dbContext.Set<ContactRequest>().CountAsync()).Should().Be(0);
                sender.Destinatarios.Should().BeEmpty();
            });
        }

        [Test]
        public async Task Sem_caixa_configurada_o_lead_ainda_e_gravado()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                SpyEmailSender sender = new();

                await CreateSubject(dbContext, sender, notifyEmail: string.Empty).SubmitAsync(ValidInput(), null);

                ContactRequest saved = await dbContext.Set<ContactRequest>().AsNoTracking().SingleAsync();
                saved.NotificationSent.Should().BeFalse();
                sender.Destinatarios.Should().BeEmpty();
            });
        }
    }
}
