using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.MultiTenancy;
using IdentityManagement.Api;
using IdentityManagement.Application.Localization;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
IServiceProvider? rootServiceProvider = null;

// Credenciais em arquivo separado do resto da configuracao: quem rotaciona segredo mexe num
// arquivo de poucas linhas, e nao no meio de URLs, CORS e flags. Opcional de proposito — em
// desenvolvimento nao existe, e o appsettings normal continua bastando.
//
// Fica DEPOIS das fontes padrao para sobrepor o que estiver no appsettings; e ANTES das variaveis
// de ambiente, que seguem tendo a ultima palavra.
builder.Configuration.AddJsonFile("secrets.Production.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

// Origens permitidas por configuracao. `AllowAnyOrigin` num provedor de identidade deixa qualquer
// pagina da web conversar com /connect/token e /api/auth a partir do navegador da vitima; a lista
// real e curta e conhecida (as SPAs dos sistemas do ecossistema).
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins is not configured. Informe as origens permitidas explicitamente.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("IdentityManagementCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// O rate limiting particiona por IP do cliente. Atras de proxy reverso, `RemoteIpAddress` e o IP do
// PROXY: sem tratar o X-Forwarded-For, todo o trafego cai numa unica particao e o limite viraria um
// teto global — o proprio limitador derrubaria o login de todo mundo.
//
// Confiar no cabecalho e opt-in de proposito: quem consegue falar direto com a aplicacao pode forjar
// o X-Forwarded-For e escapar do limite. So ligue quando a aplicacao for inalcancavel a nao ser pelo
// proxy (o caso do container atras do nginx).
bool trustForwardedHeaders = builder.Configuration.GetValue("ReverseProxy:TrustForwardedHeaders", false);
if (trustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Endpoints anonimos de credencial: login, troca de token, esqueci/redefinir senha. Sem isso,
    // o provedor de identidade aceita tentativa de senha sem limite nenhum.
    options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "sem-ip",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 20),
            Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:AuthWindowMinutes", 1)),
            QueueLimit = 0
        }));

    // Cadastro publico: cada sucesso provisiona um banco de tenant. O custo de uma chamada aqui e
    // ordens de grandeza maior que o de uma tentativa de login, entao a janela e por hora.
    options.AddPolicy(RateLimitPolicies.Signup, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "sem-ip",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:SignupPermitLimit", 3),
            Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:SignupWindowMinutes", 60)),
            QueueLimit = 0
        }));

    // Contato do site: anonimo e dispara e-mail, entao vale a mesma disciplina do cadastro.
    options.AddPolicy(RateLimitPolicies.Contact, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "sem-ip",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:ContactPermitLimit", 5),
            Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:ContactWindowMinutes", 60)),
            QueueLimit = 0
        }));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        string issuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

        string audience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) => ResolveSigningKeys(rootServiceProvider),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Default-deny. Sem isso, endpoint que esquece o [RequireAccess] nasce publico e o sintoma e
    // silencio: ele funciona, so que para qualquer um. Foi assim que AccessResources/Sync ficou
    // aberto para POST anonimo.
    //
    // A politica tambem aceita requisicao que traz credencial de integracao (Basic / X-Api-Key),
    // porque quem valida essa credencial e o proprio [RequireAccess], que roda depois daqui —
    // exigir usuario autenticado aqui derrubaria a comunicacao servico-a-servico.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(context =>
            context.User.Identity?.IsAuthenticated == true ||
            (context.Resource is HttpContext httpContext && CarriesIntegrationCredential(httpContext.Request)))
        .Build();
});
builder.Services.AddArchonApi(builder.Configuration, typeof(IdentityManagementResource));
builder.Services.AddIdentityManagementInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);
builder.Services.AddHostedService<IdentityManagement.Api.BackgroundJobs.SubscriptionDunningJob>();

var app = builder.Build();
rootServiceProvider = app.Services;

