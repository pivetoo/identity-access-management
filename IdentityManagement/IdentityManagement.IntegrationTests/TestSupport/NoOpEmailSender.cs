using IdentityManagement.Application.Services;

namespace IdentityManagement.IntegrationTests
{
    internal sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendAdminInvitationEmailAsync(string toEmail, string companyName, string systemApplicationName, string setupLink, CancellationToken cancellationToken = default)
        {
            LastSetupLink = setupLink;
            return Task.CompletedTask;
        }

        public Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken cancellationToken = default)
        {
            LastSetupLink = setupLink;
            return Task.CompletedTask;
        }

        public Task SendContactRequestEmailAsync(string toEmail, string contactName, string contactEmail, string? contactPhone, string? companyName, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Ultimo link de convite enviado. Depois que o token passou a ser guardado como hash
        /// (IDM-013), o e-mail e o unico lugar onde o valor em claro aparece — que e exatamente o
        /// ponto. O teste precisa passar pelo mesmo caminho do usuario real.
        /// </summary>
        public static string LastSetupLink { get; private set; } = string.Empty;

        public static string ExtractToken(string setupLink)
        {
            int index = setupLink.IndexOf("token=", StringComparison.Ordinal);
            return index < 0 ? string.Empty : setupLink[(index + "token=".Length)..];
        }
    }
}
