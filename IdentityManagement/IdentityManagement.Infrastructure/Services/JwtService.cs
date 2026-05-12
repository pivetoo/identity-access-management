using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class JwtService : IJwtService
    {
        private sealed class UserRoleClaimModel
        {
            public long Id { get; init; }

            public string Name { get; init; } = string.Empty;

            public bool IsRoot { get; init; }
        }

        private readonly DbContext dbContext;
        private readonly IConfiguration configuration;

        public JwtService(DbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        public async Task<string> GenerateAccessToken(User user, Contract contract, int lifetimeSeconds, string? sessionId = null, string? authorizedClientId = null, CancellationToken cancellationToken = default)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("name", user.Name),
                new Claim("email", user.Email),
                new Claim("preferred_username", user.Username),
                new Claim("contract_id", contract.Id.ToString()),
                new Claim("tenant_id", contract.Company.TenantId.ToString()),
                new Claim("system_application_name", contract.SystemApplication.Name),
                new Claim("company_name", contract.Company.LegalName)
            ];

            if (!string.IsNullOrWhiteSpace(authorizedClientId))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Azp, authorizedClientId));
            }

            List<UserRoleClaimModel> userRoles = await (
                from userRole in dbContext.Set<UserRole>().AsNoTracking()
                join role in dbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id &&
                      role.ContractId == contract.Id &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue
                select new UserRoleClaimModel
                {
                    Id = role.Id,
                    Name = role.Name,
                    IsRoot = role.IsRoot
                })
                .ToListAsync(cancellationToken);

            if (userRoles.Count == 0)
            {
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role_id", userRole.Id.ToString()));
                claims.Add(new Claim("role_name", userRole.Name));

                if (userRole.IsRoot)
                {
                    claims.Add(new Claim("root", "true"));
                    claims.Add(new Claim(ClaimTypes.Role, "Root"));
                }
            }

            List<string> accessResources = await (
                from userRole in dbContext.Set<UserRole>().AsNoTracking()
                join role in dbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                join roleAccessResource in dbContext.Set<RoleAccessResource>().AsNoTracking() on role.Id equals roleAccessResource.RoleId
                join accessResource in dbContext.Set<AccessResource>().AsNoTracking() on roleAccessResource.AccessResourceId equals accessResource.Id
                where userRole.UserId == user.Id &&
                      role.ContractId == contract.Id &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue &&
                      roleAccessResource.IsActive &&
                      accessResource.IsActive
                select accessResource.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (string accessResource in accessResources)
            {
                claims.Add(new Claim("permission", accessResource));
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                claims.Add(new Claim("session_id", sessionId));
                claims.Add(new Claim("sid", sessionId));
            }

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(lifetimeSeconds),
                Issuer = configuration["Jwt:Issuer"],
                Audience = contract.SystemApplication.Audience,
                SigningCredentials = await GetTokenSigningCredentials(cancellationToken)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> GenerateIdToken(User user, Contract contract, string clientId, int lifetimeSeconds, string? nonce, string? sessionId = null, CancellationToken cancellationToken = default)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            List<Claim> claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim("preferred_username", user.Username),
                new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("contract_id", contract.Id.ToString()),
                new Claim("company_name", contract.Company.LegalName),
                new Claim("system_application_name", contract.SystemApplication.Name)
            ];

            if (!string.IsNullOrWhiteSpace(nonce))
            {
                claims.Add(new Claim("nonce", nonce));
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                claims.Add(new Claim("sid", sessionId));
            }

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(lifetimeSeconds),
                Issuer = configuration["Jwt:Issuer"],
                Audience = clientId,
                SigningCredentials = await GetTokenSigningCredentials(cancellationToken)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<SigningCredentials> GetTokenSigningCredentials(CancellationToken cancellationToken)
        {
            SigningKey? signingKey = await dbContext.Set<SigningKey>()
                .AsNoTracking()
                .Where(item => item.IsActive &&
                               !item.RevokedAt.HasValue &&
                               item.Algorithm == SecurityAlgorithms.RsaSha256 &&
                               item.PrivateKeyEncrypted != string.Empty &&
                               DateTimeOffset.UtcNow >= item.NotBefore &&
                               (!item.ExpiresAt.HasValue || DateTimeOffset.UtcNow < item.ExpiresAt.Value))
                .OrderByDescending(item => item.NotBefore)
                .FirstOrDefaultAsync(cancellationToken);

            if (signingKey is null)
            {
                throw new InvalidOperationException("No active RSA signing key is configured.");
            }

            RSA rsa = RSA.Create();
            rsa.ImportFromPem(signingKey.PrivateKeyEncrypted);

            return new SigningCredentials(
                new RsaSecurityKey(rsa)
                {
                    KeyId = signingKey.KeyId
                },
                SecurityAlgorithms.RsaSha256);
        }

        public string GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public long? GetUserIdFromToken(string token)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
                string? userId = jwtToken.Claims.FirstOrDefault(item => item.Type == "user_id")?.Value;
                return long.TryParse(userId, out long parsedUserId) ? parsedUserId : null;
            }
            catch
            {
                return null;
            }
        }

        public DateTimeOffset GetTokenExpiration(string token)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                return DateTimeOffset.MinValue;
            }
        }
    }
}
