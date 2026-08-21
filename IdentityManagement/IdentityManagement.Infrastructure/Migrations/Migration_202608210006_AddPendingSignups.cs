using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    // Cadastro publico em duas etapas: a linha nasce aqui e o provisionamento (empresa, contratos e
    // dois bancos) so roda quando o link do e-mail e clicado. Ver PendingSignup para o porque.
    [Migration(202608210006)]
    public sealed class Migration_202608210006_AddPendingSignups : Migration
    {
        public override void Up()
        {
            if (Schema.Table("pendingsignups").Exists())
            {
                return;
            }

            Create.Table("pendingsignups")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("legalname").AsString(200).NotNullable()
                .WithColumn("tradename").AsString(200).NotNullable()
                .WithColumn("document").AsString(14).NotNullable()
                .WithColumn("email").AsString(200).NotNullable()
                .WithColumn("phonenumber").AsString(20).Nullable()
                .WithColumn("annual").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("token").AsString(64).NotNullable()
                .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                .WithColumn("consumedat").AsDateTimeOffset().Nullable()
                .WithColumn("companyid").AsInt64().Nullable()
                .WithColumn("sourceip").AsString(64).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ux_pendingsignups_token")
                .OnTable("pendingsignups")
                .OnColumn("token").Ascending()
                .WithOptions().Unique();

            // A poda periodica varre por expiracao.
            Create.Index("ix_pendingsignups_expiresat")
                .OnTable("pendingsignups")
                .OnColumn("expiresat").Ascending();
        }

        public override void Down()
        {
            if (Schema.Table("pendingsignups").Exists())
            {
                Delete.Table("pendingsignups");
            }
        }
    }
}
