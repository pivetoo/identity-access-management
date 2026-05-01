using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040002)]
    public sealed class Migration_202604040002_LinkAccessResourcesToSystemApplication : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("accessresources").Column("systemapplicationid").Exists())
            {
                Alter.Table("accessresources")
                    .AddColumn("systemapplicationid").AsInt64().Nullable();
            }

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

            if (!Schema.Table("accessresources").Constraint("fk_accessresources_systemapplications_systemapplicationid").Exists())
            {
                Create.ForeignKey("fk_accessresources_systemapplications_systemapplicationid")
                    .FromTable("accessresources").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");
            }

            if (Schema.Table("accessresources").Index("ix_accessresources_name").Exists())
            {
                Delete.Index("ix_accessresources_name")
                    .OnTable("accessresources");
            }

            if (!Schema.Table("accessresources").Index("ix_accessresources_systemapplicationid_name").Exists())
            {
                Create.Index("ix_accessresources_systemapplicationid_name")
                    .OnTable("accessresources")
                    .OnColumn("systemapplicationid").Ascending()
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (!Schema.Table("accessresources").Exists())
            {
                return;
            }

            if (Schema.Table("accessresources").Index("ix_accessresources_systemapplicationid_name").Exists())
            {
                Delete.Index("ix_accessresources_systemapplicationid_name")
                    .OnTable("accessresources");
            }

            if (!Schema.Table("accessresources").Index("ix_accessresources_name").Exists())
            {
                Create.Index("ix_accessresources_name")
                    .OnTable("accessresources")
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }

            if (Schema.Table("accessresources").Constraint("fk_accessresources_systemapplications_systemapplicationid").Exists())
            {
                Delete.ForeignKey("fk_accessresources_systemapplications_systemapplicationid")
                    .OnTable("accessresources");
            }

            if (Schema.Table("accessresources").Column("systemapplicationid").Exists())
            {
                Delete.Column("systemapplicationid")
                    .FromTable("accessresources");
            }
        }
    }
}
