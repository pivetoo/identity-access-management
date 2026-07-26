using Archon.Core.Entities;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Domain.Entities
{
    public class User : Entity
    {
        private readonly List<AuthorizationCode> authorizationCodes = [];
        private readonly List<RefreshToken> refreshTokens = [];
        private readonly List<UserRole> userRoles = [];

        public string Username { get; private set; } = string.Empty;

        public string Email { get; private set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string? AvatarUrl { get; private set; }

        public bool IsActive { get; private set; } = true;

        public DateTimeOffset? LastLoginAt { get; private set; }

        public int FailedLoginAttempts { get; private set; }

        public DateTimeOffset? LockedUntil { get; private set; }

        public Language PreferredLanguage { get; private set; } = Language.PtBr;

        public IReadOnlyCollection<AuthorizationCode> AuthorizationCodes => authorizationCodes.AsReadOnly();

        public IReadOnlyCollection<RefreshToken> RefreshTokens => refreshTokens.AsReadOnly();

        public IReadOnlyCollection<UserRole> UserRoles => userRoles.AsReadOnly();

        private User()
        {
        }

        public User(string username, string email, string passwordHash, string name, string? avatarUrl = null, Language preferredLanguage = Language.PtBr)
        {
            SetIdentity(username, email, name);
            PasswordHash = passwordHash.Trim();
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
            PreferredLanguage = preferredLanguage;
        }

        public void RegisterLogin()
        {
            LastLoginAt = DateTimeOffset.UtcNow;
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }

        /// <summary>
        /// Bloqueio temporario apos tentativas seguidas de senha errada. O custo do BCrypt sozinho
        /// nao e politica de bloqueio: com paralelismo o ataque continua viavel, e ainda vira DoS de
        /// CPU contra o provedor de identidade inteiro.
        /// </summary>
        public void RegisterFailedLogin(int maxAttempts, TimeSpan lockDuration)
        {
            FailedLoginAttempts += 1;

            if (FailedLoginAttempts >= maxAttempts)
            {
                LockedUntil = DateTimeOffset.UtcNow.Add(lockDuration);
            }
        }

        public bool IsLockedOut(DateTimeOffset now)
        {
            return LockedUntil.HasValue && now < LockedUntil.Value;
        }

        /// <summary>
        /// Senha trocada zera o bloqueio: quem provou posse da conta pelo fluxo de recuperacao nao
        /// deve ficar preso ao contador de quem estava tentando adivinhar.
        /// </summary>
        public void ClearLockout()
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }

        public void Update(string username, string email, string name, bool isActive, string? avatarUrl = null)
        {
            SetIdentity(username, email, name);
            IsActive = isActive;
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        }

        public void ChangePassword(string passwordHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
            PasswordHash = passwordHash.Trim();
            ClearLockout();
        }

        public void UpdateAvatar(string? avatarUrl)
        {
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        private void SetIdentity(string username, string email, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Username = username.Trim();
            Email = email.Trim();
            Name = name.Trim();
        }
    }
}
