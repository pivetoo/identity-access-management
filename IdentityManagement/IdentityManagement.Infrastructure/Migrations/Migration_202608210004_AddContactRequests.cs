using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Contatos vindos do site publico. Registrados antes do envio do e-mail: notificacao que falha
    // nao pode significar lead perdido.
    [Migration(202608210004)]
    public sealed class Migration_202608210004_AddContactRequests : Migration
    {
        public override void Up()
        {
            if (Schema.Table("contactrequests").Exists())
            {
                return;
            }

            Create.Table("contactrequests")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("email").AsString(255).NotNullable()
                .WithColumn("phonenumber").AsString(30).Nullable()
                .WithColumn("companyname").AsString(150).Nullable()
                .WithColumn("message").AsString(4000).NotNullable()
                .WithColumn("sourceip").AsString(64).Nullable()
                .WithColumn("notificationsent").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("notificationsentat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ix_contactrequests_createdat")
                .OnTable("contactrequests")
                .OnColumn("createdat").Descending();
        }

        public override void Down()
        {
            if (Schema.Table("contactrequests").Exists())
            {
                Delete.Table("contactrequests");
            }
        }
    }
}
