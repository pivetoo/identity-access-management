using IdentityManagement.Application.Requests.Contact;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Infrastructure.Contact;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityManagement.Infrastructure.Services
{
    /// <summary>
    /// Contato do site publico.
    ///
    /// Ordem deliberada: grava PRIMEIRO, notifica depois. E-mail e o canal de aviso, nao o registro
    /// do lead — se o provedor falhar, o contato ja esta salvo e o time consegue recuperar. A falha
    /// de envio nao propaga para o visitante: para ele o contato foi recebido, porque foi mesmo.
    /// </summary>
    public sealed class ContactRequestService : IContactRequestService
    {
        private readonly DbContext dbContext;
        private readonly IEmailSender emailSender;
        private readonly ContactOptions options;
        private readonly ILogger<ContactRequestService> logger;

        public ContactRequestService(
            DbContext dbContext,
            IEmailSender emailSender,
            IOptions<ContactOptions> options,
            ILogger<ContactRequestService> logger)
        {
            this.dbContext = dbContext;
            this.emailSender = emailSender;
            this.options = options.Value;
            this.logger = logger;
        }

        public async Task SubmitAsync(ContactRequestInput request, string? sourceIp, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Isca preenchida = robo. Sai calado: dizer "recusado" ensina o robo a contornar.
            if (!string.IsNullOrWhiteSpace(request.Website))
            {
                logger.LogInformation("Contato do site descartado pelo honeypot (origem {SourceIp}).", sourceIp);
                return;
            }

            ContactRequest contact = new(
                request.Name,
                request.Email,
                request.PhoneNumber,
                request.CompanyName,
                request.Message,
                sourceIp);

            await dbContext.Set<ContactRequest>().AddAsync(contact, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(options.NotifyEmail))
            {
                logger.LogError(
                    "Contato {ContactId} registrado, mas Contact:NotifyEmail nao esta configurado — ninguem foi avisado.",
                    contact.Id);
                return;
            }

            try
            {
                await emailSender.SendContactRequestEmailAsync(
                    options.NotifyEmail,
                    contact.Name,
                    contact.Email,
                    contact.PhoneNumber,
                    contact.CompanyName,
                    contact.Message,
                    cancellationToken);

                ContactRequest tracked = await dbContext.Set<ContactRequest>()
                    .AsTracking()
                    .FirstAsync(item => item.Id == contact.Id, cancellationToken);

                tracked.MarkNotified(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                // O lead esta salvo; falhar aqui so tira o aviso. `notificationsent = false` marca
                // exatamente quem precisa ser recuperado na mao.
                logger.LogError(
                    exception,
                    "Contato {ContactId} registrado, mas a notificacao por e-mail falhou. Recuperar por contactrequests WHERE notificationsent = false.",
                    contact.Id);
            }
        }
    }
}
