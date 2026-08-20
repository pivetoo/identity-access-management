using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Signup;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace IdentityManagement.Api.Controllers
{
    /// <summary>
    /// Cadastro publico de agencia (self-service).
    ///
    /// E o unico endpoint anonimo do sistema que CRIA infraestrutura (empresa, contratos, banco de
    /// tenant e assinatura). Por isso carrega tres travas: <c>[AllowAnonymous]</c> explicito para
    /// nao depender do default do pipeline, rate limiting por IP numa politica propria e estreita
    /// (3 por hora), e resolucao de plano/sistemas pela configuracao do servidor — nunca pelo corpo
    /// da requisicao.
    /// </summary>
    public sealed class SignupController : ApiControllerBase
    {
        private readonly ISelfServiceSignupService signupService;
        private readonly IConfiguration configuration;

        public SignupController(ISelfServiceSignupService signupService, IConfiguration configuration)
        {
            this.signupService = signupService;
            this.configuration = configuration;
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Signup)]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] SignupRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            // A base do link de convite vem do emissor, nunca do corpo: aceitar do cliente
            // permitiria mandar o convite de administrador apontando para um dominio atacante.
            string setupBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;

            SignupResponse response = await signupService.SignupAsync(request, setupBaseUrl, cancellationToken);

            return Http201(response, Localizer["signup.created"]);
        }
    }
}