// Falha no startup, e nao no primeiro webhook: fora de Development o token e obrigatorio, a menos
// que alguem tenha declarado explicitamente que aceita webhook sem autenticacao.
if (!app.Environment.IsDevelopment())
{
    bool allowUnauthenticatedWebhook = builder.Configuration.GetValue("Asaas:AllowUnauthenticatedWebhook", false);
    string webhookToken = builder.Configuration["Asaas:WebhookToken"] ?? string.Empty;

    if (string.IsNullOrEmpty(webhookToken) && !allowUnauthenticatedWebhook)
    {
        throw new InvalidOperationException("Asaas:WebhookToken is not configured. O webhook de billing altera estado de assinatura e nao pode ficar anonimo.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (trustForwardedHeaders)
{
    // Antes de tudo: o IP do cliente precisa estar resolvido quando o rate limiter partition-ar.
    app.UseForwardedHeaders();
}
else if (!app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        "ReverseProxy:TrustForwardedHeaders esta desligado. Se a aplicacao roda atras de proxy, o rate limiting de auth vai particionar pelo IP do proxy — ou seja, um teto unico para todos os usuarios.");
}

app.UseHttpsRedirection();
app.UseCors("IdentityManagementCors");
app.UseRateLimiter();
app.UseArchonApi();
app.UseAuthentication();
// Depois da autenticacao de proposito: o tenant sai de claim ja validada.
app.UseArchonTenantResolution();
app.UseAuthorization();
// UseSessionValidation() removido: nao existe implementacao de ISessionValidator no ecossistema,
// entao a chamada rodava sem validar nada e dava falsa impressao de barrar sessao revogada.
// Reativar junto com a implementacao (a tabela loginsessions ja existe aqui, falta o endpoint).

app.MapControllers();

app.Run();

/// <summary>
/// Indica apenas que a requisicao carrega alguma credencial de integracao. A validacao de verdade
/// e do <c>[RequireAccess]</c>; aqui so evitamos que requisicao sem credencial nenhuma alcance
/// endpoint que esqueceu a marcacao.
/// </summary>
static bool CarriesIntegrationCredential(HttpRequest request)
{
    if (!string.IsNullOrWhiteSpace(request.Headers["X-Api-Key"].FirstOrDefault()))
    {
        return true;
    }

    string? authorization = request.Headers.Authorization.FirstOrDefault();
    return authorization is not null && authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase);
}

static IEnumerable<SecurityKey> ResolveSigningKeys(IServiceProvider? serviceProvider)
{
    List<SecurityKey> keys = [];
    if (serviceProvider is null)
    {
        return keys;
    }

    using IServiceScope scope = serviceProvider.CreateScope();

    // Este callback roda FORA do request pipeline (scope novo a partir do root), entao o
    // TenantResolutionMiddleware nunca setou o tenant aqui. Como o IdM roda em FixedTenant,
    // setamos o tenant fixo explicitamente antes de tocar o DbContext - sem depender de fallback.
    ITenantResolver tenantResolver = scope.ServiceProvider.GetRequiredService<ITenantResolver>();
    TenantInfo? tenant = tenantResolver.ResolveAsync("FixedTenantId", CancellationToken.None).GetAwaiter().GetResult();
    if (tenant is not null && scope.ServiceProvider.GetRequiredService<ITenantContext>() is MultiTenantContext multiTenantContext)
    {
        multiTenantContext.SetTenant(tenant);
    }

    DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

    List<SigningKey> signingKeys = dbContext.Set<SigningKey>()
        .AsNoTracking()
        .Where(item => item.IsActive &&
                       !item.RevokedAt.HasValue &&
                       item.Algorithm == SecurityAlgorithms.RsaSha256 &&
                       DateTimeOffset.UtcNow >= item.NotBefore &&
                       (!item.ExpiresAt.HasValue || DateTimeOffset.UtcNow < item.ExpiresAt.Value))
        .OrderByDescending(item => item.NotBefore)
        .ToList();

    foreach (SigningKey signingKey in signingKeys)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(signingKey.PublicKeyPem);
        keys.Add(new RsaSecurityKey(rsa)
        {
            KeyId = signingKey.KeyId
        });
    }

    return keys;
}
