using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Tira mascara dos documentos ja gravados.
    ///
    /// O cadastro publico sempre normalizou antes de comparar, mas o console de administracao
    /// gravava o texto digitado. Com "64.224.591/0001-63" no banco, a checagem de CNPJ duplicado do
    /// signup nao encontra a empresa e o indice unico tambem nao barra — daria para criar uma
    /// segunda empresa com o mesmo CNPJ so mudando a formatacao.
    ///
    /// A entidade passou a normalizar na escrita; esta migration corrige o que ja existe.
    /// </summary>
    [Migration(202608210003)]
    public sealed class Migration_202608210003_NormalizeCompanyDocuments : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("companies").Exists())
            {
                return;
            }

            Execute.Sql("UPDATE companies SET document = regexp_replace(document, '[^0-9]', '', 'g') WHERE document ~ '[^0-9]';");
        }

        public override void Down()
        {
            // Sem volta: a formatacao original nao e recuperavel, e nem deveria voltar.
        }
    }
}
