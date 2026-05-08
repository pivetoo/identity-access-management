using Archon.Infrastructure.Services;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.Users;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class UserService : CrudService<User>, IUserService
    {
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public UserService(DbContext dbContext, IStringLocalizer<IdentityManagementResource> Localizer) : base(dbContext)
        {
            this.Localizer = Localizer;
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
                throw new InvalidOperationException(Localizer["request.route.idMismatch"]);
            }

            User? user = await (
                from item in DbContext.Set<User>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException(Localizer["user.notFound"]);
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
                throw new InvalidOperationException(Localizer["user.notFound"]);
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

        public async Task<ContractUserResponse> CreateUserInContract(CreateUserInContractRequest request, long contractId, CancellationToken cancellationToken = default)
        {
            Role? role = await DbContext.Set<Role>()
                .AsNoTracking()
                .Where(item => item.Id == request.RoleId && item.ContractId == contractId)
                .FirstOrDefaultAsync(cancellationToken);

            if (role is null)
            {
                throw new InvalidOperationException(Localizer["role.notFoundInContract"]);
            }

            await EnsureUniqueUser(request.Username, request.Email, null, cancellationToken);

            IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                User user = new User(request.Username, request.Email, HashPassword(request.Password), request.Name);
                bool userInserted = await Insert(cancellationToken, user);
                if (!userInserted)
                {
                    throw new InvalidOperationException(GetErrorMessages());
                }

                UserRole userRole = new UserRole(user.Id, role.Id);
                DbContext.Set<UserRole>().Add(userRole);
                await DbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return BuildContractUserResponse(user, role, userRole);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<ContractUserResponse>> GetUsersByContract(long contractId, CancellationToken cancellationToken = default)
        {
            List<ContractUserResponse> users = await (
                from userRole in DbContext.Set<UserRole>().AsNoTracking()
                join user in DbContext.Set<User>().AsNoTracking() on userRole.UserId equals user.Id
                join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                where role.ContractId == contractId &&
                      userRole.IsActive &&
                      !userRole.RevokedAt.HasValue
                orderby user.Name
                select new ContractUserResponse
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Name = user.Name,
                    AvatarUrl = user.AvatarUrl,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt,
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsRoot = role.IsRoot,
                    AssignedAt = userRole.AssignedAt
                })
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<ContractUserResponse> UpdateUserRoleInContract(long userId, long contractId, long newRoleId, CancellationToken cancellationToken = default)
        {
            Role? newRole = await DbContext.Set<Role>()
                .AsNoTracking()
                .Where(item => item.Id == newRoleId && item.ContractId == contractId)
                .FirstOrDefaultAsync(cancellationToken);

            if (newRole is null)
            {
                throw new InvalidOperationException(Localizer["role.notFoundInContract"]);
            }

            User? user = await DbContext.Set<User>()
                .AsNoTracking()
                .Where(item => item.Id == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException(Localizer["user.notFound"]);
            }

            IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                List<UserRole> currentAssignments = await (
                    from userRole in DbContext.Set<UserRole>().AsTracking()
                    join role in DbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                    where userRole.UserId == userId &&
                          role.ContractId == contractId &&
                          userRole.IsActive &&
                          !userRole.RevokedAt.HasValue
                    select userRole)
                    .ToListAsync(cancellationToken);

                UserRole? existing = currentAssignments.FirstOrDefault(item => item.RoleId == newRoleId);
                foreach (UserRole assignment in currentAssignments.Where(item => item.RoleId != newRoleId))
                {
                    assignment.Revoke();
                }

                UserRole effectiveAssignment;
                if (existing is null)
                {
                    effectiveAssignment = new UserRole(userId, newRoleId);
                    DbContext.Set<UserRole>().Add(effectiveAssignment);
                }
                else
                {
                    effectiveAssignment = existing;
                }

                await DbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return BuildContractUserResponse(user, newRole, effectiveAssignment);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<UserResponse> SetActive(long userId, bool isActive, CancellationToken cancellationToken = default)
        {
            User? user = await (
                from item in DbContext.Set<User>().AsTracking()
                where item.Id == userId
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException(Localizer["user.notFound"]);
            }

            if (isActive)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }

            User? result = await Update(user, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result);
        }

        private static ContractUserResponse BuildContractUserResponse(User user, Role role, UserRole userRole)
        {
            return new ContractUserResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                RoleId = role.Id,
                RoleName = role.Name,
                IsRoot = role.IsRoot,
                AssignedAt = userRole.AssignedAt
            };
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
                throw new InvalidOperationException(Localizer["user.username.alreadyExists"]);
            }

            bool emailExists = await (
                from user in DbContext.Set<User>().AsNoTracking()
                where user.Email == email && (!currentUserId.HasValue || user.Id != currentUserId.Value)
                select user.Id)
                .AnyAsync(cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException(Localizer["email.alreadyExists"]);
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
