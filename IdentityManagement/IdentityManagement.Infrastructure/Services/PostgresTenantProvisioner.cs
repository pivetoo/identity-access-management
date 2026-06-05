using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class PostgresTenantProvisioner : ITenantProvisioner
    {
        private readonly string adminConnectionString;
        private readonly string host;
        private readonly int port;
        private readonly string adminUsername;
        private readonly string adminPassword;
        private readonly IReadOnlyDictionary<string, DatabaseCredential> systemCredentials;
        private readonly ILogger<PostgresTenantProvisioner> logger;

        public PostgresTenantProvisioner(string selfConnectionString, TenantProvisioningOptions? options = null, ILogger<PostgresTenantProvisioner>? logger = null)
        {
            this.logger = logger ?? NullLogger<PostgresTenantProvisioner>.Instance;
            NpgsqlConnectionStringBuilder builder = new(selfConnectionString);
            host = builder.Host ?? "localhost";
            port = builder.Port;

            // Credencial administrativa (CREATE/DROP DATABASE): usa a role master quando configurada;
            // caso contrario reaproveita a credencial da propria conexao do IdM (compat com dev/testes).
            DatabaseCredential? admin = options?.Admin;
            bool hasAdmin = admin is not null && !string.IsNullOrWhiteSpace(admin.Username);
            adminUsername = hasAdmin ? admin!.Username : builder.Username ?? string.Empty;
            adminPassword = hasAdmin ? admin!.Password : builder.Password ?? string.Empty;

            systemCredentials = NormalizeCredentials(options?.SystemCredentials);

            adminConnectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = "postgres",
                Username = adminUsername,
                Password = adminPassword
            }.ToString();
        }

        public string BuildTenantConnectionString(string databaseName, string audience)
        {
            AssertValid(databaseName);
            DatabaseCredential credential = ResolveCredential(audience);
            return new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = databaseName,
                Username = credential.Username,
                Password = credential.Password
            }.ToString();
        }

        public string GenerateApiKey()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncoder.Encode(bytes);
        }

        public async Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken ct = default)
        {
            await using NpgsqlConnection connection = new(adminConnectionString);
            await connection.OpenAsync(ct);
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
            command.Parameters.Add(new NpgsqlParameter("n", databaseName));
            object? result = await command.ExecuteScalarAsync(ct);
            return result is not null;
        }

        public async Task CreateDatabaseAsync(string databaseName, string audience, CancellationToken ct = default)
        {
            AssertValid(databaseName);
            string? owner = ResolveOwner(audience);

            await using NpgsqlConnection connection = new(adminConnectionString);
            await connection.OpenAsync(ct);
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = owner is null
                ? $"CREATE DATABASE \"{databaseName}\""
                : $"CREATE DATABASE \"{databaseName}\" OWNER \"{owner}\"";
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task DropDatabaseAsync(string databaseName, CancellationToken ct = default)
        {
            AssertValid(databaseName);
            await using NpgsqlConnection connection = new(adminConnectionString);
            await connection.OpenAsync(ct);
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync(ct);
        }

        private DatabaseCredential ResolveCredential(string audience)
        {
            if (systemCredentials.TryGetValue(NormalizeKey(audience), out DatabaseCredential? credential)
                && !string.IsNullOrWhiteSpace(credential.Username))
            {
                return credential;
            }

            // Sem credencial dedicada: reaproveita a credencial administrativa. Em deployment multi-sistema
            // (ha credenciais de outros sistemas configuradas) isso defeitaria o isolamento por role, entao avisa.
            if (systemCredentials.Count > 0)
            {
                logger.LogWarning(
                    "Tenant connection string for audience '{Audience}' is falling back to the admin role '{AdminRole}': no dedicated credential configured under TenantProvisioning:SystemCredentials. Per-system isolation is not applied for this tenant.",
                    audience,
                    adminUsername);
            }

            return new DatabaseCredential { Username = adminUsername, Password = adminPassword };
        }

        private string? ResolveOwner(string audience)
        {
            if (!systemCredentials.TryGetValue(NormalizeKey(audience), out DatabaseCredential? credential)
                || string.IsNullOrWhiteSpace(credential.Username))
            {
                return null;
            }

            if (!TenantNaming.IsValidIdentifier(credential.Username))
            {
                throw new InvalidOperationException($"Role de owner invalida para o sistema '{audience}': '{credential.Username}'.");
            }

            return credential.Username;
        }

        private static IReadOnlyDictionary<string, DatabaseCredential> NormalizeCredentials(IDictionary<string, DatabaseCredential>? source)
        {
            Dictionary<string, DatabaseCredential> normalized = new(StringComparer.Ordinal);
            if (source is null)
            {
                return normalized;
            }

            foreach (KeyValuePair<string, DatabaseCredential> entry in source)
            {
                normalized[NormalizeKey(entry.Key)] = entry.Value;
            }

            return normalized;
        }

        private static string NormalizeKey(string audience)
        {
            return (audience ?? string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void AssertValid(string databaseName)
        {
            if (!TenantNaming.IsValidIdentifier(databaseName))
            {
                throw new ArgumentException($"Nome de banco inválido: '{databaseName}'.", nameof(databaseName));
            }
        }
    }
}
