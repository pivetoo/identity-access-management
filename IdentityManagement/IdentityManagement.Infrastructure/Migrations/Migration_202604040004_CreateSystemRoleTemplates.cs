using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040004)]
    public sealed class Migration_202604040004_CreateSystemRoleTemplates : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("systemroletemplates").Exists())
            {
                Create.Table("systemroletemplates")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemapplicationid").AsInt64().NotNullable()
                    .WithColumn("name").AsString(120).NotNullable()
                    .WithColumn("description").AsString(500).NotNullable()
                    .WithColumn("isroot").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("systemroletemplates").Constraint("fk_systemroletemplates_systemapplications_systemapplicationid").Exists())
            {
                Create.ForeignKey("fk_systemroletemplates_systemapplications_systemapplicationid")
                    .FromTable("systemroletemplates").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");
            }

            if (!Schema.Table("systemroletemplates").Index("ix_systemroletemplates_systemapplicationid_name").Exists())
            {
                Create.Index("ix_systemroletemplates_systemapplicationid_name")
                    .OnTable("systemroletemplates")
                    .OnColumn("systemapplicationid").Ascending()
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("systemroletemplateaccessresources").Exists())
            {
                Create.Table("systemroletemplateaccessresources")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemroletemplateid").AsInt64().NotNullable()
                    .WithColumn("accessresourceid").AsInt64().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("systemroletemplateaccessresources").Constraint("fk_systemroletemplateaccessresources_systemroletemplates_systemroletemplateid").Exists())
            {
                Create.ForeignKey("fk_systemroletemplateaccessresources_systemroletemplates_systemroletemplateid")
                    .FromTable("systemroletemplateaccessresources").ForeignColumn("systemroletemplateid")
                    .ToTable("systemroletemplates").PrimaryColumn("id");
            }

            if (!Schema.Table("systemroletemplateaccessresources").Constraint("fk_systemroletemplateaccessresources_accessresources_accessresourceid").Exists())
            {
                Create.ForeignKey("fk_systemroletemplateaccessresources_accessresources_accessresourceid")
                    .FromTable("systemroletemplateaccessresources").ForeignColumn("accessresourceid")
                    .ToTable("accessresources").PrimaryColumn("id");
            }

            if (!Schema.Table("systemroletemplateaccessresources").Index("ix_systemroletemplateaccessresources_templateid_accessresourceid").Exists())
            {
                Create.Index("ix_systemroletemplateaccessresources_templateid_accessresourceid")
                    .OnTable("systemroletemplateaccessresources")
                    .OnColumn("systemroletemplateid").Ascending()
                    .OnColumn("accessresourceid").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("systemroletemplateaccessresources").Exists())
            {
                if (Schema.Table("systemroletemplateaccessresources").Index("ix_systemroletemplateaccessresources_templateid_accessresourceid").Exists())
                {
                    Delete.Index("ix_systemroletemplateaccessresources_templateid_accessresourceid")
                        .OnTable("systemroletemplateaccessresources");
                }

                if (Schema.Table("systemroletemplateaccessresources").Constraint("fk_systemroletemplateaccessresources_accessresources_accessresourceid").Exists())
                {
                    Delete.ForeignKey("fk_systemroletemplateaccessresources_accessresources_accessresourceid")
                        .OnTable("systemroletemplateaccessresources");
                }

                if (Schema.Table("systemroletemplateaccessresources").Constraint("fk_systemroletemplateaccessresources_systemroletemplates_systemroletemplateid").Exists())
                {
                    Delete.ForeignKey("fk_systemroletemplateaccessresources_systemroletemplates_systemroletemplateid")
                        .OnTable("systemroletemplateaccessresources");
                }

                Delete.Table("systemroletemplateaccessresources");
            }

            if (!Schema.Table("systemroletemplates").Exists())
            {
                return;
            }

            if (Schema.Table("systemroletemplates").Index("ix_systemroletemplates_systemapplicationid_name").Exists())
            {
                Delete.Index("ix_systemroletemplates_systemapplicationid_name")
                    .OnTable("systemroletemplates");
            }

            if (Schema.Table("systemroletemplates").Constraint("fk_systemroletemplates_systemapplications_systemapplicationid").Exists())
            {
                Delete.ForeignKey("fk_systemroletemplates_systemapplications_systemapplicationid")
                    .OnTable("systemroletemplates");
            }

            Delete.Table("systemroletemplates");
        }
    }
}
