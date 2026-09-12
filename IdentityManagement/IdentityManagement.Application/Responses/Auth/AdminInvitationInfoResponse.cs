namespace IdentityManagement.Application.Responses.Auth
{
    public sealed class AdminInvitationInfoResponse
    {
        public string CompanyName { get; set; } = string.Empty;
        public string SystemApplicationName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string[] SystemApplicationNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Ja existe conta com o e-mail do convite. A tela usa isto para abrir direto no modo
        /// "ja tenho conta" — sem a dica, quem administra a segunda agencia tentaria criar um
        /// usuario novo com o mesmo e-mail e receberia um conflito sem saida visivel.
        ///
        /// So chega a quem apresenta um token de convite valido, entao nao serve de oraculo para
        /// descobrir quais enderecos tem cadastro.
        /// </summary>
        public bool UserExists { get; set; }
    }
}
