namespace IdentityManagement.Application.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default);

        Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

        Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

        Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    }
}
