using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605140001)]
    public sealed class Migration_202605140001_CreateTenantDatabasesTable : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("tenantdatabases").Exists())
            {
                Create.Table("tenantdatabases")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("contractid").AsInt64().NotNullable()
                    .WithColumn("connectionstring").AsString(2000).NotNullable()
                    .WithColumn("databaseprovider").AsInt32().NotNullable()
                    .WithColumn("schemaname").AsString(100).NotNullable().WithDefaultValue("public")
                    .WithColumn("apikey").AsString(255).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("tenantdatabases").Constraint("fk_tenantdatabases_contracts").Exists())
            {
                Create.ForeignKey("fk_tenantdatabases_contracts")
                    .FromTable("tenantdatabases").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");
            }

            if (!Schema.Table("tenantdatabases").Index("ix_tenantdatabases_contractid").Exists())
            {
                Create.Index("ix_tenantdatabases_contractid")
                    .OnTable("tenantdatabases")
                    .OnColumn("contractid").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("tenantdatabases").Index("ix_tenantdatabases_apikey").Exists())
            {
                Create.Index("ix_tenantdatabases_apikey")
                    .OnTable("tenantdatabases")
                    .OnColumn("apikey").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("tenantdatabases").Index("ix_tenantdatabases_isactive").Exists())
            {
                Create.Index("ix_tenantdatabases_isactive")
                    .OnTable("tenantdatabases")
                    .OnColumn("isactive").Ascending();
            }
        }

        public override void Down()
        {
            if (Schema.Table("tenantdatabases").Index("ix_tenantdatabases_isactive").Exists())
            {
                Delete.Index("ix_tenantdatabases_isactive").OnTable("tenantdatabases");
            }

            if (Schema.Table("tenantdatabases").Index("ix_tenantdatabases_apikey").Exists())
            {
                Delete.Index("ix_tenantdatabases_apikey").OnTable("tenantdatabases");
            }

            if (Schema.Table("tenantdatabases").Index("ix_tenantdatabases_contractid").Exists())
            {
                Delete.Index("ix_tenantdatabases_contractid").OnTable("tenantdatabases");
            }

            if (Schema.Table("tenantdatabases").Constraint("fk_tenantdatabases_contracts").Exists())
            {
                Delete.ForeignKey("fk_tenantdatabases_contracts").OnTable("tenantdatabases");
            }

            if (Schema.Table("tenantdatabases").Exists())
            {
                Delete.Table("tenantdatabases");
            }
        }
    }
}
