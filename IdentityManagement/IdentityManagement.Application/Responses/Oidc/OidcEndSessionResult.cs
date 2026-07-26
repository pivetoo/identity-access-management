namespace IdentityManagement.Application.Responses.Oidc
{
    public sealed class OidcEndSessionResult
    {
        public string RedirectUrl { get; set; } = string.Empty;

        /// <summary>
        /// Falso quando nao foi possivel identificar a sessao (sem <c>id_token_hint</c> valido, ou
        /// cliente que nao resolve). O redirect ainda acontece — e o previsto para RP-initiated
        /// logout —, mas o chamador precisa saber que nada foi encerrado do lado do servidor.
        /// </summary>
        public bool SessionRevoked { get; set; }
    }
}
