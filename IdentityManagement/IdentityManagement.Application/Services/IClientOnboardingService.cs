using IdentityManagement.Application.Requests.Clients;
using IdentityManagement.Application.Responses.Clients;

namespace IdentityManagement.Application.Services
{
    public interface IClientOnboardingService
    {
        Task<OnboardClientResponse> OnboardClient(OnboardClientRequest request, string setupBaseUrl, CancellationToken ct = default);
    }
}
