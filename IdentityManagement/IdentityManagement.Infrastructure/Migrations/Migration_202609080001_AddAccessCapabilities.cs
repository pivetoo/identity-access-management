using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202609080001)]
    public sealed class Migration_202609080001_AddAccessCapabilities : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("accessresources").Column("capabilities").Exists())
            {
                Alter.Table("accessresources")
                    .AddColumn("capabilities").AsString(500).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("accesscapabilities").Exists())
            {
                Create.Table("accesscapabilities")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemapplicationid").AsInt64().NotNullable()
                    .WithColumn("capabilitykey").AsString(120).NotNullable()
                    .WithColumn("module").AsString(60).NotNullable()
                    .WithColumn("modulelabel").AsString(120).NotNullable()
                    .WithColumn("moduleorder").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("label").AsString(200).NotNullable()
                    .WithColumn("description").AsString(500).NotNullable().WithDefaultValue(string.Empty)
                    .WithColumn("sortorder").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("isbaseline").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("accesscapabilities").Constraint("fk_accesscapabilities_systemapplications_systemapplicationid").Exists())
            {
                Create.ForeignKey("fk_accesscapabilities_systemapplications_systemapplicationid")
                    .FromTable("accesscapabilities").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");
            }

            if (!Schema.Table("accesscapabilities").Index("ix_accesscapabilities_systemapplicationid_capabilitykey").Exists())
            {
                Create.Index("ix_accesscapabilities_systemapplicationid_capabilitykey")
                    .OnTable("accesscapabilities")
                    .OnColumn("systemapplicationid").Ascending()
                    .OnColumn("capabilitykey").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("rolecapabilities").Exists())
            {
                Create.Table("rolecapabilities")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("roleid").AsInt64().NotNullable()
                    .WithColumn("capabilitykey").AsString(120).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("rolecapabilities").Constraint("fk_rolecapabilities_roles_roleid").Exists())
            {
                Create.ForeignKey("fk_rolecapabilities_roles_roleid")
                    .FromTable("rolecapabilities").ForeignColumn("roleid")
                    .ToTable("roles").PrimaryColumn("id");
            }

            if (!Schema.Table("rolecapabilities").Index("ix_rolecapabilities_roleid_capabilitykey").Exists())
            {
                Create.Index("ix_rolecapabilities_roleid_capabilitykey")
                    .OnTable("rolecapabilities")
                    .OnColumn("roleid").Ascending()
                    .OnColumn("capabilitykey").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("rolecapabilities").Exists())
            {
                if (Schema.Table("rolecapabilities").Index("ix_rolecapabilities_roleid_capabilitykey").Exists())
                {
                    Delete.Index("ix_rolecapabilities_roleid_capabilitykey").OnTable("rolecapabilities");
                }

                if (Schema.Table("rolecapabilities").Constraint("fk_rolecapabilities_roles_roleid").Exists())
                {
                    Delete.ForeignKey("fk_rolecapabilities_roles_roleid").OnTable("rolecapabilities");
                }

                Delete.Table("rolecapabilities");
            }

            if (Schema.Table("accesscapabilities").Exists())
            {
                if (Schema.Table("accesscapabilities").Index("ix_accesscapabilities_systemapplicationid_capabilitykey").Exists())
                {
                    Delete.Index("ix_accesscapabilities_systemapplicationid_capabilitykey").OnTable("accesscapabilities");
                }

                if (Schema.Table("accesscapabilities").Constraint("fk_accesscapabilities_systemapplications_systemapplicationid").Exists())
                {
                    Delete.ForeignKey("fk_accesscapabilities_systemapplications_systemapplicationid").OnTable("accesscapabilities");
                }

                Delete.Table("accesscapabilities");
            }

            if (Schema.Table("accessresources").Column("capabilities").Exists())
            {
                Delete.Column("capabilities").FromTable("accessresources");
            }
        }
    }
}
