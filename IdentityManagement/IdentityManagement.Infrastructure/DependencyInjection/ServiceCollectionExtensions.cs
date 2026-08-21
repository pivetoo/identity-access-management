using Archon.Application.MultiTenancy;
using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.Migrations;
using Archon.Infrastructure.MultiTenancy;
using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Billing;
using IdentityManagement.Infrastructure.Contact;
using IdentityManagement.Infrastructure.Signup;
using IdentityManagement.Infrastructure.MultiTenancy;
using IdentityManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
            services.Configure<SignupOptions>(configuration.GetSection(SignupOptions.SectionName));
            services.Configure<ContactOptions>(configuration.GetSection(ContactOptions.SectionName));

            string asaasApiKey = configuration[$"{AsaasOptions.SectionName}:ApiKey"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(asaasApiKey))
            {
                services.AddScoped<IBillingGateway, AsaasBillingGateway>();
            }
            else
            {
                services.AddScoped<IBillingGateway, NoopBillingGateway>();
            }

            string selfConnectionString = configuration[FixedTenantConnectionStringKey]
                ?? throw new InvalidOperationException($"Configuração obrigatória ausente: {FixedTenantConnectionStringKey}.");

            // Credenciais por sistema: cada banco de tenant e criado com OWNER da role do sistema
            // (app_agencycampaign, app_integrationplatform) e a connection string do tenant usa essa role.
            // A role administrativa (master) cria/dropa os bancos. Sem configuracao, cai na credencial da
            // propria conexao do IdM (compat com dev/testes single-role).
            TenantProvisioningOptions provisioningOptions = new();
            configuration.GetSection(TenantProvisioningOptions.SectionName).Bind(provisioningOptions);
            services.AddSingleton<ITenantProvisioner>(sp => new PostgresTenantProvisioner(
                selfConnectionString,
                provisioningOptions,
                sp.GetRequiredService<ILogger<PostgresTenantProvisioner>>()));

            services.AddOptions();
            services.AddHttpClient<ResendClient>();
            services.AddHttpClient<Archon.Infrastructure.RestApi.RestApi>();
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
