using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040002)]
    public sealed class Migration_202604040002_LinkAccessResourcesToSystemApplication : Migration
    {
        public override void Up()
        {
            Alter.Table("accessresources")
                .AddColumn("systemapplicationid").AsInt64().Nullable();

            Execute.Sql("""
                UPDATE accessresources
                SET systemapplicationid = (
                    SELECT id
                    FROM systemapplications
                    ORDER BY id
                    LIMIT 1
                )
                WHERE systemapplicationid IS NULL;
                """);

            Alter.Column("systemapplicationid")
                .OnTable("accessresources")
                .AsInt64()
                .NotNullable();

            Create.ForeignKey("fk_accessresources_systemapplications_systemapplicationid")
                .FromTable("accessresources").ForeignColumn("systemapplicationid")
                .ToTable("systemapplications").PrimaryColumn("id");

            Delete.Index("ix_accessresources_name")
                .OnTable("accessresources");

            Create.Index("ix_accessresources_systemapplicationid_name")
                .OnTable("accessresources")
                .OnColumn("systemapplicationid").Ascending()
                .OnColumn("name").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            Delete.Index("ix_accessresources_systemapplicationid_name")
                .OnTable("accessresources");

            Create.Index("ix_accessresources_name")
                .OnTable("accessresources")
                .OnColumn("name").Ascending()
                .WithOptions().Unique();

            Delete.ForeignKey("fk_accessresources_systemapplications_systemapplicationid")
                .OnTable("accessresources");

            Delete.Column("systemapplicationid")
                .FromTable("accessresources");
        }
    }
}
