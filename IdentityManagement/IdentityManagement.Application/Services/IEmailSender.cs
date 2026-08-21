namespace IdentityManagement.Application.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default);

        Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

        Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

        Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirmacao do cadastro publico. E o unico e-mail que sai ANTES de existir qualquer
        /// coisa provisionada: o clique neste link e que autoriza criar empresa, contratos e os
        /// bancos de tenant.
        /// </summary>
        Task SendSignupVerificationEmailAsync(string toEmail, string companyName, string confirmLink, int expiresInHours, CancellationToken cancellationToken = default);

        Task SendAdminInvitationEmailAsync(string toEmail, string companyName, string systemApplicationName, string setupLink, CancellationToken cancellationToken = default);

        Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken cancellationToken = default);

        /// <summary>
        /// Avisa o time sobre um contato vindo da landing.
        ///
        /// <paramref name="toEmail"/> vem de CONFIGURACAO, nunca do formulario: um endpoint anonimo
        /// que envia para endereco informado pelo visitante e relay de spam aberto.
        /// </summary>
        Task SendContactRequestEmailAsync(string toEmail, string contactName, string contactEmail, string? contactPhone, string? companyName, string message, CancellationToken cancellationToken = default);
    }
}
