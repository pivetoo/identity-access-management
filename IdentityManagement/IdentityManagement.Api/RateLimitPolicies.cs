namespace IdentityManagement.Api
{
    /// <summary>
    /// Nomes das politicas de rate limiting. Aplicar <c>Auth</c> em todo endpoint anonimo que
    /// recebe credencial: login, troca de token e recuperacao de senha.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string Auth = "auth";

        /// <summary>
        /// Cadastro publico. Bem mais estreita que <c>Auth</c> de proposito: cada chamada bem
        /// sucedida cria empresa, contratos e um BANCO de tenant no Postgres. Com o limite de
        /// credencial (20/min) um unico IP conseguiria provisionar milhares de bancos por hora.
        /// </summary>
        public const string Signup = "signup";

        /// <summary>
        /// Contato do site. Endpoint anonimo que dispara e-mail e alvo obvio de spam; o limite e
        /// por IP e estreito, porque contato legitimo e raro por origem.
        /// </summary>
        public const string Contact = "contact";
    }
}
