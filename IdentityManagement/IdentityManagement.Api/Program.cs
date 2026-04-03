using Archon.Api.DependencyInjection;
using Archon.Api.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using IdentityManagement.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddArchonApi(builder.Configuration);
builder.Services.AddIdentityManagementInfrastructure(builder.Configuration);
builder.Services.AddArchonAuthentication(builder.Configuration);
builder.Services.AddServicesFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseArchonApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionValidation();
app.UseIdentityManagementUserSync();

app.MapControllers();

await app.UseArchonAccessSyncAsync();

app.Run();
