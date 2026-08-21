using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // As travas de envio contam linhas por destinatario (24h) e no total (1h) a cada cadastro. Sao
    // consultas no caminho quente, e justamente sob ataque a tabela e que cresce — que e quando a
    // varredura sequencial doeria.
    [Migration(202608210007)]
    public sealed class Migration_202608210007_AddPendingSignupLookupIndexes : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("pendingsignups").Index("ix_pendingsignups_email_createdat").Exists())
            {
                Create.Index("ix_pendingsignups_email_createdat")
                    .OnTable("pendingsignups")
                    .OnColumn("email").Ascending()
                    .OnColumn("createdat").Descending();
            }

            if (!Schema.Table("pendingsignups").Index("ix_pendingsignups_createdat").Exists())
            {
                Create.Index("ix_pendingsignups_createdat")
                    .OnTable("pendingsignups")
                    .OnColumn("createdat").Descending();
            }
        }

        public override void Down()
        {
            if (Schema.Table("pendingsignups").Index("ix_pendingsignups_email_createdat").Exists())
            {
                Delete.Index("ix_pendingsignups_email_createdat").OnTable("pendingsignups");
            }

            if (Schema.Table("pendingsignups").Index("ix_pendingsignups_createdat").Exists())
            {
                Delete.Index("ix_pendingsignups_createdat").OnTable("pendingsignups");
            }
        }
    }
}
