using System.Security.Cryptography;
using System.Text;
using Archon.Application.MultiTenancy;
using Archon.Core.ValueObjects;
using Archon.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IdentityManagement.Infrastructure.MultiTenancy
{
    public sealed class IdentityManagementTenantResolver : ITenantResolver
    {
        private const string FixedTenantSection = "TenantDatabases:FixedTenantId";

        private readonly ConfigurationTenantResolver configurationResolver;
        private readonly IMemoryCache cache;
        private readonly IdentityCatalogOptions options;
        private readonly TenantInfo selfTenant;

        public IdentityManagementTenantResolver(
            ConfigurationTenantResolver configurationResolver,
            IConfiguration configuration,
            IMemoryCache cache,
            IOptions<IdentityCatalogOptions> options)
        {
            this.configurationResolver = configurationResolver;
            this.cache = cache;
            this.options = options.Value;

            IConfigurationSection section = configuration.GetSection(FixedTenantSection);
            string? connectionString = section["ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Configuração obrigatória ausente: {FixedTenantSection}:ConnectionString.");
            }

            selfTenant = new TenantInfo
            {
                TenantId = section.Key,
                CompanyName = section["CompanyName"] ?? string.Empty,
                ApplicationId = configuration["Jwt:Audience"] ?? string.Empty,
                ConnectionString = connectionString,
                Schema = section["Schema"] ?? "public",
                DatabaseProvider = DatabaseProvider.PostgreSql,
                ApiKey = section["ApiKey"]
            };
        }

        public Task<TenantInfo?> ResolveAsync(string? tenantId, CancellationToken cancellationToken = default)
        {
            return configurationResolver.ResolveAsync(tenantId, cancellationToken);
        }

        public async Task<TenantInfo?> ResolveByTenantAndApiKeyAsync(string? tenantId, string? apiKey, CancellationToken cancellationToken = default)
        {
            TenantInfo? configMatch = await configurationResolver.ResolveByTenantAndApiKeyAsync(tenantId, apiKey, cancellationToken);
            if (configMatch is not null)
            {
                return configMatch;
            }

            return await ResolveByApiKeyAsync(apiKey, cancellationToken);
        }

        public async Task<TenantInfo?> ResolveByApiKeyAsync(string? apiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            string normalized = apiKey.Trim();

            TenantInfo? configMatch = await configurationResolver.ResolveByApiKeyAsync(normalized, cancellationToken);
            if (configMatch is not null)
            {
                return configMatch;
            }

            string cacheKey = $"idm:tenantdatabases:apikey:{normalized}";
            if (cache.TryGetValue(cacheKey, out TenantInfo? cached))
            {
                return cached;
            }

            bool matched = await ApiKeyExistsInDatabaseAsync(normalized, cancellationToken);
            if (!matched)
            {
                return null;
            }

            TenantInfo tenant = new TenantInfo
            {
                TenantId = selfTenant.TenantId,
                CompanyName = selfTenant.CompanyName,
                ApplicationId = selfTenant.ApplicationId,
                ConnectionString = selfTenant.ConnectionString,
                Schema = selfTenant.Schema,
                DatabaseProvider = selfTenant.DatabaseProvider,
                ApiKey = normalized
            };

            cache.Set(cacheKey, tenant, options.CacheTtl);
            return tenant;
        }

        private async Task<bool> ApiKeyExistsInDatabaseAsync(string apiKey, CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(selfTenant.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM tenantdatabases WHERE apikey = @apikey AND isactive = TRUE LIMIT 1";
            command.Parameters.Add(new NpgsqlParameter("apikey", apiKey));

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null;
        }
    }
}
