namespace IdentityManagement.Application.Responses.Billing
{
    /// <summary>
    /// Visao da assinatura para o proprio cliente. Sem id interno, sem id do provedor e sem nada
    /// que so interesse ao suporte — quem consome e a tela do tenant, nao o console de administracao.
    /// </summary>
    public sealed class TenantSubscriptionResponse
    {
        public string PlanName { get; set; } = string.Empty;

        public decimal PriceAmount { get; set; }

        public string Currency { get; set; } = "BRL";

        /// <summary>Monthly ou Yearly.</summary>
        public string BillingPeriod { get; set; } = string.Empty;

        /// <summary>Trialing, Active, PastDue, Suspended ou Canceled.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>PIX, CREDIT_CARD ou vazio quando ainda nao ha cobranca no provedor.</summary>
        public string? PaymentMethod { get; set; }

        public DateTimeOffset? TrialEndsAt { get; set; }

        public DateTimeOffset CurrentPeriodEnd { get; set; }

        public bool IsBlocked { get; set; }

        /// <summary>Endereco de cobranca ja preenchido? O checkout de cartao depende dele.</summary>
        public bool HasBillingAddress { get; set; }

        public TenantBillingAddressResponse? BillingAddress { get; set; }

        /// <summary>Cobranca em aberto, quando existe. E por onde a agencia paga.</summary>
        public TenantPendingChargeResponse? PendingCharge { get; set; }
    }

    public sealed class TenantPendingChargeResponse
    {
        public decimal Value { get; set; }

        public DateTimeOffset? DueDate { get; set; }

        /// <summary>PIX, CREDIT_CARD, BOLETO...</summary>
        public string BillingType { get; set; } = string.Empty;

        /// <summary>PENDING ou OVERDUE.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Fatura hospedada pelo provedor, com todos os meios de pagamento.</summary>
        public string? InvoiceUrl { get; set; }

        /// <summary>Copia e cola do PIX.</summary>
        public string? PixPayload { get; set; }

        /// <summary>QR code do PIX em PNG base64 (sem o prefixo data:).</summary>
        public string? PixQrCodeBase64 { get; set; }
    }

    public sealed class TenantBillingAddressResponse
    {
        public string PostalCode { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string Number { get; set; } = string.Empty;

        public string? Complement { get; set; }

        public string District { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;
    }
}
