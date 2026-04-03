using IdentityManagement.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class TemporaryTokenService : ITemporaryTokenService
    {
        private readonly string secretKey;
        private readonly TimeSpan tokenLifetime = TimeSpan.FromMinutes(10);

        public TemporaryTokenService(IConfiguration configuration)
        {
            secretKey = configuration["Jwt:TemporarySecretKey"] ?? throw new ArgumentNullException("Jwt:TemporarySecretKey");
        }

        public string GenerateTemporaryToken(long userId)
        {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            [
                new Claim("user_id", userId.ToString()),
                new Claim("authentication_step", "contractSelection"),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ];

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "identity-management-temp",
                audience: "contract-selection",
                claims: claims,
                expires: DateTime.UtcNow.Add(tokenLifetime),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateTemporaryToken(string token, out long userId)
        {
            userId = 0;

            try
            {
                SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "identity-management-temp",
                    ValidateAudience = true,
                    ValidAudience = "contract-selection",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                string? claimUserId = principal.FindFirst("user_id")?.Value;
                string? authenticationStep = principal.FindFirst("authentication_step")?.Value;

                return authenticationStep == "contractSelection" && long.TryParse(claimUserId, out userId);
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateTemporaryTokenForUser(string token, long userId)
        {
            return ValidateTemporaryToken(token, out long tokenUserId) && tokenUserId == userId;
        }
    }
}
