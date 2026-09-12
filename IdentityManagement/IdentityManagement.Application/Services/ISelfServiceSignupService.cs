using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Signup;

namespace IdentityManagement.Application.Services
{
    public interface ISelfServiceSignupService
    {
        /// <summary>
        /// Etapa 1: valida e registra o cadastro como PENDENTE, enviando o link de confirmacao.
        /// Nao provisiona nada.
        /// </summary>
        /// <summary>Planos e trial que o cadastro vai contratar. Mesma origem do Confirm.</summary>
        Task<SignupOfferResponse> GetOfferAsync(CancellationToken cancellationToken = default);

        Task<SignupResponse> SignupAsync(SignupRequest request, string confirmBaseUrl, string? sourceIp, CancellationToken cancellationToken = default);

        /// <summary>
        /// Etapa 2: confirma o e-mail e SO ENTAO provisiona empresa, contratos, bancos e assinatura.
        /// </summary>
        Task<SignupConfirmResponse> ConfirmAsync(SignupConfirmRequest request, string setupBaseUrl, CancellationToken cancellationToken = default);
    }
}
