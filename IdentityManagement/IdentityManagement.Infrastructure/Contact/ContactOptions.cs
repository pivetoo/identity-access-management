namespace IdentityManagement.Infrastructure.Contact
{
    public sealed class ContactOptions
    {
        public const string SectionName = "Contact";

        /// <summary>
        /// Caixa que recebe os contatos do site. Vem de configuracao e NUNCA do formulario: enviar
        /// para endereco informado pelo visitante transformaria o endpoint em relay de spam.
        /// </summary>
        public string NotifyEmail { get; set; } = string.Empty;
    }
}
