using IdentityManagement.Application.Requests.Contact;

namespace IdentityManagement.Application.Services
{
    /// <summary>
    /// Contato do site publico. Registra o lead e notifica o time.
    /// </summary>
    public interface IContactRequestService
    {
        Task SubmitAsync(ContactRequestInput request, string? sourceIp, CancellationToken cancellationToken = default);
    }
}
