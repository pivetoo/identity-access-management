using IdentityManagement.Application.Requests.Billing;

namespace IdentityManagement.Application.Services
{
    public interface IBillingWebhookService
    {
        // Retorna true quando uma transicao foi aplicada; false quando o evento foi
        // ignorado, duplicado ou nao mapeou para nenhuma assinatura conhecida.
        Task<bool> ProcessAsaasEventAsync(AsaasWebhookPayload payload, CancellationToken ct = default);
    }
}
