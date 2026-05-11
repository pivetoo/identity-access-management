using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly DbContext dbContext;
        private readonly IUserService userService;
        private readonly IContractService contractService;
        private readonly IEmailSender emailSender;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public AuthService(DbContext dbContext, IUserService userService, IContractService contractService, IEmailSender emailSender, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.dbContext = dbContext;
            this.userService = userService;
            this.contractService = contractService;
            this.emailSender = emailSender;
            this.Localizer = Localizer;
        }

        public async Task<ContractSelectionResponse> IdentifyUser(IdentifyUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await userService.Authenticate(request.Username, request.Password, cancellationToken);
            if (user is null)
            {
                throw new UnauthorizedAccessException(Localizer["auth.invalidCredentials"]);
            }

            long? requestedSystemApplicationId = await GetRequestedSystemApplicationId(request.AuthorizeUrl, cancellationToken);
            IReadOnlyCollection<ContractSelectionResponseItem> availableContracts = await contractService.GetActiveContractSelectionsByUserId(user.Id, requestedSystemApplicationId, cancellationToken);
            if (availableContracts.Count == 0)
            {
                throw new UnauthorizedAccessException(Localizer["auth.user.noActiveContracts"]);
            }

            await RevokeActivePendingAuthorizationSessions(user.Id, cancellationToken);

            string? authorizeRequestHash = string.IsNullOrWhiteSpace(request.AuthorizeUrl)
                ? null
                : ComputeAuthorizeRequestHash(request.AuthorizeUrl);

            PendingAuthorizationSession authorizationSession = new(user.Id, GenerateOpaqueToken(), authorizeRequestHash);
            dbContext.Set<PendingAuthorizationSession>().Add(authorizationSession);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new ContractSelectionResponse
            {
                AuthenticationStep = "contractSelection",
                UserId = user.Id,
                UserName = user.Name,
                UserEmail = user.Email,
                AuthorizationSessionToken = authorizationSession.Token,
                AvailableContracts = availableContracts.ToList()
            };
        }

        public Task<bool> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            return userService.ChangePassword(request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
        }

        public async Task<UserResponse?> GetUserByUsername(string username, CancellationToken cancellationToken = default)
        {
            var user = await userService.GetByUsername(username, cancellationToken);
            return user is null ? null : ToUserResponse(user);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string resetBaseUrl, CancellationToken cancellationToken = default)
        {
            var user = await dbContext.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

            // Responde silenciosamente para não expor quais e-mails estão cadastrados
            if (user is null) return;

            string token = GenerateOpaqueToken();
            var resetToken = new PasswordResetToken(user.Id, token, DateTimeOffset.UtcNow.AddHours(24));
            dbContext.Set<PasswordResetToken>().Add(resetToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            string resetLink = $"{resetBaseUrl.TrimEnd('/')}/reset-password?token={token}";
            await emailSender.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink, cancellationToken);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var resetToken = await dbContext.Set<PasswordResetToken>()
                .AsTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

            if (resetToken is null || !resetToken.IsValid()) return false;

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            resetToken.User.ChangePassword(passwordHash);
            resetToken.MarkAsUsed();
            await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static UserResponse ToUserResponse(IdentityManagement.Domain.Entities.User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private async Task RevokeActivePendingAuthorizationSessions(long userId, CancellationToken cancellationToken)
        {
            List<PendingAuthorizationSession> sessions = await dbContext.Set<PendingAuthorizationSession>()
                .AsTracking()
                .Where(item => item.UserId == userId &&
                               !item.IsUsed &&
                               !item.IsRevoked &&
                               DateTimeOffset.UtcNow < item.ExpiresAt)
                .ToListAsync(cancellationToken);

            foreach (PendingAuthorizationSession session in sessions)
            {
                session.Revoke();
            }
        }

        private async Task<long?> GetRequestedSystemApplicationId(string? authorizeUrl, CancellationToken cancellationToken)
        {
            string clientId = GetAuthorizeClientId(authorizeUrl);
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            return await dbContext.Set<OAuthClient>()
                .AsNoTracking()
                .Where(item => item.ClientId == clientId && item.IsActive)
                .Select(item => (long?)item.SystemApplicationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static string GetAuthorizeClientId(string? authorizeUrl)
        {
            if (string.IsNullOrWhiteSpace(authorizeUrl))
            {
                return string.Empty;
            }

            try
            {
                Uri uri = new(authorizeUrl);
                Dictionary<string, string> parameters = ParseQuery(uri.Query);
                return parameters.GetValueOrDefault("client_id") ?? string.Empty;
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }

        private static string GenerateOpaqueToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncoder.Encode(bytes);
        }

        private static string ComputeAuthorizeRequestHash(string authorizeUrl)
        {
            Uri uri;
            try
            {
                uri = new Uri(authorizeUrl);
            }
            catch (UriFormatException)
            {
                throw new InvalidOperationException("invalid_authorize_url");
            }

            Dictionary<string, string> parameters = ParseQuery(uri.Query);
            string normalizedRequest = string.Join("|", [
                $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}",
                parameters.GetValueOrDefault("client_id") ?? string.Empty,
                parameters.GetValueOrDefault("redirect_uri") ?? string.Empty,
                parameters.GetValueOrDefault("response_type") ?? string.Empty,
                parameters.GetValueOrDefault("scope") ?? string.Empty,
                parameters.GetValueOrDefault("state") ?? string.Empty,
                parameters.GetValueOrDefault("nonce") ?? string.Empty,
                parameters.GetValueOrDefault("code_challenge") ?? string.Empty,
                parameters.GetValueOrDefault("code_challenge_method") ?? string.Empty
            ]);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRequest));
            return Base64UrlEncoder.Encode(hash);
        }

        private static Dictionary<string, string> ParseQuery(string queryString)
        {
            return queryString
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part =>
                {
                    string[] pair = part.Split('=', 2);
                    string key = Uri.UnescapeDataString(pair[0]);
                    string value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : string.Empty;
                    return new KeyValuePair<string, string>(key, value);
                })
                .GroupBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
        }

    }
}
