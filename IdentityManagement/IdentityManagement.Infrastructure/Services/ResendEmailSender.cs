using IdentityManagement.Application.Services;
using Resend;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ResendEmailSender(ResendClient resend) : IEmailSender
    {
        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "noreply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Recuperação de senha — Mainstay";
            message.HtmlBody = BuildHtml(toName, resetLink);

            await resend.EmailSendAsync(message, cancellationToken);
        }

        private static string BuildHtml(string name, string resetLink) => $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <body style="font-family:sans-serif;background:#f4f4f5;margin:0;padding:32px;">
              <div style="max-width:480px;margin:0 auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                <h2 style="margin:0 0 8px;color:#1F3B61;font-size:20px;">Recuperação de senha</h2>
                <p style="color:#374151;margin:0 0 16px;">Olá, {name}.</p>
                <p style="color:#374151;margin:0 0 24px;">Recebemos uma solicitação para redefinir a senha da sua conta. Clique no botão abaixo para criar uma nova senha:</p>
                <a href="{resetLink}" style="display:inline-block;padding:12px 28px;background:#1F3B61;color:#fff;border-radius:6px;text-decoration:none;font-weight:600;font-size:15px;">Redefinir senha</a>
                <p style="color:#6b7280;font-size:13px;margin:24px 0 0;">Este link expira em 24 horas. Se você não solicitou a recuperação de senha, ignore este e-mail — sua conta permanece segura.</p>
              </div>
            </body>
            </html>
            """;
    }
}
