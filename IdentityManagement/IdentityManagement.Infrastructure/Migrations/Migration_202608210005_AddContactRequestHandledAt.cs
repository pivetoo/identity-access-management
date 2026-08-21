using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Estado de tratamento do contato. Migration separada porque a 202608210004 ja rodou em
    // producao — editar migration aplicada nao altera banco nenhum, so o codigo.
    [Migration(202608210005)]
    public sealed class Migration_202608210005_AddContactRequestHandledAt : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("contactrequests").Exists() || Schema.Table("contactrequests").Column("handledat").Exists())
            {
                return;
            }

            Alter.Table("contactrequests")
                .AddColumn("handledat").AsDateTimeOffset().Nullable();

            // A tela abre filtrando por pendente; sem indice isso vira varredura conforme cresce.
            Create.Index("ix_contactrequests_handledat")
                .OnTable("contactrequests")
                .OnColumn("handledat").Ascending();
        }

        public override void Down()
        {
            if (Schema.Table("contactrequests").Column("handledat").Exists())
            {
                Delete.Index("ix_contactrequests_handledat").OnTable("contactrequests");
                Delete.Column("handledat").FromTable("contactrequests");
            }
        }
    }
}
