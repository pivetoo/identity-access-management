namespace IdentityManagement.Api
{
    /// <summary>
    /// Nomes das politicas de rate limiting. Aplicar <c>Auth</c> em todo endpoint anonimo que
    /// recebe credencial: login, troca de token e recuperacao de senha.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string Auth = "auth";
    }
}
