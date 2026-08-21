using Archon.Core.Entities;

namespace IdentityManagement.Domain.Entities
{
    /// <summary>
    /// Contato vindo do site publico.
    ///
    /// Guardado ANTES de tentar o e-mail de proposito: e-mail e notificacao, nao armazenamento.
    /// Se o provedor cair ou a mensagem se perder na caixa, o lead continua aqui.
    /// </summary>
    public class ContactRequest : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string? PhoneNumber { get; private set; }

        public string? CompanyName { get; private set; }

        public string Message { get; private set; } = string.Empty;

        /// <summary>IP de origem: serve para investigar abuso sem depender do log do proxy.</summary>
        public string? SourceIp { get; private set; }

        public bool NotificationSent { get; private set; }

        public DateTimeOffset? NotificationSentAt { get; private set; }

        private ContactRequest()
        {
        }

        public ContactRequest(string name, string email, string? phoneNumber, string? companyName, string message, string? sourceIp)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            Name = name.Trim();
            Email = email.Trim();
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
            Message = message.Trim();
            SourceIp = string.IsNullOrWhiteSpace(sourceIp) ? null : sourceIp.Trim();
        }

        public void MarkNotified(DateTimeOffset now)
        {
            NotificationSent = true;
            NotificationSentAt = now;
        }
    }
}
