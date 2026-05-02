using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605020001)]
    public sealed class Migration_202605020001_AddTenantIdToContracts : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("contracts").Column("tenantid").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("tenantid").AsGuid().Nullable();
            }

            Execute.Sql(@"
                UPDATE contracts
                SET tenantid = gen_random_uuid()
                WHERE tenantid IS NULL;
            ");

            Alter.Table("contracts")
                .AlterColumn("tenantid").AsGuid().NotNullable();

            if (!Schema.Table("contracts").Index("ix_contracts_tenantid").Exists())
            {
                Create.Index("ix_contracts_tenantid")
                    .OnTable("contracts")
                    .OnColumn("tenantid").Ascending()
                    .WithOptions().Unique();
            }
        }

        public override void Down()
        {
            if (Schema.Table("contracts").Index("ix_contracts_tenantid").Exists())
            {
                Delete.Index("ix_contracts_tenantid").OnTable("contracts");
            }

            if (Schema.Table("contracts").Column("tenantid").Exists())
            {
                Delete.Column("tenantid").FromTable("contracts");
            }
        }
    }
}
