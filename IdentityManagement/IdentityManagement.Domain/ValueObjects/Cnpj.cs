namespace IdentityManagement.Domain.ValueObjects
{
    /// <summary>
    /// Validacao de CNPJ para cadastro publico. Aceita com ou sem mascara; a normalizacao
    /// devolve somente digitos, que e como o documento e persistido e enviado ao gateway.
    /// </summary>
    public static class Cnpj
    {
        public static string Normalize(string? value)
        {
            return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        public static bool IsValid(string? value)
        {
            string digits = Normalize(value);

            if (digits.Length != 14)
            {
                return false;
            }

            if (digits.Distinct().Count() == 1)
            {
                return false;
            }

            int[] firstWeights = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] secondWeights = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int firstCheck = ComputeCheckDigit(digits, firstWeights);
            if (firstCheck != digits[12] - '0')
            {
                return false;
            }

            int secondCheck = ComputeCheckDigit(digits, secondWeights);
            return secondCheck == digits[13] - '0';
        }

        private static int ComputeCheckDigit(string digits, int[] weights)
        {
            int sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += (digits[i] - '0') * weights[i];
            }

            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
    }
}
