using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202609080002)]
    public sealed class Migration_202609080002_AddSystemRoleTemplateCapabilities : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("systemroletemplatecapabilities").Exists())
            {
                Create.Table("systemroletemplatecapabilities")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("systemroletemplateid").AsInt64().NotNullable()
                    .WithColumn("capabilitykey").AsString(120).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("systemroletemplatecapabilities").Constraint("fk_systemroletemplatecapabilities_systemroletemplates_templateid").Exists())
            {
                Create.ForeignKey("fk_systemroletemplatecapabilities_systemroletemplates_templateid")
                    .FromTable("systemroletemplatecapabilities").ForeignColumn("systemroletemplateid")
                    .ToTable("systemroletemplates").PrimaryColumn("id");
            }

            if (!Schema.Table("systemroletemplatecapabilities").Index("ix_systemroletemplatecapabilities_templateid_capabilitykey").Exists())
            {
                Create.Index("ix_systemroletemplatecapabilities_templateid_capabilitykey")
                    .OnTable("systemroletemplatecapabilities")
                    .OnColumn("systemroletemplateid").Ascending()
                    .OnColumn("capabilitykey").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (!Schema.Table("systemroletemplatecapabilities").Exists())
            {
                return;
            }

            if (Schema.Table("systemroletemplatecapabilities").Index("ix_systemroletemplatecapabilities_templateid_capabilitykey").Exists())
            {
                Delete.Index("ix_systemroletemplatecapabilities_templateid_capabilitykey").OnTable("systemroletemplatecapabilities");
            }

            if (Schema.Table("systemroletemplatecapabilities").Constraint("fk_systemroletemplatecapabilities_systemroletemplates_templateid").Exists())
            {
                Delete.ForeignKey("fk_systemroletemplatecapabilities_systemroletemplates_templateid").OnTable("systemroletemplatecapabilities");
            }

            Delete.Table("systemroletemplatecapabilities");
        }
    }
}
