using IdentityManagement.Application.Requests.Billing;

namespace IdentityManagement.Application.Services
{
    public interface IBillingWebhookService
    {
        // Retorna true quando uma transicao de assinatura foi aplicada; false quando o evento foi
        // ignorado, duplicado ou nao mapeou para nenhuma assinatura conhecida. O pagamento e
        // sempre persistido e o evento sempre registrado (com o corpo bruto) para auditoria.
        Task<bool> ProcessAsaasEventAsync(AsaasWebhookPayload payload, string rawPayload = "", CancellationToken ct = default);
    }
}
