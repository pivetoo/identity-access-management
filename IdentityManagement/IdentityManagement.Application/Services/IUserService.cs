using Archon.Application.Services;
using IdentityManagement.Application.Requests.Users;
using IdentityManagement.Application.Responses.Users;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IUserService : ICrudService<User>
    {
        Task<User?> Authenticate(string username, string password, CancellationToken cancellationToken = default);

        Task<User> Register(string username, string email, string password, string? name = null, CancellationToken cancellationToken = default);

        Task<bool> ChangePassword(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        string HashPassword(string password);

        bool VerifyPassword(string password, string hashedPassword);

        Task<UserResponse> CreateUser(RegisterUserRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse> UpdateUser(long id, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<string?> UpdateAvatar(long id, string? avatarUrl, CancellationToken cancellationToken = default);

        Task<string?> DeleteAvatar(long id, CancellationToken cancellationToken = default);

        Task<User?> GetById(long id, CancellationToken cancellationToken = default);

        Task<User?> GetByUsernameOrEmail(string usernameOrEmail, CancellationToken cancellationToken = default);

        Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<UserResponse>> GetActiveUsers(CancellationToken cancellationToken = default);

        Task<bool> HasAnyUser(CancellationToken cancellationToken = default);

        Task<ContractUserResponse> CreateUserInContract(CreateUserInContractRequest request, long contractId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractUserResponse>> GetUsersByContract(long contractId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractUserResponse>> GetAdminsByApiKey(string apiKey, CancellationToken cancellationToken = default);

        Task<ContractUserResponse> UpdateUserRoleInContract(long userId, long contractId, long newRoleId, CancellationToken cancellationToken = default);

        Task<UserResponse> SetActive(long userId, bool isActive, CancellationToken cancellationToken = default);
    }
}
