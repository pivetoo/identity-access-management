using IdentityManagement.Application.Requests.Contact;
using IdentityManagement.Application.Responses.Contact;

namespace IdentityManagement.Application.Services
{
    /// <summary>
    /// Contato do site publico. Registra o lead e notifica o time.
    /// </summary>
    public interface IContactRequestService
    {
        Task SubmitAsync(ContactRequestInput request, string? sourceIp, CancellationToken cancellationToken = default);

        /// <summary>Lista para o console. <paramref name="pending"/> nulo traz tudo.</summary>
        Task<IReadOnlyList<ContactRequestResponse>> ListAsync(bool? pending, int? take, CancellationToken cancellationToken = default);

        Task<ContactRequestResponse> SetHandledAsync(long id, bool handled, CancellationToken cancellationToken = default);
    }
}
