using IdentityManagement.Application.Requests.Auth;
using IdentityManagement.Application.Responses.Auth;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Responses.Users;

namespace IdentityManagement.Application.Services
{
    public interface IAuthService
    {
        Task<ContractSelectionResponse> IdentifyUser(IdentifyUserRequest request, CancellationToken cancellationToken = default);

        Task<bool> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default);

        Task<UserResponse?> GetUserByUsername(string username, CancellationToken cancellationToken = default);

        Task ForgotPasswordAsync(ForgotPasswordRequest request, string resetBaseUrl, CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
