using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605190008)]
    public sealed class Migration_202605190008_AddSystemIntegrationBlueprint : Migration
    {
        public override void Up()
        {
            if (Schema.Table("systemapplications").Exists())
            {
                if (!Schema.Table("systemapplications").Column("baseurl").Exists())
                {
                    Alter.Table("systemapplications")
                        .AddColumn("baseurl").AsString(2000).Nullable();
                }
            }

            if (!Schema.Table("systemintegrations").Exists())
            {
                Create.Table("systemintegrations")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemapplicationid").AsInt64().NotNullable()
                    .WithColumn("name").AsString(200).NotNullable()
                    .WithColumn("baseurl").AsString(2000).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.ForeignKey("fk_systemintegrations_systemapplications")
                    .FromTable("systemintegrations").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");

                Create.Index("ix_systemintegrations_systemapplicationid")
                    .OnTable("systemintegrations")
                    .OnColumn("systemapplicationid").Ascending();
            }

            if (!Schema.Table("systemintegrationparameters").Exists())
            {
                Create.Table("systemintegrationparameters")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemintegrationid").AsInt64().NotNullable()
                    .WithColumn("key").AsString(200).NotNullable()
                    .WithColumn("value").AsCustom("text").Nullable()
                    .WithColumn("issecret").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("valuesource").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("sourceaudience").AsString(200).Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();

                Create.ForeignKey("fk_systemintegrationparameters_systemintegrations")
                    .FromTable("systemintegrationparameters").ForeignColumn("systemintegrationid")
                    .ToTable("systemintegrations").PrimaryColumn("id");

                Create.Index("ux_systemintegrationparameters_integration_key")
                    .OnTable("systemintegrationparameters")
                    .OnColumn("systemintegrationid").Ascending()
                    .OnColumn("key").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("systemintegrationparameters").Exists())
            {
                if (Schema.Table("systemintegrationparameters").Index("ux_systemintegrationparameters_integration_key").Exists())
                {
                    Delete.Index("ux_systemintegrationparameters_integration_key").OnTable("systemintegrationparameters");
                }

                if (Schema.Table("systemintegrationparameters").Constraint("fk_systemintegrationparameters_systemintegrations").Exists())
                {
                    Delete.ForeignKey("fk_systemintegrationparameters_systemintegrations").OnTable("systemintegrationparameters");
                }

                Delete.Table("systemintegrationparameters");
            }

            if (Schema.Table("systemintegrations").Exists())
            {
                if (Schema.Table("systemintegrations").Index("ix_systemintegrations_systemapplicationid").Exists())
                {
                    Delete.Index("ix_systemintegrations_systemapplicationid").OnTable("systemintegrations");
                }

                if (Schema.Table("systemintegrations").Constraint("fk_systemintegrations_systemapplications").Exists())
                {
                    Delete.ForeignKey("fk_systemintegrations_systemapplications").OnTable("systemintegrations");
                }

                Delete.Table("systemintegrations");
            }

            if (Schema.Table("systemapplications").Exists())
            {
                if (Schema.Table("systemapplications").Column("baseurl").Exists())
                {
                    Delete.Column("baseurl").FromTable("systemapplications");
                }
            }
        }
    }
}
