using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Responses.Clients;

namespace IdentityManagement.Application.Services
{
    public interface IClientOnboardingService
    {
        /// <param name="sendInvitationEmail">
        /// Falso no cadastro publico: ali a pessoa acabou de clicar num link do proprio e-mail e ja
        /// cai na tela de senha, entao um segundo e-mail chegaria junto com o primeiro dizendo
        /// quase a mesma coisa. O onboarding administrativo continua enviando, porque ali o convite
        /// E o unico caminho ate o cliente.
        /// </param>
        Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, bool sendInvitationEmail = true, CancellationToken ct = default);
    }
}
