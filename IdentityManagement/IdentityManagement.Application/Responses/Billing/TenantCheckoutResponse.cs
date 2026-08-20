namespace IdentityManagement.Application.Responses.Billing
{
    public sealed class TenantCheckoutResponse
    {
        /// <summary>URL hospedada pelo provedor. O dado de cartao nunca passa por nos.</summary>
        public string CheckoutUrl { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }
    }
}
