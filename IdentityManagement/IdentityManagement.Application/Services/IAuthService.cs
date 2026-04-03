using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Responses.Users;

namespace IdentityManagement.Application.Services
{
    public interface IAuthService
    {
        Task<object> IdentifyUser(IdentifyUserRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);

        Task<LoginResponse> LoginWithContract(LoginWithContractRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);

        Task<bool> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse?> GetUserByUsername(string username, CancellationToken cancellationToken = default);
    }
}
