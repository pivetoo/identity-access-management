using System.Security.Cryptography;
using System.Text;

namespace IdentityManagement.Domain.Security
{
    /// <summary>
    /// Credencial de portador (refresh token, codigo de autorizacao, token de reset, sessao pendente)
    /// nao fica legivel em repouso: o banco guarda o hash, e o valor em claro so existe no caminho de
    /// volta para o cliente. Um SELECT na tabela deixa de ser um conjunto de sessoes prontas para uso.
    ///
    /// SHA-256 puro basta, e nao KDF: sao valores aleatorios de 256 bits gerados por CSPRNG, nao
    /// senhas. Nao ha espaco de busca para forca bruta, e o custo por verificacao precisa ser baixo
    /// porque isso roda em todo refresh.
    /// </summary>
    public static class TokenHasher
    {
        public static string Hash(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return Convert.ToBase64String(hash);
        }
    }
}
