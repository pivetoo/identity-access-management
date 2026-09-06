namespace IdentityManagement.Domain.ValueObjects
{
    /// <summary>
    /// Origem de um cadastro publico: parametros UTM, identificadores de clique dos anunciantes
    /// (gclid do Google, fbclid do Meta), pagina de entrada e referrer da primeira visita.
    ///
    /// Tudo opcional: cadastro direto chega vazio. Fica no cadastro pendente e e copiado para a
    /// empresa na confirmacao, para responder "qual anuncio trouxe esta agencia" e permitir a
    /// importacao de conversoes offline nas plataformas de anuncio, sem pixel no site.
    /// </summary>
    public sealed record SignupAttribution(
        string? Source,
        string? Medium,
        string? Campaign,
        string? Content,
        string? Term,
        string? Gclid,
        string? Fbclid,
        string? LandingPage,
        string? Referrer)
    {
        public const int ShortMaxLength = 200;

        public const int LongMaxLength = 500;

        public bool IsEmpty =>
            Source is null && Medium is null && Campaign is null && Content is null && Term is null
            && Gclid is null && Fbclid is null && LandingPage is null && Referrer is null;

        /// <summary>
        /// Apara espacos, transforma vazio em nulo e corta no tamanho da coluna. O valor vem da URL
        /// montada por quem anuncia, entao nao ha formato a validar, so a garantir que cabe.
        /// </summary>
        public static SignupAttribution Normalize(
            string? source,
            string? medium,
            string? campaign,
            string? content,
            string? term,
            string? gclid,
            string? fbclid,
            string? landingPage,
            string? referrer)
        {
            return new SignupAttribution(
                Clean(source, ShortMaxLength),
                Clean(medium, ShortMaxLength),
                Clean(campaign, ShortMaxLength),
                Clean(content, ShortMaxLength),
                Clean(term, ShortMaxLength),
                Clean(gclid, ShortMaxLength),
                Clean(fbclid, ShortMaxLength),
                Clean(landingPage, LongMaxLength),
                Clean(referrer, LongMaxLength));
        }

        private static string? Clean(string? value, int maxLength)
        {
            string? trimmed = value?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }
    }
}
