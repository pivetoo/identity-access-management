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

        public async Task SendPasswordResetConfirmationEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Sua senha foi redefinida — Mainstay";
            message.HtmlBody = BuildSecurityAlertHtml(
                toName,
                "Senha redefinida com sucesso",
                "Sua senha foi redefinida com sucesso.",
                "Se você não realizou esta ação, sua conta pode estar comprometida. Entre em contato com o suporte imediatamente."
            );

            await resend.EmailSendAsync(message, cancellationToken);
        }

        public async Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Sua senha foi alterada — Mainstay";
            message.HtmlBody = BuildSecurityAlertHtml(
                toName,
                "Senha alterada",
                "Sua senha foi alterada com sucesso.",
                "Se você não realizou esta alteração, sua conta pode estar comprometida. Entre em contato com o suporte imediatamente."
            );

            await resend.EmailSendAsync(message, cancellationToken);
        }

        public async Task SendAccountDeactivatedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Sua conta foi desativada — Mainstay";
            message.HtmlBody = BuildSecurityAlertHtml(
                toName,
                "Conta desativada",
                "Sua conta foi desativada e você não poderá mais acessar o sistema.",
                "Se acredita que isso foi um engano, entre em contato com o administrador do sistema."
            );

            await resend.EmailSendAsync(message, cancellationToken);
        }

        public async Task SendAdminInvitationEmailAsync(string toEmail, string companyName, string systemApplicationName, string setupLink, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = $"Configure seu acesso — {systemApplicationName}";
            message.HtmlBody = BuildAdminInvitationHtml(companyName, systemApplicationName, setupLink);

            await resend.EmailSendAsync(message, cancellationToken);
        }

        public async Task SendClientAdminInvitationEmailAsync(string toEmail, string companyName, IReadOnlyCollection<string> systemNames, string setupLink, CancellationToken cancellationToken = default)
        {
            var message = new EmailMessage();
            message.From = "no-reply@mainstay.com.br";
            message.To.Add(toEmail);
            message.Subject = "Configure seu acesso — Mainstay";
            message.HtmlBody = BuildClientAdminInvitationHtml(companyName, setupLink);

            await resend.EmailSendAsync(message, cancellationToken);
        }

        private static string BuildSecurityAlertHtml(string name, string title, string body, string footer) => $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width,initial-scale=1" /></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                      <tr>
                        <td align="center" style="padding-bottom:24px;">
                          <img src="https://auth.mainstay.com.br/logo-mainstay.png" alt="Mainstay" width="160" style="display:block;" />
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#ffffff;border-radius:10px;padding:40px 36px;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                          <h2 style="margin:0 0 8px;color:#1F3B61;font-size:22px;font-weight:700;">{title}</h2>
                          <p style="margin:0 0 16px;color:#374151;font-size:15px;line-height:1.6;">Olá, {name}.</p>
                          <p style="margin:0 0 24px;color:#374151;font-size:15px;line-height:1.6;">{body}</p>
                          <p style="margin:0;padding:16px;background:#fff7ed;border-left:4px solid #f97316;border-radius:4px;color:#9a3412;font-size:13px;line-height:1.6;">{footer}</p>
                        </td>
                      </tr>
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

        private static string BuildAdminInvitationHtml(string companyName, string systemApplicationName, string setupLink) => $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width,initial-scale=1" /></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                      <tr>
                        <td align="center" style="padding-bottom:24px;">
                          <img src="https://auth.mainstay.com.br/logo-mainstay.png" alt="Mainstay" width="160" style="display:block;" />
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#ffffff;border-radius:10px;padding:40px 36px;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                          <h2 style="margin:0 0 8px;color:#1F3B61;font-size:22px;font-weight:700;">Configure seu acesso</h2>
                          <p style="margin:0 0 16px;color:#374151;font-size:15px;line-height:1.6;">Olá, <strong>{companyName}</strong>.</p>
                          <p style="margin:0 0 8px;color:#374151;font-size:15px;line-height:1.6;">Um contrato com o sistema <strong>{systemApplicationName}</strong> foi criado para sua empresa.</p>
                          <p style="margin:0 0 28px;color:#374151;font-size:15px;line-height:1.6;">Clique no botão abaixo para configurar o usuário <strong>Administrador</strong> e definir suas credenciais de acesso:</p>
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="border-radius:6px;background:#1F3B61;">
                                <a href="{setupLink}" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">Configurar meu acesso</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:28px 0 0;color:#6b7280;font-size:13px;line-height:1.6;">Este link expira em <strong>7 dias</strong>. Se não reconhece este convite, ignore este e-mail com segurança.</p>
                        </td>
                      </tr>
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

        private static string BuildClientAdminInvitationHtml(string companyName, string setupLink)
        {
            return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width,initial-scale=1" /></head>
            <body style="margin:0;padding:0;background:#f4f4f5;font-family:sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                      <tr>
                        <td align="center" style="padding-bottom:24px;">
                          <img src="https://auth.mainstay.com.br/logo-mainstay.png" alt="Mainstay" width="160" style="display:block;" />
                        </td>
                      </tr>
                      <tr>
                        <td style="background:#ffffff;border-radius:10px;padding:40px 36px;box-shadow:0 1px 4px rgba(0,0,0,.08);">
                          <h2 style="margin:0 0 8px;color:#1F3B61;font-size:22px;font-weight:700;">Configure seu acesso</h2>
                          <p style="margin:0 0 16px;color:#374151;font-size:15px;line-height:1.6;">Olá, <strong>{companyName}</strong>.</p>
                          <p style="margin:0 0 28px;color:#374151;font-size:15px;line-height:1.6;">Sua conta na <strong>Mainstay</strong> foi criada. Clique no botão abaixo para configurar o usuário <strong>Administrador</strong> e definir suas credenciais de acesso:</p>
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="border-radius:6px;background:#1F3B61;">
                                <a href="{setupLink}" style="display:inline-block;padding:14px 32px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">Configurar meu acesso</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:28px 0 0;color:#6b7280;font-size:13px;line-height:1.6;">Este link expira em <strong>7 dias</strong>. Se não reconhece este convite, ignore este e-mail com segurança.</p>
                        </td>
                      </tr>
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
