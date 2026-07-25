using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.MultiTenancy;
using IdentityManagement.Application.Localization;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
IServiceProvider? rootServiceProvider = null;

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("IdentityManagementCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
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
builder.Services.AddAuthorization();
builder.Services.AddArchonApi(builder.Configuration, typeof(IdentityManagementResource));
builder.Services.AddIdentityManagementInfrastructure(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);
builder.Services.AddHostedService<IdentityManagement.Api.BackgroundJobs.SubscriptionDunningJob>();

var app = builder.Build();
rootServiceProvider = app.Services;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("IdentityManagementCors");
app.UseArchonApi();
app.UseAuthentication();
// Depois da autenticacao de proposito: o tenant sai de claim ja validada.
app.UseArchonTenantResolution();
app.UseAuthorization();
app.UseSessionValidation();

app.MapControllers();

app.Run();

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
