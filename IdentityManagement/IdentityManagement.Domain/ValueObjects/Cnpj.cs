using System.Text;

namespace IdentityManagement.Domain.ValueObjects
{
    /// <summary>
    /// Validacao de CNPJ para cadastro publico. Aceita com ou sem mascara; a normalizacao devolve
    /// somente [A-Z0-9] em maiusculo, que e como o documento e persistido e enviado ao gateway.
    ///
    /// Suporta o formato ALFANUMERICO da Receita Federal: as posicoes 1-12 podem ter letras e os
    /// digitos verificadores (13-14) continuam numericos. No modulo 11, o valor de cada caractere e
    /// (ASCII - 48) — que e exatamente o que <c>(c - '0')</c> produz, entao 'A' vale 17. CNPJ
    /// numerico atual valida identico por esta mesma conta, sem caminho separado.
    ///
    /// Mesma regra ja implementada no AgencyCampaign (DocumentValidation); aqui e porte, nao
    /// invencao — se uma das duas mudar, a outra precisa mudar junto.
    /// </summary>
    public static class Cnpj
    {
        public static string Normalize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length);

            foreach (char character in value)
            {
                if (char.IsAsciiLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString();
        }

        public static bool IsValid(string? value)
        {
            string document = Normalize(value);

            if (document.Length != 14 || AllSame(document))
            {
                return false;
            }

            // Os verificadores nunca sao letra, mesmo no formato alfanumerico.
            if (!char.IsAsciiDigit(document[12]) || !char.IsAsciiDigit(document[13]))
            {
                return false;
            }

            int[] firstWeights = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] secondWeights = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            return ComputeCheckDigit(document, firstWeights) == document[12] - '0'
                && ComputeCheckDigit(document, secondWeights) == document[13] - '0';
        }

        private static int ComputeCheckDigit(string document, int[] weights)
        {
            int sum = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                sum += (document[i] - '0') * weights[i];
            }

            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        private static bool AllSame(string value)
        {
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i] != value[0])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
