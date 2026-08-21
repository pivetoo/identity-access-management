namespace IdentityManagement.Domain.ValueObjects
{
    /// <summary>
    /// Telefone brasileiro do cadastro publico. Aceita com ou sem mascara; a normalizacao devolve
    /// somente digitos.
    ///
    /// Valida 10 digitos (fixo) ou 11 (celular), sempre com DDD. Nao e preciosismo: o telefone e
    /// obrigatorio no checkout do provedor de cobranca — a primeira tentativa de cadastrar cartao
    /// falhou com 500 exatamente porque a empresa tinha telefone vazio, e o erro so apareceu
    /// semanas depois do cadastro, longe da causa.
    /// </summary>
    public static class Phone
    {
        public static string Normalize(string? value)
        {
            return new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        }

        public static bool IsValid(string? value)
        {
            string digits = Normalize(value);

            if (digits.Length is not (10 or 11))
            {
                return false;
            }

            // DDD valido comeca em 11; nenhum estado usa 0x ou 10.
            int areaCode = int.Parse(digits[..2]);
            if (areaCode < 11)
            {
                return false;
            }

            // Celular (11 digitos) sempre comeca com 9 depois do DDD; fixo nunca comeca com 0 ou 1.
            if (digits.Length == 11 && digits[2] != '9')
            {
                return false;
            }

            if (digits.Length == 10 && (digits[2] == '0' || digits[2] == '1'))
            {
                return false;
            }

            return !digits[2..].All(character => character == digits[2]);
        }
    }
}
