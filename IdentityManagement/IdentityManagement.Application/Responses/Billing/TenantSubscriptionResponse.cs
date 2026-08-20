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
