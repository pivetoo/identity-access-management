using IdentityManagement.Application.Services;
using Resend;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class ResendEmailSender(ResendClient resend) : IEmailSender
    {
        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Recuperação de senha — Mainstay";
            message.HtmlBody = BuildHtml(toName, resetLink);

            await resend.EmailSendAsync(message, cancellationToken);
        }

        private static string BuildHtml(string name, string resetLink) => $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width,initial-scale=1" /></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">

                      <!-- Logo -->
                      <tr>
                        <td align="center" style="padding-bottom:24px;">
                          <img src="https://auth.mainstay.com.br/logo-mainstay.png" alt="Mainstay" width="160" style="display:block;" />
                        </td>
                      </tr>

                      <!-- Card -->
                      <tr>
                        <td style="background:#ffffff;border-radius:10px;padding:40px 36px;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                          <h2 style="margin:0 0 8px;color:#1F3B61;font-size:22px;font-weight:700;">Recuperação de senha</h2>
                          <p style="margin:0 0 16px;color:#374151;font-size:15px;line-height:1.6;">Olá, {name}.</p>
                          <p style="margin:0 0 28px;color:#374151;font-size:15px;line-height:1.6;">Recebemos uma solicitação para redefinir a senha da sua conta. Clique no botão abaixo para criar uma nova senha:</p>
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="border-radius:6px;background:#1F3B61;">
                                <a href="{resetLink}" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">Redefinir senha</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:28px 0 0;color:#6b7280;font-size:13px;line-height:1.6;">Este link expira em <strong>24 horas</strong>. Se você não solicitou a recuperação de senha, ignore este e-mail — sua conta permanece segura.</p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td align="center" style="padding-top:24px;">
                          <p style="margin:0;color:#9ca3af;font-size:12px;">© {DateTimeOffset.UtcNow.Year} Mainstay. Todos os direitos reservados.</p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
