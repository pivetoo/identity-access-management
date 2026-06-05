using IdentityManagement.Application.Services;
using IdentityManagement.Infrastructure.Tenancy;
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
        private readonly string username;
        private readonly string password;

        public PostgresTenantProvisioner(string selfConnectionString)
        {
            NpgsqlConnectionStringBuilder builder = new(selfConnectionString);
            host = builder.Host ?? "localhost";
            port = builder.Port;
            username = builder.Username ?? string.Empty;
            password = builder.Password ?? string.Empty;

            adminConnectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = "postgres",
                Username = username,
                Password = password
            }.ToString();
        }

        public string BuildTenantConnectionString(string databaseName)
        {
            AssertValid(databaseName);
            return new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = databaseName,
                Username = username,
                Password = password
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

        public async Task CreateDatabaseAsync(string databaseName, CancellationToken ct = default)
        {
            AssertValid(databaseName);
            await using NpgsqlConnection connection = new(adminConnectionString);
            await connection.OpenAsync(ct);
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
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

        private static void AssertValid(string databaseName)
        {
            if (!TenantNaming.IsValidIdentifier(databaseName))
            {
                throw new ArgumentException($"Nome de banco inválido: '{databaseName}'.", nameof(databaseName));
            }
        }
    }
}
