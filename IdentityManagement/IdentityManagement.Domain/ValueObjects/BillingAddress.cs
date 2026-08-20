namespace IdentityManagement.Domain.ValueObjects
{
    /// <summary>
    /// Endereco de cobranca da empresa.
    ///
    /// Existe porque o checkout recorrente do Asaas EXIGE endereco do pagador: sem CEP, logradouro,
    /// numero, bairro e cidade a criacao do checkout e recusada. Nao e enfeite de cadastro.
    /// </summary>
    public sealed record BillingAddress(
        string PostalCode,
        string Street,
        string Number,
        string? Complement,
        string District,
        string City,
        string State)
    {
        public static string NormalizePostalCode(string? value) =>
            new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        public bool IsComplete()
        {
            return NormalizePostalCode(PostalCode).Length == 8
                && !string.IsNullOrWhiteSpace(Street)
                && !string.IsNullOrWhiteSpace(Number)
                && !string.IsNullOrWhiteSpace(District)
                && !string.IsNullOrWhiteSpace(City)
                && !string.IsNullOrWhiteSpace(State);
        }
    }
}
