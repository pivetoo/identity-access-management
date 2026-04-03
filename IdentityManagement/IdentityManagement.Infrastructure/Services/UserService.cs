using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Users;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class UserService : CrudService<User>, IUserService
    {
        public UserService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<UserResponse> CreateUser(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            User user = await Register(request.Username, request.Email, request.Password, request.Name, cancellationToken);
            return ToResponse(user);
        }

        public async Task<UserResponse> UpdateUser(long id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            User? user = await (
                from item in DbContext.Set<User>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            await EnsureUniqueUser(request.Username, request.Email, id, cancellationToken);

            user.Update(request.Username, request.Email, request.Name, request.IsActive, user.AvatarUrl);

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.ChangePassword(HashPassword(request.Password));
            }

            User? result = await Update(user, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result);
        }

        public async Task<string?> UpdateAvatar(long id, string? avatarUrl, CancellationToken cancellationToken = default)
        {
            User? user = await (
                from item in DbContext.Set<User>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            string? previousAvatarUrl = user.AvatarUrl;
            user.UpdateAvatar(avatarUrl);

            User? result = await Update(user, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return previousAvatarUrl;
        }

        public Task<string?> DeleteAvatar(long id, CancellationToken cancellationToken = default)
        {
            return UpdateAvatar(id, null, cancellationToken);
        }

        public Task<User?> GetById(long id, CancellationToken cancellationToken = default)
        {
            return (
                from user in DbContext.Set<User>().AsTracking()
                where user.Id == id
                select user)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> Authenticate(string username, string password, CancellationToken cancellationToken = default)
        {
            User? user = await GetByUsernameOrEmail(username, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            user.RegisterLogin();
            await Update(user, cancellationToken);

            return user;
        }

        public async Task<User> Register(string username, string email, string password, string? name = null, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueUser(username, email, null, cancellationToken);

            User user = new User(username, email, HashPassword(password), string.IsNullOrWhiteSpace(name) ? username : name);
            bool success = await Insert(cancellationToken, user);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return user;
        }

        public async Task<bool> ChangePassword(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            User? user = await (
                from item in DbContext.Set<User>().AsTracking()
                where item.Id == userId
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null || !VerifyPassword(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.ChangePassword(HashPassword(newPassword));
            return await Update(user, cancellationToken) is not null;
        }

        public Task<User?> GetByUsernameOrEmail(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            return (
                from user in DbContext.Set<User>().AsTracking()
                where user.Username == usernameOrEmail || user.Email == usernameOrEmail
                select user)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default)
        {
            return (
                from user in DbContext.Set<User>().AsTracking()
                where user.Username == username
                select user)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<UserResponse>> GetActiveUsers(CancellationToken cancellationToken = default)
        {
            List<UserResponse> users = await (
                from user in DbContext.Set<User>().AsNoTracking()
                where user.IsActive
                orderby user.Name
                select new UserResponse
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
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private async Task EnsureUniqueUser(string username, string email, long? currentUserId, CancellationToken cancellationToken)
        {
            bool usernameExists = await (
                from user in DbContext.Set<User>().AsNoTracking()
                where user.Username == username && (!currentUserId.HasValue || user.Id != currentUserId.Value)
                select user.Id)
                .AnyAsync(cancellationToken);

            if (usernameExists)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            bool emailExists = await (
                from user in DbContext.Set<User>().AsNoTracking()
                where user.Email == email && (!currentUserId.HasValue || user.Id != currentUserId.Value)
                select user.Id)
                .AnyAsync(cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException("Email already exists.");
            }
        }

        private static UserResponse ToResponse(User user)
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
    }
}
