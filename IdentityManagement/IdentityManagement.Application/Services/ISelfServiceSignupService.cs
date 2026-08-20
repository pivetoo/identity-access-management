using IdentityManagement.Application.Requests.Signup;
using IdentityManagement.Application.Responses.Signup;

namespace IdentityManagement.Application.Services
{
    public interface ISelfServiceSignupService
    {
        Task<SignupResponse> SignupAsync(SignupRequest request, string setupBaseUrl, CancellationToken cancellationToken = default);
    }
}
