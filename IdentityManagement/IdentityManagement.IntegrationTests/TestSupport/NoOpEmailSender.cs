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
            return Task.CompletedTask;
        }

        public Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
