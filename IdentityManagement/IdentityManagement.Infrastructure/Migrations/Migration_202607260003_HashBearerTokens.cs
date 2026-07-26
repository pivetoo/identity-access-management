using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    /// <summary>
    /// A partir daqui as credenciais de portador sao guardadas como hash. As linhas gravadas em claro
    /// antes desta migration nunca mais vao casar com o valor apresentado, entao ficariam como lixo
    /// que aparenta ser credencial valida. Sao encerradas aqui, de proposito:
    ///
    /// - refresh tokens ativos sao revogados (o usuario autentica de novo);
    /// - codigos de autorizacao e sessoes pendentes sao descartados (vivem minutos);
    /// - links de reset de senha e convites de admin em aberto param de funcionar e precisam ser reenviados.
    /// </summary>
    [Migration(202607260003)]
    public sealed class Migration_202607260003_HashBearerTokens : Migration
    {
        public override void Up()
        {
            if (Schema.Table("refreshtokens").Exists())
            {
                Execute.Sql("UPDATE refreshtokens SET isrevoked = true, revokedat = now() WHERE isrevoked = false;");
            }

            if (Schema.Table("authorizationcodes").Exists())
            {
                Execute.Sql("DELETE FROM authorizationcodes WHERE isused = false AND isrevoked = false;");
            }

            if (Schema.Table("pendingauthorizationsessions").Exists())
            {
                Execute.Sql("DELETE FROM pendingauthorizationsessions WHERE isused = false AND isrevoked = false;");
            }

            if (Schema.Table("passwordresettokens").Exists())
            {
                Execute.Sql("DELETE FROM passwordresettokens WHERE usedat IS NULL;");
            }

            if (Schema.Table("contractadmininvitations").Exists())
            {
                Execute.Sql("UPDATE contractadmininvitations SET revokedat = now() WHERE usedat IS NULL AND revokedat IS NULL;");
            }
        }

        public override void Down()
        {
            // Sem volta: o valor em claro das credenciais encerradas aqui nao existe mais em lugar nenhum.
        }
    }
}
