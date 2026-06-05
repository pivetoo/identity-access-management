using Archon.Application.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.Migrations;
using Archon.Infrastructure.MultiTenancy;
using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Billing;
using IdentityManagement.Infrastructure.MultiTenancy;
using IdentityManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resend;

namespace IdentityManagement.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        private const string FixedTenantConnectionStringKey = "TenantDatabases:FixedTenantId:ConnectionString";

        public static IServiceCollection AddIdentityManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddArchonPersistence(configuration, typeof(ServiceCollectionExtensions).Assembly);
            services.Replace(ServiceDescriptor.Singleton<ITenantResolver, IdentityManagementTenantResolver>());
            services.RunMigrations(
                configuration,
                GetMigrationSchema(configuration),
                typeof(DatabaseMigrator).Assembly,
                typeof(ServiceCollectionExtensions).Assembly);
            services.AddServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

            // Gateway de cobranca: usa o Asaas quando ha ApiKey configurada; senao mantem o no-op.
            // Registro manual porque o nome nao termina em "Service" (nao e auto-descoberto).
            services.Configure<AsaasOptions>(configuration.GetSection(AsaasOptions.SectionName));

            string asaasApiKey = configuration[$"{AsaasOptions.SectionName}:ApiKey"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(asaasApiKey))
            {
                string asaasBaseUrl = configuration[$"{AsaasOptions.SectionName}:BaseUrl"] ?? string.Empty;
                services.AddHttpClient<IBillingGateway, AsaasBillingGateway>(client =>
                {
                    if (!string.IsNullOrWhiteSpace(asaasBaseUrl))
                    {
                        client.BaseAddress = new Uri(asaasBaseUrl.EndsWith('/') ? asaasBaseUrl : asaasBaseUrl + "/");
                    }

                    client.DefaultRequestHeaders.Remove("access_token");
                    client.DefaultRequestHeaders.Add("access_token", asaasApiKey);
                });
            }
            else
            {
                services.AddScoped<IBillingGateway, NoopBillingGateway>();
            }

            string selfConnectionString = configuration[FixedTenantConnectionStringKey]
                ?? throw new InvalidOperationException($"Configuração obrigatória ausente: {FixedTenantConnectionStringKey}.");
            services.AddSingleton<ITenantProvisioner>(_ => new PostgresTenantProvisioner(selfConnectionString));

            services.AddOptions();
            services.AddHttpClient<ResendClient>();
            services.Configure<ResendClientOptions>(o => o.ApiToken = configuration["Resend:ApiKey"] ?? string.Empty);
            services.AddScoped<IEmailSender, ResendEmailSender>();

            return services;
        }

        private static string GetMigrationSchema(IConfiguration configuration)
        {
            TenantDatabaseOptions tenantDatabaseOptions = new TenantDatabaseOptions();
            configuration.Bind(tenantDatabaseOptions);

            string? schema = tenantDatabaseOptions.TenantDatabases
                .Select(item => item.Value.Schema)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return schema ?? "public";
        }
    }
}
