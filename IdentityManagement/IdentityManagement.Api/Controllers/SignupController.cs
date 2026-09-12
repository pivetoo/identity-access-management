using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Signup;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    /// <summary>
    /// Cadastro publico de agencia (self-service).
    ///
    /// Fluxo em duas etapas: <c>Create</c> so registra um cadastro pendente e manda o link de
    /// confirmacao; <c>Confirm</c> e o unico que CRIA infraestrutura (empresa, contratos, bancos de
    /// tenant e assinatura). Provisionar so depois do clique tira do endpoint anonimo o poder de
    /// criar banco no Postgres.
    ///
    /// As duas etapas carregam as mesmas travas: <c>[AllowAnonymous]</c> explicito para nao depender
    /// do default do pipeline, rate limiting por IP numa politica propria e estreita, e resolucao de
    /// plano/sistemas pela configuracao do servidor — nunca pelo corpo da requisicao.
    /// </summary>
    public sealed class SignupController : ApiControllerBase
    {
        private readonly ISelfServiceSignupService signupService;
        private readonly IConfiguration configuration;

        // O Localizer do ApiControllerBase aponta para o resource do FRAMEWORK: chave do sistema
        // passada por ele volta crua na resposta. Por isso o localizador proprio.
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public SignupController(ISelfServiceSignupService signupService, IConfiguration configuration, IStringLocalizer<IdentityManagementResource> localizer)
        {
            this.signupService = signupService;
            this.configuration = configuration;
            Localizer = localizer;
        }

        /// <summary>
        /// Plano e preco que o cadastro vai contratar. A tela renderiza disto em vez de texto fixo.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [GetEndpoint]
        public async Task<IActionResult> Offer(CancellationToken cancellationToken)
        {
            return Http200(await signupService.GetOfferAsync(cancellationToken));
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

            // A base do link vem do emissor, nunca do corpo: aceitar do cliente permitiria mandar
            // o link de confirmacao apontando para um dominio atacante.
            string baseUrl = configuration["Oidc:Issuer"] ?? string.Empty;

            string? sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            SignupResponse response = await signupService.SignupAsync(request, baseUrl, sourceIp, cancellationToken);

            return Http201(response, Localizer["signup.created"]);
        }

        /// <summary>
        /// Etapa 2: confirma o e-mail e provisiona.
        ///
        /// Politica LARGA de proposito, ao contrario da etapa 1. O que limita o provisionamento aqui
        /// nao e o rate limit e sim o token: so confirma quem recebeu um, e a quantidade de tokens
        /// validos por IP ja esta presa no limite estreito do <c>Create</c>. Reusar a politica
        /// estreita nesta rota so criaria um jeito de o cliente legitimo tomar 429 no clique do
        /// e-mail, segurando um link valido que nao funciona. O limite largo cobre o unico abuso que
        /// sobra: martelar token invalido, que morre na consulta sem provisionar nada.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [PostEndpoint]
        public async Task<IActionResult> Confirm([FromBody] SignupConfirmRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            string setupBaseUrl = configuration["Oidc:Issuer"] ?? string.Empty;

            SignupConfirmResponse response = await signupService.ConfirmAsync(request, setupBaseUrl, cancellationToken);

            return Http200(response, Localizer["signup.confirmed"]);
        }
    }
}
